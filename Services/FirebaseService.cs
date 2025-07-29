using Google.Cloud.Firestore;
using System.Text.Json;

namespace Stockly.Services
{
    public class FirebaseService
    {
        private readonly FirestoreDb _db;
        private readonly string _credentialsPath;

        public FirebaseService(IConfiguration configuration)
        {
            _credentialsPath = Path.Combine(Directory.GetCurrentDirectory(), "Credentials", "Firebase", "stockly-db-firebase-adminsdk-fbsvc-0441a05a82.json");
            
            try
            {
                var builder = new FirestoreDbBuilder
                {
                    ProjectId = "stockly-db",
                    CredentialsPath = _credentialsPath
                };
                
                _db = builder.Build();
                Console.WriteLine($"Successfully connected to Firebase project: stockly-db");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to Firebase: {ex.Message}");
                Console.WriteLine($"Please ensure:");
                Console.WriteLine($"1. The project 'stockly-db' exists in Firebase Console");
                Console.WriteLine($"2. Firestore Database is enabled for this project");
                Console.WriteLine($"3. The service account has proper permissions");
                throw;
            }
        }

        public async Task<User> AuthenticateUserAsync(string username, string password)
        {
            try
            {
                Console.WriteLine($"Attempting to authenticate user: {username}");
                
                // Query the users collection for the username
                CollectionReference usersRef = _db.Collection("users");
                Query query = usersRef.WhereEqualTo("username", username);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                Console.WriteLine($"Found {snapshot.Count} matching users");

                if (snapshot.Count == 0)
                {
                    Console.WriteLine($"User '{username}' not found in database");
                    return null; // User not found
                }

                // Get the first matching user document
                DocumentSnapshot userDoc = snapshot.Documents[0];
                var userData = userDoc.ConvertTo<Dictionary<string, object>>();

                Console.WriteLine($"User document ID: {userDoc.Id}");
                Console.WriteLine($"User data keys: {string.Join(", ", userData.Keys)}");

                // Check if password matches (in a real app, you'd hash the password)
                if (userData.ContainsKey("password") && userData["password"].ToString() == password)
                {
                    Console.WriteLine($"Authentication successful for user: {username}");
                    return new User
                    {
                        Id = userDoc.Id,
                        Username = userData["username"].ToString(),
                        Role = userData.ContainsKey("role") ? userData["role"].ToString() : "user",
                        Email = userData.ContainsKey("email") ? userData["email"].ToString() : ""
                    };
                }

                Console.WriteLine($"Invalid password for user: {username}");
                return null; // Invalid password
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error authenticating user: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<bool> CreateUserAsync(string username, string password, string email = "", string role = "user")
        {
            try
            {
                // Check if user already exists
                CollectionReference usersRef = _db.Collection("users");
                Query query = usersRef.WhereEqualTo("username", username);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                if (snapshot.Count > 0)
                {
                    return false; // User already exists
                }

                // Create new user document
                var userData = new Dictionary<string, object>
                {
                    { "username", username },
                    { "password", password }, // In production, hash this password
                    { "email", email },
                    { "role", role },
                    { "createdAt", Timestamp.FromDateTime(DateTime.UtcNow) }
                };

                await usersRef.AddAsync(userData);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user: {ex.Message}");
                return false;
            }
        }
    }

    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
} 