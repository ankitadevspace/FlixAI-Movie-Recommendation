using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace NetflixClone.Services;

public sealed class UserAiContextService : IUserAiContextService
{
    private readonly AppDbContext _context;

    public UserAiContextService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserAiContext> BuildContextAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == userId,
                cancellationToken);

        if (user == null)
        {
            throw new ArgumentException(
                $"User with ID {userId} was not found.");
        }

        var favoriteMovies = await _context.Favorites
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Include(x => x.Movie)
            .Where(x => x.Movie != null)
            .Select(x => x.Movie!.Title)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Take(10)
            .ToListAsync(cancellationToken);

        var watchedMovies = await _context.WatchHistory
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Include(x => x.Movie)
            .Where(x => x.Movie != null)
            .OrderByDescending(x => x.WatchedAt)
            .Select(x => x.Movie!.Title)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Take(10)
            .ToListAsync(cancellationToken);

        var highlyRatedMovies = await _context.Ratings
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.Score >= 4)
            .Include(x => x.Movie)
            .Where(x => x.Movie != null)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Movie!.Title)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Take(10)
            .ToListAsync(cancellationToken);

        return new UserAiContext
        {
            UserId = user.Id,

            UserName = user.Name,

            PreferredGenres =
                user.PreferredGenres ?? string.Empty,

            FavoriteMovies = favoriteMovies,

            WatchedMovies = watchedMovies,

            HighlyRatedMovies = highlyRatedMovies
        };
    }
}