using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Dtos;
using NetflixClone.Services;

namespace NetflixClone.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RagController : ControllerBase
{
    private readonly IRagService _ragService;

    public RagController(IRagService ragService)
    {
        _ragService = ragService;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<RagResponse>> Ask(
        [FromBody] RagRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new
            {
                message = "The authenticated user ID is missing or invalid."
            });
        }

        request.UserId = userId;

        try
        {
            var response = await _ragService.AskAsync(
                request,
                cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}