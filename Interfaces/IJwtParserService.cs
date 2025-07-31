using Stockly.Entities;

namespace Stockly.Interfaces;

public interface IJwtParserService
{
    User? ParseUserFromToken(string jwtToken);
    bool IsTokenValid(string jwtToken);  // Optional
    DateTime GetTokenExpiration(string jwtToken);  // Optional
}