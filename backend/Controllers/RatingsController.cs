using System.Security.Claims;

using backend.Data;
using backend.Dtos;
using backend.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace NetflixClone.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RatingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public RatingsController(AppDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // ADD OR UPDATE RATING
    // POST: api/Ratings
    // =========================================================

    [HttpPost]
    public async Task<ActionResult<RatingResponse>> AddOrUpdateRating(
        [FromBody] CreateRatingRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(new
            {
                message = "Invalid user token."
            });
        }

        if (request.Score < 0.5 || request.Score > 5)
        {
            return BadRequest(new
            {
                message = "Rating must be between 0.5 and 5."
            });
        }

        var movie = await _context.Movies
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Id == request.MovieId,
                cancellationToken);

        if (movie == null)
        {
            return NotFound(new
            {
                message = "Movie not found."
            });
        }

        var rating = await _context.Ratings
            .FirstOrDefaultAsync(
                r =>
                    r.UserId == userId.Value &&
                    r.MovieId == request.MovieId,
                cancellationToken);

        if (rating == null)
        {
            rating = new Rating
            {
                UserId = userId.Value,
                MovieId = request.MovieId,
                Score = request.Score,
                CreatedAt = DateTime.UtcNow
            };

            _context.Ratings.Add(rating);
        }
        else
        {
            rating.Score = request.Score;
            rating.CreatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(rating, movie.Title));
    }

    // =========================================================
    // GET CURRENT USER'S RATINGS
    // GET: api/Ratings
    // =========================================================

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RatingResponse>>> GetMyRatings(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(new
            {
                message = "Invalid user token."
            });
        }

        var ratings = await _context.Ratings
            .AsNoTracking()
            .Where(r => r.UserId == userId.Value)
            .Include(r => r.Movie)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RatingResponse
            {
                Id = r.Id,
                UserId = r.UserId,
                MovieId = r.MovieId,
                Score = r.Score,
                CreatedAt = r.CreatedAt,
                MovieTitle = r.Movie != null
                    ? r.Movie.Title
                    : null
            })
            .ToListAsync(cancellationToken);

        return Ok(ratings);
    }

    // =========================================================
    // GET RATING FOR CURRENT USER + MOVIE
    // GET: api/Ratings/movie/{movieId}
    // =========================================================

    [HttpGet("movie/{movieId:int}")]
    public async Task<ActionResult<RatingResponse>> GetMovieRating(
        int movieId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(new
            {
                message = "Invalid user token."
            });
        }

        var rating = await _context.Ratings
            .AsNoTracking()
            .Include(r => r.Movie)
            .FirstOrDefaultAsync(
                r =>
                    r.UserId == userId.Value &&
                    r.MovieId == movieId,
                cancellationToken);

        if (rating == null)
        {
            return NotFound(new
            {
                message = "Rating not found."
            });
        }

        return Ok(ToResponse(
            rating,
            rating.Movie?.Title));
    }

    // =========================================================
    // DELETE RATING
    // DELETE: api/Ratings/movie/{movieId}
    // =========================================================

    [HttpDelete("movie/{movieId:int}")]
    public async Task<IActionResult> DeleteRating(
        int movieId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(new
            {
                message = "Invalid user token."
            });
        }

        var rating = await _context.Ratings
            .FirstOrDefaultAsync(
                r =>
                    r.UserId == userId.Value &&
                    r.MovieId == movieId,
                cancellationToken);

        if (rating == null)
        {
            return NotFound(new
            {
                message = "Rating not found."
            });
        }

        _context.Ratings.Remove(rating);

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // =========================================================
    // JWT USER ID
    // =========================================================

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    // =========================================================
    // RESPONSE
    // =========================================================

    private static RatingResponse ToResponse(
        Rating rating,
        string? movieTitle)
    {
        return new RatingResponse
        {
            Id = rating.Id,
            UserId = rating.UserId,
            MovieId = rating.MovieId,
            Score = rating.Score,
            CreatedAt = rating.CreatedAt,
            MovieTitle = movieTitle
        };
    }
}