using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Stockly.Entities;
using Stockly.Interfaces;

namespace Stockly.Services;

public class JwtParserService : IJwtParserService
{
    public User? ParseUserFromToken(string jwtToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(jwtToken)) return null;

            var token = handler.ReadJwtToken(jwtToken);
            var claims = token.Claims.ToList();

            // Flexible claim type resolution
            var userIdClaim = claims.FirstOrDefault(c => 
                c.Type == ClaimTypes.NameIdentifier || 
                c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            var usernameClaim = claims.FirstOrDefault(c => 
                c.Type == ClaimTypes.Name || 
                c.Type == JwtRegisteredClaimNames.UniqueName)?.Value;

            var roleClaim = claims.FirstOrDefault(c => 
                c.Type == ClaimTypes.Role || 
                c.Type == "role")?.Value; // Some systems use just "role"

            var emailClaim = claims.FirstOrDefault(c => 
                c.Type == ClaimTypes.Email || 
                c.Type == JwtRegisteredClaimNames.Email)?.Value;

            return new User
            {
                Id = Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty,
                Username = usernameClaim ?? string.Empty,
                Role = roleClaim ?? string.Empty,
                Email = emailClaim ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Token parsing failed: {ex.Message}");
            return null;
        }
    }

    public bool IsTokenValid(string jwtToken)
    {
        // Early return for null/empty tokens
        if (string.IsNullOrWhiteSpace(jwtToken))
            return false;

        // Check if the token is structurally valid
        if (!CanReadToken(jwtToken))
            return false;

        // Optional: Add time-based validation
        var token = ReadJwtTokenSafely(jwtToken);
        return token != null && token.ValidTo > DateTime.UtcNow;
    }

    public DateTime GetTokenExpiration(string jwtToken)
    {
        var token = new JwtSecurityToken(jwtToken);
        return token.ValidTo;
    }
    
    private static bool CanReadToken(string jwtToken)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.CanReadToken(jwtToken);
    }
    
    
    private static JwtSecurityToken? ReadJwtTokenSafely(string jwtToken)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            return handler.ReadJwtToken(jwtToken);
        }
        catch
        {
            return null;
        }
    }
}