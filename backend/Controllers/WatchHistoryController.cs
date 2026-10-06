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
public class WatchHistoryController : ControllerBase
{
    private readonly AppDbContext _context;

    public WatchHistoryController(AppDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // ADD WATCH HISTORY
    // POST: api/WatchHistory
    // ============================================================

    [HttpPost]
    public async Task<ActionResult<WatchHistoryResponse>> AddWatchHistory(
        [FromBody] CreateWatchHistoryRequest request,
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

        var history = new WatchHistory
        {
            UserId = userId.Value,
            MovieId = request.MovieId,
            WatchDurationSeconds = request.WatchDurationSeconds,
            Completed = request.Completed,
            WatchedAt = DateTime.UtcNow
        };

        _context.WatchHistory.Add(history);

        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetHistory),
            new { id = history.Id },
            ToResponse(
                history,
                movie.Title));
    }

    // ============================================================
    // GET HISTORY ITEM
    // GET: api/WatchHistory/1
    // ============================================================

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WatchHistoryResponse>> GetHistory(
        int id,
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

        var history = await _context.WatchHistory
            .AsNoTracking()
            .Include(h => h.Movie)
            .FirstOrDefaultAsync(
                h =>
                    h.Id == id &&
                    h.UserId == userId.Value,
                cancellationToken);

        if (history == null)
        {
            return NotFound(new
            {
                message = "Watch history entry not found."
            });
        }

        return Ok(
            ToResponse(
                history,
                history.Movie?.Title));
    }

    // ============================================================
    // GET CURRENT USER HISTORY
    // GET: api/WatchHistory
    // ============================================================

    [HttpGet]
    public async Task<
        ActionResult<IEnumerable<WatchHistoryResponse>>>
        GetUserHistory(
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

        var history = await _context.WatchHistory
            .AsNoTracking()
            .Where(h => h.UserId == userId.Value)
            .Include(h => h.Movie)
            .OrderByDescending(h => h.WatchedAt)
            .Select(h => new WatchHistoryResponse
            {
                Id = h.Id,
                UserId = h.UserId,
                MovieId = h.MovieId,
                MovieTitle = h.Movie != null
                    ? h.Movie.Title
                    : null,
                WatchedAt = h.WatchedAt,
                WatchDurationSeconds = h.WatchDurationSeconds,
                Completed = h.Completed
            })
            .ToListAsync(cancellationToken);

        return Ok(history);
    }

    // ============================================================
// CONTINUE WATCHING
// GET: api/WatchHistory/continue-watching
// ============================================================

[HttpGet("continue-watching")]
public async Task<ActionResult<IEnumerable<Movie>>> GetContinueWatching(
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

    var history = await _context.WatchHistory
        .AsNoTracking()
        .Where(h =>
            h.UserId == userId.Value &&
            !h.Completed &&
            h.Movie != null)
        .Include(h => h.Movie)
        .OrderByDescending(h => h.WatchedAt)
        .ToListAsync(cancellationToken);

    var movies = history
        .GroupBy(h => h.MovieId)
        .Select(g => g.First().Movie!)
        .Take(20)
        .ToList();

    return Ok(movies);
}

    // ============================================================
    // GET HISTORY FOR A MOVIE
    // GET: api/WatchHistory/movie/12
    // ============================================================

    [HttpGet("movie/{movieId:int}")]
    public async Task<
        ActionResult<IEnumerable<WatchHistoryResponse>>>
        GetMovieHistory(
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

        var history = await _context.WatchHistory
            .AsNoTracking()
            .Where(h =>
                h.UserId == userId.Value &&
                h.MovieId == movieId)
            .Include(h => h.Movie)
            .OrderByDescending(h => h.WatchedAt)
            .Select(h => new WatchHistoryResponse
            {
                Id = h.Id,
                UserId = h.UserId,
                MovieId = h.MovieId,
                MovieTitle = h.Movie != null
                    ? h.Movie.Title
                    : null,
                WatchedAt = h.WatchedAt,
                WatchDurationSeconds = h.WatchDurationSeconds,
                Completed = h.Completed
            })
            .ToListAsync(cancellationToken);

        return Ok(history);
    }

    // ============================================================
    // DELETE HISTORY ITEM
    // DELETE: api/WatchHistory/1
    // ============================================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteHistory(
        int id,
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

        var history = await _context.WatchHistory
            .FirstOrDefaultAsync(
                h =>
                    h.Id == id &&
                    h.UserId == userId.Value,
                cancellationToken);

        if (history == null)
        {
            return NotFound(new
            {
                message = "Watch history entry not found."
            });
        }

        _context.WatchHistory.Remove(history);

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ============================================================
    // DELETE ALL HISTORY FOR CURRENT USER
    // DELETE: api/WatchHistory
    // ============================================================

    [HttpDelete]
    public async Task<IActionResult> DeleteAllHistory(
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

        var history = await _context.WatchHistory
            .Where(h => h.UserId == userId.Value)
            .ToListAsync(cancellationToken);

        if (history.Count == 0)
        {
            return NoContent();
        }

        _context.WatchHistory.RemoveRange(history);

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ============================================================
    // JWT USER ID
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
    // RESPONSE MAPPING
    // ============================================================

    private static WatchHistoryResponse ToResponse(
        WatchHistory history,
        string? movieTitle)
    {
        return new WatchHistoryResponse
        {
            Id = history.Id,
            UserId = history.UserId,
            MovieId = history.MovieId,
            MovieTitle = movieTitle,
            WatchedAt = history.WatchedAt,
            WatchDurationSeconds = history.WatchDurationSeconds,
            Completed = history.Completed
        };
    }
}