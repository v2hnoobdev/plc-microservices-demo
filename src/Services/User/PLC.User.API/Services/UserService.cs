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
        _logger.LogDebug("Fetching all active users");

        return await _context.Users
            .Where(u => u.IsActive)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                Department = u.Department,
                IsActive = u.IsActive
            })
            .ToListAsync();
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
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
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Department = user.Department,
            IsActive = user.IsActive
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
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Department = user.Department,
            IsActive = user.IsActive
        };
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
    {
        _logger.LogDebug("Creating user with username {Username}", createUserDto.Username);

        var user = new Models.User
        {
            Username = createUserDto.Username,
            Email = createUserDto.Email,
            FullName = createUserDto.FullName,
            Department = createUserDto.Department,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User created with ID {UserId}", user.Id);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Department = user.Department,
            IsActive = user.IsActive
        };
    }

    public async Task<bool> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
    {
        _logger.LogDebug("Updating user with ID {UserId}", id);

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return false;
        }

        user.Email = updateUserDto.Email;
        user.FullName = updateUserDto.FullName;
        user.Department = updateUserDto.Department;
        user.IsActive = updateUserDto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated successfully", id);

        return true;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        _logger.LogDebug("Soft deleting user with ID {UserId}", id);

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return false;
        }

        // Soft delete
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} soft deleted successfully", id);

        return true;
    }

    public async Task<bool> UserExistsAsync(int id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _context.Users.AnyAsync(u => u.Username == username);
    }
}
