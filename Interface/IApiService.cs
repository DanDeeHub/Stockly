using Stockly.Dtos.Users;

namespace Stockly.Interface;

public interface IApiService
{
    Task<bool> LoginAsync(UserRequestDto request);
    Task<string?> GetTokenAsync();
    Task LogoutAsync();
}