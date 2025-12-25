using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PLC.User.API.DTOs;
using PLC.User.API.Services;

namespace PLC.User.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        _logger.LogInformation("Getting all users. User: {Username}", User.Identity?.Name);

        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        _logger.LogInformation("Getting user {UserId}. User: {Username}", id, User.Identity?.Name);

        var user = await _userService.GetUserByIdAsync(id);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", id);
            return NotFound(new { message = $"User with ID {id} not found" });
        }

        return Ok(user);
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
    {
        _logger.LogInformation("Creating new user {Username}. User: {CurrentUser}",
            createUserDto.Username, User.Identity?.Name);

        // Check if username already exists
        if (await _userService.UsernameExistsAsync(createUserDto.Username))
        {
            _logger.LogWarning("Username {Username} already exists", createUserDto.Username);
            return Conflict(new { message = $"Username '{createUserDto.Username}' already exists" });
        }

        var user = await _userService.CreateUserAsync(createUserDto);

        _logger.LogInformation("User {UserId} created successfully", user.Id);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserDto updateUserDto)
    {
        _logger.LogInformation("Updating user {UserId}. User: {CurrentUser}",
            id, User.Identity?.Name);

        var success = await _userService.UpdateUserAsync(id, updateUserDto);

        if (!success)
        {
            _logger.LogWarning("User {UserId} not found", id);
            return NotFound(new { message = $"User with ID {id} not found" });
        }

        _logger.LogInformation("User {UserId} updated successfully", id);

        return NoContent();
    }

    /// <summary>
    /// Delete a user (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        _logger.LogInformation("Deleting user {UserId}. User: {CurrentUser}",
            id, User.Identity?.Name);

        var success = await _userService.DeleteUserAsync(id);

        if (!success)
        {
            _logger.LogWarning("User {UserId} not found", id);
            return NotFound(new { message = $"User with ID {id} not found" });
        }

        _logger.LogInformation("User {UserId} deleted successfully", id);

        return NoContent();
    }

    /// <summary>
    /// Get current authenticated user info
    /// </summary>
    [HttpGet("me")]
    public ActionResult<object> GetCurrentUser()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });

        var userInfo = new
        {
            Username = User.Identity?.Name,
            IsAuthenticated = User.Identity?.IsAuthenticated,
            AuthenticationType = User.Identity?.AuthenticationType,
            Claims = claims
        };

        _logger.LogInformation("Current user info requested: {Username}", User.Identity?.Name);

        return Ok(userInfo);
    }
}
