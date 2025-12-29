using System.ComponentModel.DataAnnotations;

namespace PLC.User.API.Models;

public class User
{
    /// <summary>
    /// Primary key - User Service's internal ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to Keycloak user (sub claim from JWT token)
    /// </summary>
    [Required]
    public Guid KeycloakUserId { get; set; }

    /// <summary>
    /// Username from Keycloak
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email from Keycloak
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    [MaxLength(100)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name
    /// </summary>
    [MaxLength(100)]
    public string? LastName { get; set; }

    /// <summary>
    /// User role (e.g., "admin", "user", "manager")
    /// </summary>
    [MaxLength(50)]
    public string? Role { get; set; }

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Record last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
