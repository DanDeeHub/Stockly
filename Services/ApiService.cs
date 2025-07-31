using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Stockly.Dtos.Users;
using Stockly.Interface;

namespace Stockly.Services;

public class ApiService(IHttpClientFactory httpClientFactory, ProtectedLocalStorage localStorage)
    : IApiService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("StocklyAPI");

    public async Task<bool> LoginAsync(UserRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("v1/users/authenticate", request);
        
        if (response.IsSuccessStatusCode)
        {
            var authResult = await response.Content.ReadFromJsonAsync<UserResponseDto>();
            
            if (authResult?.JwtToken == null)
            {
                await Console.Error.WriteLineAsync("Authentication succeeded but no token was received");
                return false;
            }
            
            await localStorage.SetAsync("authToken", authResult.JwtToken);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.JwtToken);
            
            return true;
        }
        return false;
    }
    
    public async Task<string?> GetTokenAsync()
    {
        var result = await localStorage.GetAsync<string>("authToken");
        return result.Success ? result.Value : null;
    }
    
    public async Task LogoutAsync()
    {
        await localStorage.DeleteAsync("authToken");
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }
}