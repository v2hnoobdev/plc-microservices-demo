using Microsoft.EntityFrameworkCore;
using PLC.User.API.Data;
using PLC.User.API.DTOs;

namespace PLC.User.API.Services;

public class UserService : IUserService
{
    private readonly UserDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(UserDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        _logger.LogDebug("Fetching all users");

        return await _context.Users
            .Select(u => new UserDto
            {
                Id = u.Id,
                KeycloakUserId = u.KeycloakUserId,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        _logger.LogDebug("Fetching user with ID {UserId}", id);

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return null;
        }

        return new UserDto
        {
            Id = user.Id,
            KeycloakUserId = user.KeycloakUserId,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<UserDto?> GetUserByKeycloakIdAsync(Guid keycloakUserId)
    {
        _logger.LogDebug("Fetching user with Keycloak ID {KeycloakUserId}", keycloakUserId);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId);

        if (user == null)
        {
            return null;
        }

        return new UserDto
        {
            Id = user.Id,
            KeycloakUserId = user.KeycloakUserId,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        _logger.LogDebug("Fetching user with username {Username}", username);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            return null;
        }

        return new UserDto
        {
            Id = user.Id,
            KeycloakUserId = user.KeycloakUserId,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
    {
        _logger.LogDebug("Creating user with username {Username}", createUserDto.Username);

        var user = new Models.User
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = createUserDto.KeycloakUserId,
            Username = createUserDto.Username,
            Email = createUserDto.Email,
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            Role = createUserDto.Role,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User created with ID {UserId} for Keycloak user {KeycloakUserId}",
            user.Id, user.KeycloakUserId);

        return new UserDto
        {
            Id = user.Id,
            KeycloakUserId = user.KeycloakUserId,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<bool> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto)
    {
        _logger.LogDebug("Updating user with ID {UserId}", id);

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return false;
        }

        user.Email = updateUserDto.Email;
        user.FirstName = updateUserDto.FirstName;
        user.LastName = updateUserDto.LastName;
        user.Role = updateUserDto.Role;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated successfully", id);

        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        _logger.LogDebug("Deleting user with ID {UserId}", id);

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deleted successfully", id);

        return true;
    }

    public async Task<bool> UserExistsAsync(Guid id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }

    public async Task<bool> KeycloakUserExistsAsync(Guid keycloakUserId)
    {
        return await _context.Users.AnyAsync(u => u.KeycloakUserId == keycloakUserId);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _context.Users.AnyAsync(u => u.Username == username);
    }
}
