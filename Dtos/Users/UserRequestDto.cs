using System.ComponentModel.DataAnnotations;

namespace Stockly.Dtos.Users;

public class UserRequestDto
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; }  = string.Empty;
    public bool RememberMe { get; set; }
}