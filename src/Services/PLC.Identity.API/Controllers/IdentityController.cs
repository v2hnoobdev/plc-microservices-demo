using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PLC.Identity.API.DTOs;
using PLC.Identity.API.Services;

namespace PLC.Identity.API.Controllers;

[ApiController]
[Route("internal/identity")]
[Authorize]
public class IdentityController : ControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(
        IIdentityService identityService,
        ILogger<IdentityController> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new user in Keycloak
    /// </summary>
    [HttpPost("users")]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _identityService.CreateUserAsync(request);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Username}", request.Username);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get user by ID from Keycloak
    /// </summary>
    [HttpGet("users/{id}")]
    public async Task<ActionResult<UserResponse>> GetUser(string id)
    {
        try
        {
            var user = await _identityService.GetUserByIdAsync(id);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user: {UserId}", id);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update user in Keycloak
    /// </summary>
    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            await _identityService.UpdateUserAsync(id, request);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete user from Keycloak
    /// </summary>
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        try
        {
            await _identityService.DeleteUserAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reset user password in Keycloak
    /// </summary>
    [HttpPost("users/{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest request)
    {
        try
        {
            await _identityService.ResetPasswordAsync(id, request);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password: {UserId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Assign role to user in Keycloak
    /// </summary>
    [HttpPost("users/{id}/roles")]
    public async Task<IActionResult> AssignRole(string id, [FromBody] AssignRoleRequest request)
    {
        try
        {
            await _identityService.AssignRoleAsync(id, request);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role: {UserId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
}
