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
public class FavoritesController : ControllerBase
{
    private readonly AppDbContext _context;

    public FavoritesController(AppDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // ADD FAVORITE
    // POST: api/Favorites
    // ============================================================

    [HttpPost]
    public async Task<ActionResult<FavoriteResponse>> AddFavorite(
        [FromBody] CreateFavoriteRequest request,
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

        var alreadyFavorite = await _context.Favorites
            .AnyAsync(
                f =>
                    f.UserId == userId.Value &&
                    f.MovieId == request.MovieId,
                cancellationToken);

        if (alreadyFavorite)
        {
            return Conflict(new
            {
                message = "Movie is already in favorites."
            });
        }

        var favorite = new Favorite
        {
            UserId = userId.Value,
            MovieId = request.MovieId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Favorites.Add(favorite);

        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetUserFavorites),
            null,
            ToResponse(
                favorite,
                movie.Title));
    }

    // ============================================================
    // GET CURRENT USER FAVORITES
    // GET: api/Favorites
    // ============================================================

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FavoriteResponse>>>
        GetUserFavorites(
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

        var favorites = await _context.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId.Value)
            .Include(f => f.Movie)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FavoriteResponse
            {
                Id = f.Id,
                UserId = f.UserId,
                MovieId = f.MovieId,
                MovieTitle = f.Movie != null
                    ? f.Movie.Title
                    : null,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(favorites);
    }

    // ============================================================
    // REMOVE FAVORITE
    // DELETE: api/Favorites/movie/16
    // ============================================================

    [HttpDelete("movie/{movieId:int}")]
    public async Task<IActionResult> RemoveFavorite(
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

        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(
                f =>
                    f.UserId == userId.Value &&
                    f.MovieId == movieId,
                cancellationToken);

        if (favorite == null)
        {
            return NotFound(new
            {
                message = "Favorite not found."
            });
        }

        _context.Favorites.Remove(favorite);

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ============================================================
    // CHECK WHETHER CURRENT USER FAVORITED A MOVIE
    // GET: api/Favorites/movie/16
    // ============================================================

    [HttpGet("movie/{movieId:int}")]
    public async Task<IActionResult> IsFavorite(
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

        var isFavorite = await _context.Favorites
            .AnyAsync(
                f =>
                    f.UserId == userId.Value &&
                    f.MovieId == movieId,
                cancellationToken);

        return Ok(new
        {
            movieId,
            isFavorite
        });
    }

    // ============================================================
    // JWT USER ID HELPER
    // ============================================================

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

    // ============================================================
    // RESPONSE MAPPER
    // ============================================================

    private static FavoriteResponse ToResponse(
        Favorite favorite,
        string? movieTitle)
    {
        return new FavoriteResponse
        {
            Id = favorite.Id,
            UserId = favorite.UserId,
            MovieId = favorite.MovieId,
            MovieTitle = movieTitle,
            CreatedAt = favorite.CreatedAt
        };
    }
}