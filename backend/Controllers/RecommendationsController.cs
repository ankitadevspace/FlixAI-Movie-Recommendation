using backend.Dtos;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Services;

namespace NetflixClone.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(
        IRecommendationService recommendationService,
        ILogger<RecommendationsController> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    // ============================================================
    // GET PERSONALIZED RECOMMENDATIONS
    //
    // GET:
    // /api/Recommendations/user/1?count=10
    // ============================================================

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<RecommendationResult>> GetRecommendations(
        int userId,
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        if (count < 1 || count > 50)
        {
            count = 10;
        }

        try
        {
            var result = await _recommendationService
                .GetRecommendationsAsync(
                    userId,
                    count,
                    cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to generate recommendations for user {UserId}",
                userId);

            return StatusCode(500, new
            {
                message = "Unable to generate recommendations."
            });
        }
    }
}