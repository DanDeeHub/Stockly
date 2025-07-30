using System.Security.Cryptography;
using System.Text;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Stockly.Services
{
    public class SecureStorageService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly string _encryptionKey;

        public SecureStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            // In production, this should be stored securely and not hardcoded
            // Consider using environment variables or a secure key management service
            _encryptionKey = "StocklySecureKey2024!@#$%^&*()";
        }

        public async Task SaveCredentialsAsync(Guid userId, string username, string password, bool rememberMe = false)
        {
            if (!rememberMe)
            {
                // If not remembering, just store session data
                await SaveSessionDataAsync(userId, username);
                return;
            }

            try
            {
                var credentials = new StoredCredentials
                {
                    UserId = userId,
                    Username = username,
                    Password = password,
                    StoredAt = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(credentials);
                var encryptedData = EncryptString(json);
                
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "stockly_credentials", encryptedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving credentials: {ex.Message}");
                throw;
            }
        }

        public async Task<StoredCredentials?> LoadCredentialsAsync()
        {
            try
            {
                var encryptedData = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "stockly_credentials");
                
                if (string.IsNullOrEmpty(encryptedData))
                    return null;

                var decryptedJson = DecryptString(encryptedData);
                var credentials = JsonSerializer.Deserialize<StoredCredentials>(decryptedJson);
                
                // Check if credentials are still valid (not older than 30 days)
                if (credentials?.StoredAt.AddDays(30) < DateTime.UtcNow)
                {
                    await ClearCredentialsAsync();
                    return null;
                }

                return credentials;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading credentials: {ex.Message}");
                await ClearCredentialsAsync();
                return null;
            }
        }

        public async Task SaveSessionDataAsync(Guid userId, string username)
        {
            try
            {
                var sessionData = new SessionData
                {
                    UserId = userId,
                    Username = username,
                    SessionStart = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(sessionData);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "stockly_session", json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving session data: {ex.Message}");
            }
        }

        public async Task<SessionData?> LoadSessionDataAsync()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "stockly_session");
                
                if (string.IsNullOrEmpty(json))
                    return null;

                var sessionData = JsonSerializer.Deserialize<SessionData>(json);
                
                // Check if session is still valid (not older than 24 hours)
                if (sessionData?.SessionStart.AddHours(24) < DateTime.UtcNow)
                {
                    await ClearSessionDataAsync();
                    return null;
                }

                return sessionData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading session data: {ex.Message}");
                await ClearSessionDataAsync();
                return null;
            }
        }

        public async Task ClearCredentialsAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "stockly_credentials");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing credentials: {ex.Message}");
            }
        }

        public async Task ClearSessionDataAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "stockly_session");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing session data: {ex.Message}");
            }
        }

        public async Task ClearAllStorageAsync()
        {
            await ClearCredentialsAsync();
            await ClearSessionDataAsync();
        }

        private string EncryptString(string plainText)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16]; // Simple IV for demo - in production use random IV

                using var encryptor = aes.CreateEncryptor();
                using var msEncrypt = new MemoryStream();
                using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
                using var swEncrypt = new StreamWriter(csEncrypt);

                swEncrypt.Write(plainText);
                swEncrypt.Flush();
                csEncrypt.FlushFinalBlock();

                return Convert.ToBase64String(msEncrypt.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Encryption error: {ex.Message}");
                throw;
            }
        }

        private string DecryptString(string cipherText)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16]; // Simple IV for demo - in production use random IV

                using var decryptor = aes.CreateDecryptor();
                using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);

                return srDecrypt.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption error: {ex.Message}");
                throw;
            }
        }
    }

    public class StoredCredentials
    {
        public Guid UserId { get; set; } = Guid.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime StoredAt { get; set; }
    }

    public class SessionData
    {
        public Guid UserId { get; set; } = Guid.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime SessionStart { get; set; }
    }
} 