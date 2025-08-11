
using Microsoft.JSInterop;
using System.Text.Json;

namespace Stockly.Services
{
    public class SecureStorageService
    {
        private readonly IJSRuntime _jsRuntime;

        public SecureStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
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
            catch (Exception)
            {
                // Ignore errors
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
                // Don't try to clear session data during prerendering
                if (!ex.Message.Contains("JavaScript interop calls cannot be issued"))
                {
                    await ClearSessionDataAsync();
                }
                return null;
            }
        }



        public async Task ClearSessionDataAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "stockly_session");
            }
            catch (Exception)
            {
                // Ignore prerendering errors
            }
        }

        public async Task ClearAllStorageAsync()
        {
            await ClearSessionDataAsync();
        }




    }

    public class SessionData
    {
        public Guid UserId { get; set; } = Guid.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime SessionStart { get; set; }
    }
} 