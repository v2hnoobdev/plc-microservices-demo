using PLC.Identity.API.DTOs;

namespace PLC.Identity.API.Services;

public interface IIdentityService
{
    Task<UserResponse> CreateUserAsync(CreateUserRequest request);
    Task<UserResponse> GetUserByIdAsync(string userId);
    Task UpdateUserAsync(string userId, UpdateUserRequest request);
    Task DeleteUserAsync(string userId);
    Task ResetPasswordAsync(string userId, ResetPasswordRequest request);
    Task AssignRoleAsync(string userId, AssignRoleRequest request);
}
