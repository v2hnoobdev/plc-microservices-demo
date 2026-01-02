namespace PLC.Identity.API.DTOs;

public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool? EmailVerified { get; set; }
    public bool? Enabled { get; set; }
}
