using System;
using System.Threading.Tasks;
using MongoDB.Driver;

public class MongoUserStore
{
    private readonly IMongoCollection<User> users;

    public MongoUserStore(string connectionString, string dbName = "sof2remake")
    {
        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(dbName);
        users = db.GetCollection<User>("users");

        // Indices: username und email einmalig
        var usernameIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Username),
            new CreateIndexOptions { Unique = true });

        var emailIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true });

        users.Indexes.CreateMany(new[] { usernameIndex, emailIndex });
    }

    public Task<User> FindByEmailAsync(string email)
        => users.Find(u => u.Email == email).FirstOrDefaultAsync();

    public Task<User> FindByUsernameAsync(string username)
        => users.Find(u => u.Username == username).FirstOrDefaultAsync();

    public Task CreateUserAsync(User user)
        => users.InsertOneAsync(user);
}
