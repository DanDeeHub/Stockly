using Google.Cloud.Firestore;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Stockly.Services
{
    public class FirebaseService
    {
        private readonly FirestoreDb _db;

        public FirebaseService(IConfiguration configuration)
        {
            try
            {
                // Use the credentials file for now (it's already in .gitignore)
                var credentialsPath = Path.Combine(Directory.GetCurrentDirectory(), "Credentials", "Firebase", "stockly-db-firebase-adminsdk-fbsvc-0441a05a82.json");
                
                var builder = new FirestoreDbBuilder
                {
                    ProjectId = "stockly-db",
                    CredentialsPath = credentialsPath
                };
                
                _db = builder.Build();
                Console.WriteLine($"Successfully connected to Firebase project: stockly-db");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to Firebase: {ex.Message}");
                Console.WriteLine($"Please ensure the Firebase credentials file exists at:");
                Console.WriteLine($"Credentials/Firebase/stockly-db-firebase-adminsdk-fbsvc-0441a05a82.json");
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
                    
                    // Debug: Print all user data
                    Console.WriteLine($"User data: {JsonSerializer.Serialize(userData)}");
                    
                    // Handle case where field name might have trailing spaces
                    var roleKey = userData.Keys.FirstOrDefault(k => k.Trim() == "role");
                    var role = roleKey != null ? userData[roleKey].ToString() : "user";
                    Console.WriteLine($"Retrieved role: '{role}'");
                    
                    return new User
                    {
                        Id = userDoc.Id,
                        Username = userData["username"].ToString(),
                        Role = role,
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

        // Product Management Methods
        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                Console.WriteLine("Attempting to get products from Firestore...");
                
                CollectionReference productsRef = _db.Collection("products");
                QuerySnapshot snapshot = await productsRef.GetSnapshotAsync();
                
                Console.WriteLine($"Found {snapshot.Count} products in collection");
                
                var products = new List<Product>();
                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    var data = doc.ConvertTo<Dictionary<string, object>>();
                    var product = new Product
                    {
                        Id = doc.Id,
                        Name = data.ContainsKey("name") ? data["name"].ToString() : "",
                        Category = data.ContainsKey("category") ? data["category"].ToString() : "",
                        Stock = data.ContainsKey("stock") ? Convert.ToInt32(data["stock"]) : 0,
                        Status = data.ContainsKey("status") ? data["status"].ToString() : "",
                        Price = data.ContainsKey("price") ? Convert.ToDecimal(data["price"]) : 0,
                        LowStockThreshold = data.ContainsKey("lowStockThreshold") ? Convert.ToInt32(data["lowStockThreshold"]) : 10,
                        CreatedAt = data.ContainsKey("createdAt") ? ((Timestamp)data["createdAt"]).ToDateTime() : DateTime.UtcNow
                    };
                    
                    // Set status color based on status
                    product.StatusColor = product.Status switch
                    {
                        "In stock" => Color.Success,
                        "Low stock" => Color.Warning,
                        "Out of stock" => Color.Error,
                        _ => Color.Default
                    };
                    
                    products.Add(product);
                    Console.WriteLine($"Loaded product: {product.Name} (ID: {product.Id})");
                }
                
                Console.WriteLine($"Successfully loaded {products.Count} products");
                return products;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting products: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<Product>();
            }
        }

        public async Task<bool> CreateProductAsync(Product product)
        {
            try
            {
                Console.WriteLine($"Attempting to create product: {product.Name}");
                
                CollectionReference productsRef = _db.Collection("products");
                var productData = new Dictionary<string, object>
                {
                    { "name", product.Name },
                    { "category", product.Category },
                    { "stock", product.Stock },
                    { "status", product.Status },
                    { "price", Convert.ToDouble(product.Price) }, // Convert decimal to double for Firestore
                    { "lowStockThreshold", product.LowStockThreshold },
                    { "createdAt", Timestamp.FromDateTime(DateTime.UtcNow) }
                };

                Console.WriteLine($"Product data prepared: {JsonSerializer.Serialize(productData)}");
                
                // Add the document to the collection (this will create the collection if it doesn't exist)
                DocumentReference docRef = await productsRef.AddAsync(productData);
                
                Console.WriteLine($"Product created successfully with ID: {docRef.Id}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating product: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                DocumentReference productRef = _db.Collection("products").Document(product.Id);
                var productData = new Dictionary<string, object>
                {
                    { "name", product.Name },
                    { "category", product.Category },
                    { "stock", product.Stock },
                    { "status", product.Status },
                    { "price", Convert.ToDouble(product.Price) }, // Convert decimal to double for Firestore
                    { "lowStockThreshold", product.LowStockThreshold },
                    { "updatedAt", Timestamp.FromDateTime(DateTime.UtcNow) }
                };

                await productRef.UpdateAsync(productData);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(string productId)
        {
            try
            {
                DocumentReference productRef = _db.Collection("products").Document(productId);
                await productRef.DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting product: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                Console.WriteLine("Testing Firebase connection...");
                
                // Try to access a collection to test the connection
                CollectionReference testRef = _db.Collection("test");
                await testRef.GetSnapshotAsync();
                
                Console.WriteLine("Firebase connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Firebase connection test failed: {ex.Message}");
                return false;
            }
        }

        // Activity Management Methods
        public async Task<bool> CreateActivityAsync(Activity activity)
        {
            try
            {
                CollectionReference activitiesRef = _db.Collection("activities");
                var activityData = new Dictionary<string, object>
                {
                    { "type", activity.Type },
                    { "title", activity.Title },
                    { "description", activity.Description },
                    { "productName", activity.ProductName },
                    { "category", activity.Category },
                    { "timestamp", Timestamp.FromDateTime(activity.Timestamp) },
                    { "iconColor", activity.IconColor.ToString() },
                    { "icon", activity.Icon }
                };

                await activitiesRef.AddAsync(activityData);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating activity: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Activity>> GetRecentActivitiesAsync(int hours = 24)
        {
            try
            {
                CollectionReference activitiesRef = _db.Collection("activities");
                var cutoffTime = DateTime.UtcNow.AddHours(-hours);
                
                Query query = activitiesRef.WhereGreaterThan("timestamp", Timestamp.FromDateTime(cutoffTime))
                                        .OrderByDescending("timestamp")
                                        .Limit(10);

                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                var activities = new List<Activity>();

                foreach (var doc in snapshot.Documents)
                {
                    var data = doc.ToDictionary();
                    var activity = new Activity
                    {
                        Id = doc.Id,
                        Type = data.ContainsKey("type") ? data["type"].ToString() : "",
                        Title = data.ContainsKey("title") ? data["title"].ToString() : "",
                        Description = data.ContainsKey("description") ? data["description"].ToString() : "",
                        ProductName = data.ContainsKey("productName") ? data["productName"].ToString() : "",
                        Category = data.ContainsKey("category") ? data["category"].ToString() : "",
                        Timestamp = data.ContainsKey("timestamp") ? ((Timestamp)data["timestamp"]).ToDateTime() : DateTime.UtcNow,
                        Icon = data.ContainsKey("icon") ? data["icon"].ToString() : "",
                        IconColor = data.ContainsKey("iconColor") ? ParseColor(data["iconColor"].ToString()) : Color.Default
                    };
                    activities.Add(activity);
                }

                return activities;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting recent activities: {ex.Message}");
                return new List<Activity>();
            }
        }

        private Color ParseColor(string colorString)
        {
            return colorString switch
            {
                "Success" => Color.Success,
                "Warning" => Color.Warning,
                "Error" => Color.Error,
                "Info" => Color.Info,
                "Primary" => Color.Primary,
                _ => Color.Default
            };
        }

        // Enhanced product creation with activity tracking
        public async Task<bool> CreateProductWithActivityAsync(Product product)
        {
            try
            {
                // Create the product first
                var productSuccess = await CreateProductAsync(product);
                if (!productSuccess) return false;

                // Create activity for new product
                var activity = new Activity
                {
                    Type = "new_item",
                    Title = "New item added",
                    Description = $"New item added: {product.Name}",
                    ProductName = product.Name,
                    Category = product.Category,
                    Timestamp = DateTime.UtcNow,
                    IconColor = Color.Success,
                    Icon = Icons.Material.Filled.Add
                };

                await CreateActivityAsync(activity);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating product with activity: {ex.Message}");
                return false;
            }
        }

        // Check for low stock and create activity if needed
        public async Task CheckLowStockAndCreateActivityAsync(Product product)
        {
            try
            {
                if (product.Stock <= product.LowStockThreshold && product.Stock > 0)
                {
                    var activity = new Activity
                    {
                        Type = "low_stock",
                        Title = "Low stock alert",
                        Description = $"Low stock alert: {product.Name}",
                        ProductName = product.Name,
                        Category = product.Category,
                        Timestamp = DateTime.UtcNow,
                        IconColor = Color.Warning,
                        Icon = Icons.Material.Filled.Warning
                    };

                    await CreateActivityAsync(activity);
                }
                else if (product.Stock == 0)
                {
                    var activity = new Activity
                    {
                        Type = "out_of_stock",
                        Title = "Out of stock",
                        Description = $"Out of stock: {product.Name}",
                        ProductName = product.Name,
                        Category = product.Category,
                        Timestamp = DateTime.UtcNow,
                        IconColor = Color.Error,
                        Icon = Icons.Material.Filled.Remove
                    };

                    await CreateActivityAsync(activity);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking low stock: {ex.Message}");
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

    public class Product
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public int Stock { get; set; }
        public string Status { get; set; } = "";
        public Color StatusColor { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int LowStockThreshold { get; set; } = 10; // Default threshold for low stock alerts
    }

    public class Activity
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = ""; // "new_item", "low_stock", "out_of_stock", "inventory_count"
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Category { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Color IconColor { get; set; }
        public string Icon { get; set; } = "";
    }
} 