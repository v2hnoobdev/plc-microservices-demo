using PLC.Identity.API.DTOs;

namespace PLC.Identity.API.Services;

public class IdentityService : IIdentityService
{
    private readonly KeycloakAdminClient _keycloakClient;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        KeycloakAdminClient keycloakClient,
        ILogger<IdentityService> logger)
    {
        _keycloakClient = keycloakClient;
        _logger = logger;
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            var user = new KeycloakUserRepresentation
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailVerified = request.EmailVerified,
                Enabled = request.Enabled,
                Credentials = new List<KeycloakCredential>
                {
                    new KeycloakCredential
                    {
                        Type = "password",
                        Value = request.Password,
                        Temporary = false
                    }
                }
            };

            var userId = await _keycloakClient.CreateUserAsync(user);

            var createdUser = await _keycloakClient.GetUserAsync(userId);

            if (createdUser == null)
            {
                throw new Exception("User created but could not be retrieved");
            }

            // Assign role if provided
            if (!string.IsNullOrEmpty(request.Role))
            {
                await AssignRoleAsync(userId, new AssignRoleRequest { RoleName = request.Role });
            }

            return new UserResponse
            {
                Id = createdUser.Id ?? string.Empty,
                Username = createdUser.Username ?? string.Empty,
                Email = createdUser.Email ?? string.Empty,
                FirstName = createdUser.FirstName,
                LastName = createdUser.LastName,
                EmailVerified = createdUser.EmailVerified ?? false,
                Enabled = createdUser.Enabled ?? true,
                CreatedTimestamp = createdUser.CreatedTimestamp ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Username}", request.Username);
            throw;
        }
    }

    public async Task<UserResponse> GetUserByIdAsync(string userId)
    {
        try
        {
            var user = await _keycloakClient.GetUserAsync(userId);

            if (user == null)
            {
                throw new Exception($"User {userId} not found");
            }

            return new UserResponse
            {
                Id = user.Id ?? string.Empty,
                Username = user.Username ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmailVerified = user.EmailVerified ?? false,
                Enabled = user.Enabled ?? true,
                CreatedTimestamp = user.CreatedTimestamp ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user: {UserId}", userId);
            throw;
        }
    }

    public async Task UpdateUserAsync(string userId, UpdateUserRequest request)
    {
        try
        {
            var existingUser = await _keycloakClient.GetUserAsync(userId);

            if (existingUser == null)
            {
                throw new Exception($"User {userId} not found");
            }

            // Update only provided fields
            var updatedUser = new KeycloakUserRepresentation
            {
                Id = userId,
                Username = existingUser.Username,
                Email = request.Email ?? existingUser.Email,
                FirstName = request.FirstName ?? existingUser.FirstName,
                LastName = request.LastName ?? existingUser.LastName,
                EmailVerified = request.EmailVerified ?? existingUser.EmailVerified,
                Enabled = request.Enabled ?? existingUser.Enabled
            };

            await _keycloakClient.UpdateUserAsync(userId, updatedUser);

            _logger.LogInformation("User updated successfully: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", userId);
            throw;
        }
    }

    public async Task DeleteUserAsync(string userId)
    {
        try
        {
            await _keycloakClient.DeleteUserAsync(userId);
            _logger.LogInformation("User deleted successfully: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", userId);
            throw;
        }
    }

    public async Task ResetPasswordAsync(string userId, ResetPasswordRequest request)
    {
        try
        {
            await _keycloakClient.ResetPasswordAsync(userId, request.NewPassword, request.Temporary);

            _logger.LogInformation("Password reset successfully for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user: {UserId}", userId);
            throw;
        }
    }

    public async Task AssignRoleAsync(string userId, AssignRoleRequest request)
    {
        try
        {
            var realmRoles = await _keycloakClient.GetRealmRolesAsync();
            var role = realmRoles.FirstOrDefault(r => r.Name?.Equals(request.RoleName, StringComparison.OrdinalIgnoreCase) == true);

            if (role == null)
            {
                throw new Exception($"Role '{request.RoleName}' not found in realm");
            }

            await _keycloakClient.AssignRealmRoleToUserAsync(userId, new List<KeycloakRoleRepresentation> { role });

            _logger.LogInformation("Role '{RoleName}' assigned to user: {UserId}", request.RoleName, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to user: {UserId}", userId);
            throw;
        }
    }
}
