using Stockly.Entities;
using Stockly.Interfaces;

namespace Stockly.Services;

public class UserStateService(IJwtParserService jwtParser, IApiService apiService) : IUserStateService
{
    private User? _currentUser;
    public User? CurrentUser 
    { 
        get => _currentUser;
        private set
        {
            _currentUser = value;
            NotifyStateChanged();
        }
    }

    // Role-specific properties
    public string Role => _currentUser?.Role ?? string.Empty;
    public string Username => _currentUser?.Username ?? string.Empty;
    public bool IsAuthenticated => _currentUser != null;
    public event Action? OnChange;

    public async Task<bool> LoadUserStateAsync()
    {
        var token = await apiService.GetTokenAsync();
        if (string.IsNullOrEmpty(token)) 
            return false; // No token available isn't an error case

        var user = jwtParser.ParseUserFromToken(token);
        CurrentUser = user ?? throw new InvalidOperationException("Failed to parse user from token");
        return true;
    }

    public void UpdateFromToken(string jwtToken) => CurrentUser = jwtParser.ParseUserFromToken(jwtToken);

    public Task ClearUserStateAsync()
    {
        CurrentUser = null;
        return Task.CompletedTask;
    }

    // Role checking methods
    public bool HasRole(string role) => 
        IsAuthenticated && string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);

    public bool HasAnyRole(params string[] roles) => 
        IsAuthenticated && roles.Any(r => string.Equals(Role, r, StringComparison.OrdinalIgnoreCase));

    private void NotifyStateChanged() => OnChange?.Invoke();
}