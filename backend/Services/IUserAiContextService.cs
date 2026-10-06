namespace NetflixClone.Services;

public interface IUserAiContextService
{
    Task<UserAiContext> BuildContextAsync(
        int userId,
        CancellationToken cancellationToken = default);
}

public sealed class UserAiContext
{
    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string PreferredGenres { get; set; } = string.Empty;

    public List<string> FavoriteMovies { get; set; } = [];

    public List<string> WatchedMovies { get; set; } = [];

    public List<string> HighlyRatedMovies { get; set; } = [];

    public string ToPromptContext()
    {
        var lines = new List<string>
        {
            $"User: {UserName}",
            $"Preferred genres: {PreferredGenres}"
        };

        if (FavoriteMovies.Count > 0)
        {
            lines.Add(
                $"Favorite movies: {string.Join(", ", FavoriteMovies)}");
        }

        if (WatchedMovies.Count > 0)
        {
            lines.Add(
                $"Recently watched: {string.Join(", ", WatchedMovies)}");
        }

        if (HighlyRatedMovies.Count > 0)
        {
            lines.Add(
                $"Highly rated movies: {string.Join(", ", HighlyRatedMovies)}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}