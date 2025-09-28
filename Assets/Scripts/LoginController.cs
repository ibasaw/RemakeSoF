using UnityEngine;
using UnityEngine.UIElements;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class LoginController : MonoBehaviour
{
    private TextField emailField;
    private TextField passwordField;
    private Button loginButton;
    private Label statusLabel;

    private HttpClient httpClient;

    void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        emailField = root.Q<TextField>("emailField");
        passwordField = root.Q<TextField>("passwordField");
        loginButton = root.Q<Button>("loginButton");
        statusLabel = root.Q<Label>("statusLabel");

        loginButton.clicked += OnLoginClicked;

        httpClient = new HttpClient();
    }

    private async void OnLoginClicked()
    {
        statusLabel.text = "Logging in...";
        loginButton.SetEnabled(false);

        var payload = new JObject
        {
            ["email"] = emailField.text,
            ["password"] = passwordField.text
        };

        try
        {
            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("http://localhost:8080/login", content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var json = JObject.Parse(responseBody);
                string token = json["access_token"]?.ToString();
                statusLabel.text = "Login successful!";
                Debug.Log("JWT Token: " + token);

                // TODO: Client-Netcode verbinden / nächste Scene laden
            }
            else
            {
                statusLabel.text = "Login failed: " + responseBody;
            }
        }
        catch (System.Exception ex)
        {
            statusLabel.text = "Error: " + ex.Message;
        }
        finally
        {
            loginButton.SetEnabled(true);
        }
    }
}
