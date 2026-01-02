using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PLC.Identity.API.Services;
/// <summary>
/// Ref link: https://www.keycloak.org/docs-api/latest/rest-api/index.html
/// </summary>
public class KeycloakAdminClient
{
    private readonly HttpClient _httpClient;
    private readonly string _keycloakUrl;
    private readonly string _realm;
    private readonly string _adminUsername;
    private readonly string _adminPassword;
    private readonly ILogger<KeycloakAdminClient> _logger;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public KeycloakAdminClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<KeycloakAdminClient> logger)
    {
        _httpClient = httpClient;
        _keycloakUrl = configuration["Keycloak:Url"] ?? throw new InvalidOperationException("Keycloak:Url not configured");
        _realm = configuration["Keycloak:Realm"] ?? throw new InvalidOperationException("Keycloak:Realm not configured");
        _adminUsername = configuration["Keycloak:AdminUsername"] ?? throw new InvalidOperationException("Keycloak:AdminUsername not configured");
        _adminPassword = configuration["Keycloak:AdminPassword"] ?? throw new InvalidOperationException("Keycloak:AdminPassword not configured");
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_keycloakUrl);
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return; // Token still valid
        }

        await AuthenticateAsync();
    }

    private async Task AuthenticateAsync()
    {
        var tokenUrl = $"/realms/master/protocol/openid-connect/token";
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", "admin-cli"),
            new KeyValuePair<string, string>("username", _adminUsername),
            new KeyValuePair<string, string>("password", _adminPassword),
            new KeyValuePair<string, string>("grant_type", "password")
        });

        var response = await _httpClient.PostAsync(tokenUrl, content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(json);

        _accessToken = tokenResponse.GetProperty("access_token").GetString();
        var expiresIn = tokenResponse.GetProperty("expires_in").GetInt32();
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 10); // 10 second buffer

        _logger.LogInformation("Authenticated with Keycloak admin API");
    }

    public async Task<string> CreateUserAsync(KeycloakUserRepresentation user)
    {
        await EnsureAuthenticatedAsync();

        var url = $"/admin/realms/{_realm}/users";
        var json = JsonSerializer.Serialize(user, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to create user in Keycloak: {Error}", error);
            throw new Exception($"Failed to create user: {response.StatusCode} - {error}");
        }

        // Get user ID from Location header
        var location = response.Headers.Location?.ToString();
        if (location == null)
        {
            throw new Exception("User created but Location header not found");
        }

        var userId = location.Split('/').Last();
        return userId;
    }

    public async Task<KeycloakUserRepresentation?> GetUserAsync(string userId)
    {
        await EnsureAuthenticatedAsync();

        var url = $"/admin/realms/{_realm}/users/{userId}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<KeycloakUserRepresentation>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    public async Task<List<KeycloakUserRepresentation>> SearchUsersAsync(string? username = null, string? email = null)
    {
        await EnsureAuthenticatedAsync();

        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(username))
            queryParams.Add($"username={Uri.EscapeDataString(username)}");
        if (!string.IsNullOrEmpty(email))
            queryParams.Add($"email={Uri.EscapeDataString(email)}");

        var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        var url = $"/admin/realms/{_realm}/users{query}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<KeycloakUserRepresentation>>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) ?? new List<KeycloakUserRepresentation>();
    }

    public async Task UpdateUserAsync(string userId, KeycloakUserRepresentation user)
    {
        await EnsureAuthenticatedAsync();

        var url = $"/admin/realms/{_realm}/users/{userId}";
        var json = JsonSerializer.Serialize(user, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteUserAsync(string userId)
    {
        await EnsureAuthenticatedAsync();

        var url = $"/admin/realms/{_realm}/users/{userId}";
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task ResetPasswordAsync(string userId, string newPassword, bool temporary = false)
    {
        await EnsureAuthenticatedAsync();

        var url = $"/admin/realms/{_realm}/users/{userId}/reset-password";
        var credential = new
        {
            type = "password",
            value = newPassword,
            temporary = temporary
        };

        var json = JsonSerializer.Serialize(credential);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<KeycloakRoleRepresentation>> GetRealmRolesAsync()
    {
        await EnsureAuthenticatedAsync();

        var url = $"/admin/realms/{_realm}/roles";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<KeycloakRoleRepresentation>>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) ?? new List<KeycloakRoleRepresentation>();
    }

    public async Task AssignRealmRoleToUserAsync(string userId, List<KeycloakRoleRepresentation> roles)
    {
        await EnsureAuthenticatedAsync();

        var url = $"/admin/realms/{_realm}/users/{userId}/role-mappings/realm";
        var json = JsonSerializer.Serialize(roles, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}

public class KeycloakUserRepresentation
{
    public string? Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool? Enabled { get; set; }
    public bool? EmailVerified { get; set; }
    public long? CreatedTimestamp { get; set; }
    public List<KeycloakCredential>? Credentials { get; set; }
}

public class KeycloakCredential
{
    public string? Type { get; set; }
    public string? Value { get; set; }
    public bool Temporary { get; set; }
}

public class KeycloakRoleRepresentation
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
