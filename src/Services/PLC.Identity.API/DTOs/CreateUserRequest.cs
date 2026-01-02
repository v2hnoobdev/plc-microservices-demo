namespace PLC.Identity.API.DTOs;

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; }
    public bool EmailVerified { get; set; } = false;
    public bool Enabled { get; set; } = true;
}
