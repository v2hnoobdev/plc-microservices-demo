using PLC.User.API.DTOs;

namespace PLC.User.API.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<UserDto?> GetUserByKeycloakIdAsync(Guid keycloakUserId);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
    Task<bool> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);
    Task<bool> DeleteUserAsync(Guid id);
    Task<bool> UserExistsAsync(Guid id);
    Task<bool> KeycloakUserExistsAsync(Guid keycloakUserId);
    Task<bool> UsernameExistsAsync(string username);
}
