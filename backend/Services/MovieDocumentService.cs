using backend.Models;

namespace NetflixClone.Services;

public sealed class MovieDocumentService
{
    public string BuildDocument(Movie movie)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(movie.Title))
        {
            parts.Add($"Title: {movie.Title}");
        }

        if (!string.IsNullOrWhiteSpace(movie.Overview))
        {
            parts.Add($"Plot: {movie.Overview}");
        }

        if (!string.IsNullOrWhiteSpace(movie.Genres))
        {
            parts.Add($"Genres: {movie.Genres}");
        }

        if (!string.IsNullOrWhiteSpace(movie.ReleaseDate))
        {
            parts.Add($"Release Date: {movie.ReleaseDate}");
        }

        if (movie.Rating.HasValue)
        {
            parts.Add($"Rating: {movie.Rating.Value}");
        }

        return string.Join(
            Environment.NewLine,
            parts);
    }
}