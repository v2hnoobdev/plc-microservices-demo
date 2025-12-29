using System.Text.Json.Serialization;

namespace PLC.User.API.Models.KeycloakEvents;

/// <summary>
/// Keycloak Admin Event từ NATS
/// </summary>
public class KeycloakAdminEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("realmId")]
    public string RealmId { get; set; } = string.Empty;

    [JsonPropertyName("realmName")]
    public string RealmName { get; set; } = string.Empty;

    [JsonPropertyName("authDetails")]
    public AuthDetails? AuthDetails { get; set; }

    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = string.Empty;

    [JsonPropertyName("operationType")]
    public string OperationType { get; set; } = string.Empty;

    [JsonPropertyName("resourcePath")]
    public string? ResourcePath { get; set; }

    [JsonPropertyName("representation")]
    public string? Representation { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("resourceId")]
    public string? ResourceId { get; set; }
}

public class AuthDetails
{
    [JsonPropertyName("realmId")]
    public string RealmId { get; set; } = string.Empty;

    [JsonPropertyName("realmName")]
    public string RealmName { get; set; } = string.Empty;

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;
}
