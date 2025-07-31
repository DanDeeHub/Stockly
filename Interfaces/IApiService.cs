using Stockly.Dtos.Users;

namespace Stockly.Interfaces;

public interface IApiService
{
    Task<(bool Success, string? Token)> AuthenticateAsync(UserRequestDto request);
    Task<string?> GetTokenAsync();
    Task LogoutAsync();
}