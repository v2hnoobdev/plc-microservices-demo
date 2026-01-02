namespace PLC.Identity.API.DTOs;

public class ResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
    public bool Temporary { get; set; } = false;
}
