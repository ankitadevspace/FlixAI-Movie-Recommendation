using backend.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using NetflixClone.Services;

namespace NetflixClone.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // =========================================================
    // REGISTER
    // POST: api/auth/register
    // =========================================================

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response =
                await _authService.RegisterAsync(
                    request,
                    cancellationToken);

            return Created(
                string.Empty,
                response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // =========================================================
    // LOGIN
    // POST: api/auth/login
    // =========================================================

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response =
                await _authService.LoginAsync(
                    request,
                    cancellationToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }
    }

    // =========================================================
    // CURRENT USER
    // GET: api/auth/me
    // =========================================================

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId =
            User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)
            ?.Value;

        var name =
            User.FindFirst(
                System.Security.Claims.ClaimTypes.Name)
            ?.Value;

        var email =
            User.FindFirst(
                System.Security.Claims.ClaimTypes.Email)
            ?.Value;

        return Ok(new
        {
            userId,
            name,
            email
        });
    }
}