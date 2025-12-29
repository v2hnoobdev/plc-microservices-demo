using System.Text.Json.Serialization;

namespace PLC.User.API.Models.KeycloakEvents;

/// <summary>
/// Keycloak User Representation - parse từ field "representation" trong AdminEvent
/// </summary>
public class KeycloakUserRepresentation
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("createdTimestamp")]
    public long? CreatedTimestamp { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("totp")]
    public bool Totp { get; set; }

    [JsonPropertyName("emailVerified")]
    public bool EmailVerified { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, List<string>>? Attributes { get; set; }

    [JsonPropertyName("disableableCredentialTypes")]
    public List<string>? DisableableCredentialTypes { get; set; }

    [JsonPropertyName("requiredActions")]
    public List<string>? RequiredActions { get; set; }

    [JsonPropertyName("notBefore")]
    public long? NotBefore { get; set; }

    [JsonPropertyName("access")]
    public Access? Access { get; set; }
}

public class Access
{
    [JsonPropertyName("manageGroupMembership")]
    public bool ManageGroupMembership { get; set; }

    [JsonPropertyName("view")]
    public bool View { get; set; }

    [JsonPropertyName("mapRoles")]
    public bool MapRoles { get; set; }

    [JsonPropertyName("impersonate")]
    public bool Impersonate { get; set; }

    [JsonPropertyName("manage")]
    public bool Manage { get; set; }
}
