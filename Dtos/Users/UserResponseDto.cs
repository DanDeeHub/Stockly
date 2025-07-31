namespace Stockly.Dtos.Users;

public class UserResponseDto(string jwtToken)
{
    public string JwtToken { get; init; } = jwtToken;
}