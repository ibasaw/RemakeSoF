using UnityEngine;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq; // Newtonsoft.Json
using System;
using Newtonsoft.Json;

public class AuthServer : MonoBehaviour
{
    private HttpListener listener;
    private bool isRunning = true;

    // Hol dir die Werte aus ENV in Produktion
    private readonly string mongoConn;
    private readonly string jwtSecret;
    private MongoUserStore userStore;

    private readonly string SERVER_HOST = "localhost";
    private readonly string MONGODB_HOST = "localhost";
    private readonly int PORT = 8080;
    private readonly int MONGODB_PORT = 27017;
    private const int BCRYPT_WORK_FACTOR = 12;



    public AuthServer()
    {
        // Umgebungsvariablen oder Fallbacks
        mongoConn = Environment.GetEnvironmentVariable("MONGO_CONN") ?? "mongodb://" + MONGODB_HOST + ":" + MONGODB_PORT;
        jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "THIS_IS_A_SUPER_SECRET_KEY_32BYTES!!";
    }

    void Awake()
    {
        userStore = new MongoUserStore(mongoConn, "sof2remake");
    }

    void Start()
    {
        Debug.Log("[AuthServer] AuthServer gestartet!");
        listener = new HttpListener();
        listener.Prefixes.Add("http://" + SERVER_HOST + ":" + PORT + "/");
        listener.Start();
        Debug.Log("[AuthServer] Listening on http://" + SERVER_HOST + ":" + PORT);
        Task.Run(() => HandleRequests());
    }

    public bool IsRunning()
    {
        return listener != null && listener.IsListening;
    }

    async Task HandleRequests()
    {
        while (isRunning)
        {
            HttpListenerContext ctx = null;
            try
            {
                ctx = await listener.GetContextAsync();
                var req = ctx.Request;
                var res = ctx.Response;

                if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/register")
                {
                    using var reader = new System.IO.StreamReader(req.InputStream);
                    var body = await reader.ReadToEndAsync();
                    var json = JObject.Parse(body);
                    var username = (string)json["username"];
                    var email = (string)json["email"];
                    var password = (string)json["password"];

                    if (string.IsNullOrWhiteSpace(username) ||
                        string.IsNullOrWhiteSpace(email) ||
                        string.IsNullOrWhiteSpace(password))
                    {
                        WriteJsonResponse(res, 400, new { error = "username,email,password required" });
                        continue;
                    }

                    // Prüfe existierende Email/Username
                    var existingByEmail = await userStore.FindByEmailAsync(email);
                    if (existingByEmail != null)
                    {
                        WriteJsonResponse(res, 409, new { error = "email already in use" });
                        continue;
                    }
                    var existingByUsername = await userStore.FindByUsernameAsync(username);
                    if (existingByUsername != null)
                    {
                        WriteJsonResponse(res, 409, new { error = "username already in use" });
                        continue;
                    }

                    // Hash password (BCrypt, work factor 12)
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: BCRYPT_WORK_FACTOR);

                    var user = new User
                    {
                        Username = username,
                        Email = email,
                        PasswordHash = passwordHash,
                        CreatedAt = DateTime.UtcNow
                    };

                    await userStore.CreateUserAsync(user);

                    WriteJsonResponse(res, 201, new { result = "ok", userId = user.Id });
                }
                else if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/login")
                {
                    using var reader = new System.IO.StreamReader(req.InputStream);
                    var body = await reader.ReadToEndAsync();
                    var json = JObject.Parse(body);
                    var email = (string)json["email"];
                    var password = (string)json["password"];

                    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    {
                        WriteJsonResponse(res, 400, new { error = "email,password required" });
                        continue;
                    }

                    var user = await userStore.FindByEmailAsync(email);
                    if (user == null)
                    {
                        WriteJsonResponse(res, 401, new { error = "invalid credentials" });
                        continue;
                    }

                    bool ok = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                    if (!ok)
                    {
                        WriteJsonResponse(res, 401, new { error = "invalid credentials" });
                        continue;
                    }

                    // Erstelle JWT
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.UTF8.GetBytes(jwtSecret);
                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new System.Security.Claims.ClaimsIdentity(
                            new[] { new System.Security.Claims.Claim("sub", user.Id) }),
                        Expires = DateTime.UtcNow.AddMinutes(15),
                        SigningCredentials = new SigningCredentials(
                            new SymmetricSecurityKey(key),
                            SecurityAlgorithms.HmacSha256Signature)
                    };

                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    var jwt = tokenHandler.WriteToken(token);

                    WriteJsonResponse(res, 200, new { access_token = jwt });
                }
                else
                {
                    res.StatusCode = 404;
                    res.Close();
                }
            }
            catch (JsonReaderException jrex)
            {
                if (isRunning)
                    Debug.LogWarning("JSON Parse Error: " + jrex.Message);
                break;
            }
            catch (HttpListenerException hlex)
            {
                if (isRunning)
                    Debug.LogWarning("HttpListener Exception: " + hlex.Message);
                break;
            }
            catch (ObjectDisposedException odex)
            {
                Debug.Log("Listener disposed: " + odex.Message);
                break;
            }
            catch (Exception ex)
            {
                Debug.LogError("Unhandled exception in AuthServer: " + ex);
                if (ctx != null && ctx.Response != null)
                {
                    try
                    {
                        ctx.Response.StatusCode = 500;
                        ctx.Response.Close();
                    }
                    catch { /* ignore */ }
                }
            }
        }
    }

    void WriteJsonResponse(HttpListenerResponse res, int statusCode, object obj)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        var buffer = Encoding.UTF8.GetBytes(json);
        res.ContentType = "application/json";
        res.ContentLength64 = buffer.Length;
        res.StatusCode = statusCode;
        using (var output = res.OutputStream)
            output.Write(buffer, 0, buffer.Length);
        res.Close();
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (listener != null && listener.IsListening)
        {
            listener.Stop();
            listener.Close();
        }
    }

    public void StopServer()
    {
        isRunning = false;
        if (listener != null && listener.IsListening)
        {
            listener.Stop();
            listener.Close();
            Debug.Log("[AuthServer] AuthServer gestoppt");
        }
    }

}
