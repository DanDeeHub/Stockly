using Google.Cloud.Firestore;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
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
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<User> AuthenticateUserAsync(string username, string password)
        {
            try
            {
                // Query the users collection for the username
                CollectionReference usersRef = _db.Collection("users");
                Query query = usersRef.WhereEqualTo("username", username);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                if (snapshot.Count == 0)
                {
                    return null; // User not found
                }

                // Get the first matching user document
                DocumentSnapshot userDoc = snapshot.Documents[0];
                var userData = userDoc.ConvertTo<Dictionary<string, object>>();

                // Check if password matches (in a real app, you'd hash the password)
                if (userData.ContainsKey("password") && userData["password"].ToString() == password)
                {
                    // Handle case where field name might have trailing spaces
                    var roleKey = userData.Keys.FirstOrDefault(k => k.Trim() == "role");
                    var role = roleKey != null ? userData[roleKey].ToString() : "user";
                    
                    // Try to parse the document ID as Guid, fallback to empty Guid if it's not a valid Guid
                    Guid userId = Guid.Empty;
                    if (Guid.TryParse(userDoc.Id, out Guid parsedId))
                    {
                        userId = parsedId;
                    }
                    
                    return new User
                    {
                        Id = userId,
                        Username = userData["username"].ToString(),
                        Role = role,
                        Email = userData.ContainsKey("email") ? userData["email"].ToString() : ""
                    };
                }

                return null; // Invalid password
            }
            catch (Exception ex)
            {
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

                // Create new user document with Guid as ID
                var userId = Guid.NewGuid();
                var userData = new Dictionary<string, object>
                {
                    { "username", username },
                    { "password", password }, // In production, hash this password
                    { "email", email },
                    { "role", role },
                    { "createdAt", Timestamp.FromDateTime(DateTime.UtcNow) }
                };

                await usersRef.Document(userId.ToString()).SetAsync(userData);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Product Management Methods
        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                CollectionReference productsRef = _db.Collection("products");
                QuerySnapshot snapshot = await productsRef.GetSnapshotAsync();
                
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
                        CreatedAt = data.ContainsKey("createdAt") ? ((Timestamp)data["createdAt"]).ToDateTime() : DateTime.UtcNow,
                        LastUpdated = data.ContainsKey("lastUpdated") ? ((Timestamp)data["lastUpdated"]).ToDateTime() : null
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
                }
                
                return products;
            }
            catch (Exception)
            {
                return new List<Product>();
            }
        }

        public async Task<bool> CheckProductNameExistsAsync(string productName)
        {
            try
            {
                CollectionReference productsRef = _db.Collection("products");
                Query query = productsRef.WhereEqualTo("name", productName);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                
                return snapshot.Count > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CheckProductNameExistsAsync(string productName, string excludeProductId)
        {
            try
            {
                CollectionReference productsRef = _db.Collection("products");
                Query query = productsRef.WhereEqualTo("name", productName);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                
                // Check if any product with this name exists, excluding the specified product ID
                foreach (var doc in snapshot.Documents)
                {
                    if (doc.Id != excludeProductId)
                    {
                        return true; // Found another product with the same name
                    }
                }
                
                return false; // No other product with this name found
            }
            catch (Exception)
            {
                return false;
            }
        }

        private DateTime GetPhilippineTime()
        {
            // Philippine Standard Time is UTC+8
            return DateTime.UtcNow.AddHours(8);
        }

        public async Task<bool> CreateProductAsync(Product product)
        {
            try
            {
                CollectionReference productsRef = _db.Collection("products");
                var productData = new Dictionary<string, object>
                {
                    { "name", product.Name },
                    { "category", product.Category },
                    { "stock", product.Stock },
                    { "status", product.Status },
                    { "price", Convert.ToDouble(product.Price) }, // Convert decimal to double for Firestore
                    { "lowStockThreshold", product.LowStockThreshold },
                    { "createdAt", Timestamp.FromDateTime(GetPhilippineTime()) },
                    { "lastUpdated", Timestamp.FromDateTime(GetPhilippineTime()) }
                };
                
                // Add the document to the collection (this will create the collection if it doesn't exist)
                await productsRef.AddAsync(productData);
                return true;
            }
            catch (Exception)
            {
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
                    { "updatedAt", Timestamp.FromDateTime(GetPhilippineTime()) },
                    { "lastUpdated", Timestamp.FromDateTime(GetPhilippineTime()) }
                };

                await productRef.UpdateAsync(productData);
                return true;
            }
            catch (Exception)
            {
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
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteProductWithActivityAsync(Product product)
        {
            try
            {
                // Delete the product
                DocumentReference productRef = _db.Collection("products").Document(product.Id);
                await productRef.DeleteAsync();

                // Create activity for item deletion
                var activity = new Activity
                {
                    Type = "item_deleted",
                    Title = "Item Deleted",
                    Description = $"Product '{product.Name}' has been removed from inventory",
                    ProductName = product.Name,
                    Category = product.Category,
                    Timestamp = DateTime.UtcNow,
                    Icon = Icons.Material.Filled.Delete,
                    IconColor = Color.Error
                };

                await CreateActivityAsync(activity);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // Try to access a collection to test the connection
                CollectionReference testRef = _db.Collection("test");
                await testRef.GetSnapshotAsync();
                return true;
            }
            catch (Exception)
            {
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
                    { "timestamp", Timestamp.FromDateTime(GetPhilippineTime()) },
                    { "iconColor", activity.IconColor.ToString() },
                    { "icon", activity.Icon }
                };

                // Add the new activity
                await activitiesRef.AddAsync(activityData);

                // Maintain only the 14 most recent activities (sliding window)
                await MaintainActivityLimitAsync(14);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task MaintainActivityLimitAsync(int maxActivities)
        {
            try
            {
                CollectionReference activitiesRef = _db.Collection("activities");
                
                // Get all activities ordered by timestamp (oldest first)
                Query query = activitiesRef.OrderBy("timestamp");
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                // If we have more than the limit, delete the oldest ones
                if (snapshot.Count > maxActivities)
                {
                    int activitiesToDelete = snapshot.Count - maxActivities;
                    
                    // Delete the oldest activities
                    for (int i = 0; i < activitiesToDelete; i++)
                    {
                        var doc = snapshot.Documents[i];
                        await doc.Reference.DeleteAsync();
                    }
                }
            }
            catch (Exception)
            {
                // Log error if needed, but don't fail the main operation
            }
        }

        public async Task<List<Activity>> GetRecentActivitiesAsync(int hours = 24)
        {
            try
            {
                CollectionReference activitiesRef = _db.Collection("activities");
                
                // Since we now maintain only 14 activities, we can get all of them
                // and filter by time if needed, or just return the most recent ones
                Query query = activitiesRef.OrderByDescending("timestamp")
                                        .Limit(14); // Get the 14 most recent activities

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
            catch (Exception)
            {
                return new List<Activity>();
            }
        }

        public async Task<List<Activity>> GetAllActivitiesAsync()
        {
            try
            {
                CollectionReference activitiesRef = _db.Collection("activities");
                
                Query query = activitiesRef.OrderByDescending("timestamp");

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
            catch (Exception)
            {
                return new List<Activity>();
            }
        }

        public async Task<bool> CreateReminderAsync(string title, string message, string createdBy)
        {
            try
            {
                Console.WriteLine($"CreateReminderAsync called with: title='{title}', message='{message}', createdBy='{createdBy}'");
                
                // Test Firebase connection first
                var connectionTest = await TestConnectionAsync();
                if (!connectionTest)
                {
                    Console.WriteLine("Firebase connection test failed");
                    return false;
                }
                
                // Delete any existing reminders first
                var existingRemindersRef = _db.Collection("reminders");
                Console.WriteLine("Getting existing reminders...");
                var existingSnapshot = await existingRemindersRef.GetSnapshotAsync();
                Console.WriteLine($"Found {existingSnapshot.Documents.Count} existing reminders");
                
                foreach (var doc in existingSnapshot.Documents)
                {
                    try
                    {
                        await doc.Reference.DeleteAsync();
                    }
                    catch (Exception)
                    {
                        // Continue with creation even if deletion fails
                    }
                }

                var remindersRef = _db.Collection("reminders");
                var currentTime = DateTime.UtcNow; // Use UTC time for Firestore
                var reminderData = new Dictionary<string, object>
                {
                    { "title", title },
                    { "message", message },
                    { "createdAt", Timestamp.FromDateTime(currentTime) },
                    { "createdBy", createdBy }
                };
                
                await remindersRef.AddAsync(reminderData);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<Reminder>> GetActiveRemindersAsync()
        {
            try
            {
                var remindersRef = _db.Collection("reminders");
                var snapshot = await remindersRef.GetSnapshotAsync();

                var reminders = new List<Reminder>();
                
                foreach (var doc in snapshot.Documents)
                {
                    var data = doc.ToDictionary();
                    var createdAt = data.ContainsKey("createdAt") ? ((Timestamp)data["createdAt"]).ToDateTime() : DateTime.UtcNow;
                    
                    var reminder = new Reminder
                    {
                        Id = doc.Id,
                        Title = data.ContainsKey("title") ? data["title"].ToString() : "",
                        Message = data.ContainsKey("message") ? data["message"].ToString() : "",
                        CreatedAt = createdAt,
                        CreatedBy = data.ContainsKey("createdBy") ? data["createdBy"].ToString() : ""
                    };
                    reminders.Add(reminder);
                }

                var result = reminders.OrderByDescending(r => r.CreatedAt).ToList();
                return result;
            }
            catch (Exception)
            {
                return new List<Reminder>();
            }
        }

        public async Task<bool> DeleteReminderAsync(string reminderId)
        {
            try
            {
                var reminderRef = _db.Collection("reminders").Document(reminderId);
                await reminderRef.DeleteAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
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
            catch (Exception)
            {
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
            catch (Exception)
            {
                // Ignore errors
            }
        }
    }

    public class User
    {
        public Guid Id { get; set; } = Guid.Empty;
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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(8); // Philippine time
        public DateTime? LastUpdated { get; set; } // Timestamp of last update
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
        public DateTime Timestamp { get; set; } = DateTime.UtcNow.AddHours(8); // Philippine time
        public Color IconColor { get; set; }
        public string Icon { get; set; } = "";
    }

    public class Reminder
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
} 