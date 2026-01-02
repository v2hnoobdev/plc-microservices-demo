namespace PLC.Identity.API.DTOs;

public class UserResponse
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool EmailVerified { get; set; }
    public bool Enabled { get; set; }
    public long CreatedTimestamp { get; set; }
}
