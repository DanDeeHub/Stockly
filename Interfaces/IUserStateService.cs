using Stockly.Entities;

namespace Stockly.Interfaces;

public interface IUserStateService
{
    User? CurrentUser { get; }
    bool IsAuthenticated { get; }
    string Role { get; }
    string Username { get; }
    event Action? OnChange;
    
    Task<bool> LoadUserStateAsync();
    void UpdateFromToken(string jwtToken);
    Task ClearUserStateAsync();
    
    bool HasRole(string role);
    bool HasAnyRole(params string[] roles);
}