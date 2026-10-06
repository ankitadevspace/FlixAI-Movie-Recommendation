using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using backend.Data;
using backend.Models;

using Microsoft.EntityFrameworkCore;

namespace NetflixClone.Services;

public class MovieImportService
{
    private readonly ITmdbService _tmdbService;
    private readonly AppDbContext _context;
    private readonly ILogger<MovieImportService> _logger;

    public MovieImportService(
        ITmdbService tmdbService,
        AppDbContext context,
        ILogger<MovieImportService> logger)
    {
        _tmdbService = tmdbService;
        _context = context;
        _logger = logger;
    }

    // ============================================================
    // IMPORT TRENDING
    // ============================================================

    public async Task<ImportResult> ImportTrendingAsync(
        CancellationToken cancellationToken)
    {
        var json = await _tmdbService
            .GetTrendingAsync(cancellationToken);

        return await ImportMoviesFromTmdbJsonAsync(
            json,
            cancellationToken);
    }

    // ============================================================
    // IMPORT TOP RATED
    // ============================================================

    public async Task<ImportResult> ImportTopRatedAsync(
        CancellationToken cancellationToken)
    {
        var json = await _tmdbService
            .GetTopRatedAsync(cancellationToken);

        return await ImportMoviesFromTmdbJsonAsync(
            json,
            cancellationToken);
    }

    // ============================================================
    // COMMON IMPORT LOGIC
    // ============================================================

    private async Task<ImportResult> ImportMoviesFromTmdbJsonAsync(
        string json,
        CancellationToken cancellationToken)
    {
        var response = JsonSerializer.Deserialize<TmdbMovieResponse>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (response?.Results == null ||
            response.Results.Count == 0)
        {
            return new ImportResult
            {
                Received = 0,
                Inserted = 0,
                Skipped = 0
            };
        }

        var tmdbIds = response.Results
            .Select(x => x.Id)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var existingMovies = await _context.Movies
            .Where(m => tmdbIds.Contains(m.TmdbId))
            .ToDictionaryAsync(
                m => m.TmdbId,
                cancellationToken);

        var inserted = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var x in response.Results)
        {
            if (x.Id <= 0)
            {
                skipped++;
                continue;
            }

            // ----------------------------------------------------
            // EXISTING MOVIE
            // ----------------------------------------------------

            if (existingMovies.TryGetValue(
                x.Id,
                out var existingMovie))
            {
                if (!string.IsNullOrWhiteSpace(x.Title))
                {
                    existingMovie.Title = x.Title;
                }
                else if (
                    string.IsNullOrWhiteSpace(existingMovie.Title) &&
                    !string.IsNullOrWhiteSpace(x.OriginalTitle))
                {
                    existingMovie.Title = x.OriginalTitle;
                }

                if (!string.IsNullOrWhiteSpace(x.Overview))
                {
                    existingMovie.Overview = x.Overview;
                }

                if (!string.IsNullOrWhiteSpace(x.PosterPath))
                {
                    existingMovie.PosterPath = x.PosterPath;
                }

                if (!string.IsNullOrWhiteSpace(x.BackdropPath))
                {
                    existingMovie.BackdropPath = x.BackdropPath;
                }

                if (!string.IsNullOrWhiteSpace(x.ReleaseDate))
                {
                    existingMovie.ReleaseDate = x.ReleaseDate;
                }

                if (x.VoteAverage.HasValue)
                {
                    existingMovie.Rating = x.VoteAverage;
                }

                if (x.GenreIds != null &&
                    x.GenreIds.Count > 0)
                {
                    existingMovie.Genres =
                        string.Join(",", x.GenreIds);
                }

                updated++;
            }

            // ----------------------------------------------------
            // NEW MOVIE
            // ----------------------------------------------------

            else
            {
                var title =
                    !string.IsNullOrWhiteSpace(x.Title)
                        ? x.Title
                        : x.OriginalTitle;

                var movie = new Movie
                {
                    TmdbId = x.Id,
                    Title = title ?? string.Empty,
                    Overview = x.Overview,
                    PosterPath = x.PosterPath,
                    BackdropPath = x.BackdropPath,
                    ReleaseDate = x.ReleaseDate,
                    Rating = x.VoteAverage,
                    Runtime = null,
                    Genres =
                        x.GenreIds != null &&
                        x.GenreIds.Count > 0
                            ? string.Join(",", x.GenreIds)
                            : null
                };

                _context.Movies.Add(movie);

                inserted++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "TMDb import completed. " +
            "Received: {Received}, " +
            "Inserted: {Inserted}, " +
            "Updated: {Updated}, " +
            "Skipped: {Skipped}",
            response.Results.Count,
            inserted,
            updated,
            skipped);

        return new ImportResult
        {
            Received = response.Results.Count,
            Inserted = inserted,
            Skipped = skipped
        };
    }

    // ============================================================
    // REPAIR INCOMPLETE MOVIES
    // ============================================================

    public async Task<MetadataRepairResult>
        RepairIncompleteMoviesAsync(
            CancellationToken cancellationToken)
    {
        var movies = await _context.Movies
            .Where(m =>
                string.IsNullOrWhiteSpace(m.Title) ||
                string.IsNullOrWhiteSpace(m.PosterPath) ||
                string.IsNullOrWhiteSpace(m.BackdropPath) ||
                m.Rating == null)
            .ToListAsync(cancellationToken);

        var result = new MetadataRepairResult
        {
            Found = movies.Count
        };

        foreach (var movie in movies)
        {
            try
            {
                if (movie.TmdbId <= 0)
                {
                    result.Skipped++;

                    _logger.LogWarning(
                        "Skipping movie {MovieId} because it has " +
                        "an invalid TMDb ID.",
                        movie.Id);

                    continue;
                }

                _logger.LogInformation(
                    "Repairing metadata for movie {MovieId}, " +
                    "TMDb ID {TmdbId}",
                    movie.Id,
                    movie.TmdbId);

                // ------------------------------------------------
                // STEP 1:
                // Try the existing TMDb ID
                // ------------------------------------------------

                string detailsJson;

                try
                {
                    detailsJson =
                        await _tmdbService.GetMovieDetailsAsync(
                            movie.TmdbId,
                            cancellationToken);
                }
                catch (HttpRequestException ex)
                    when (IsNotFound(ex))
                {
                    // ------------------------------------------------
                    // Existing TMDb ID is no longer available.
                    //
                    // If we have a title, search TMDb and recover
                    // the correct/current movie ID.
                    // ------------------------------------------------

                    if (string.IsNullOrWhiteSpace(movie.Title))
                    {
                        result.Skipped++;

                        _logger.LogWarning(
                            "TMDb ID {TmdbId} was not found for " +
                            "movie {MovieId}, and the movie has no " +
                            "title to perform a fallback search.",
                            movie.TmdbId,
                            movie.Id);

                        continue;
                    }

                    _logger.LogInformation(
                        "TMDb ID {TmdbId} was not found. " +
                        "Searching TMDb using title '{Title}'.",
                        movie.TmdbId,
                        movie.Title);

                    var searchJson =
                        await _tmdbService.SearchMoviesAsync(
                            movie.Title,
                            cancellationToken);

                    var searchResponse =
                        JsonSerializer.Deserialize<TmdbMovieResponse>(
                            searchJson,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                    if (searchResponse?.Results == null ||
                        searchResponse.Results.Count == 0)
                    {
                        result.Failed++;

                        _logger.LogWarning(
                            "No TMDb search results found for " +
                            "movie {MovieId} with title '{Title}'.",
                            movie.Id,
                            movie.Title);

                        continue;
                    }

                    var matchedMovie =
                        FindBestSearchMatch(
                            movie.Title,
                            searchResponse.Results);

                    if (matchedMovie == null)
                    {
                        result.Failed++;

                        _logger.LogWarning(
                            "Could not find a suitable TMDb match " +
                            "for movie {MovieId} with title '{Title}'.",
                            movie.Id,
                            movie.Title);

                        continue;
                    }

                    // ------------------------------------------------
                    // Prevent duplicate TMDb IDs.
                    // ------------------------------------------------

                    var duplicateExists =
                        await _context.Movies
                            .AsNoTracking()
                            .AnyAsync(
                                m =>
                                    m.Id != movie.Id &&
                                    m.TmdbId == matchedMovie.Id,
                                cancellationToken);

                    if (duplicateExists)
                    {
                        result.Skipped++;

                        _logger.LogWarning(
                            "TMDb ID {TmdbId} already belongs to " +
                            "another movie. Skipping repair for " +
                            "movie {MovieId}.",
                            matchedMovie.Id,
                            movie.Id);

                        continue;
                    }

                    _logger.LogInformation(
                        "Recovered TMDb ID for movie {MovieId}: " +
                        "{OldTmdbId} -> {NewTmdbId} ({MatchedTitle})",
                        movie.Id,
                        movie.TmdbId,
                        matchedMovie.Id,
                        matchedMovie.Title);

                    // ------------------------------------------------
                    // Replace stale TMDb ID.
                    // ------------------------------------------------

                    movie.TmdbId = matchedMovie.Id;

                    // ------------------------------------------------
                    // STEP 2:
                    // Fetch full details using recovered ID.
                    // ------------------------------------------------

                    detailsJson =
                        await _tmdbService.GetMovieDetailsAsync(
                            movie.TmdbId,
                            cancellationToken);
                }

                // ------------------------------------------------
                // Deserialize details response
                // ------------------------------------------------

                var details =
                    JsonSerializer.Deserialize<TmdbMovieDetailsDto>(
                        detailsJson,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (details == null)
                {
                    result.Failed++;

                    _logger.LogWarning(
                        "TMDb returned an empty details response " +
                        "for movie {MovieId}, TMDb ID {TmdbId}.",
                        movie.Id,
                        movie.TmdbId);

                    continue;
                }

                // ------------------------------------------------
                // Apply missing metadata
                // ------------------------------------------------

                var changed = false;

                if (string.IsNullOrWhiteSpace(movie.Title))
                {
                    if (!string.IsNullOrWhiteSpace(details.Title))
                    {
                        movie.Title = details.Title;
                        changed = true;
                    }
                    else if (
                        !string.IsNullOrWhiteSpace(
                            details.OriginalTitle))
                    {
                        movie.Title = details.OriginalTitle;
                        changed = true;
                    }
                }

                if (string.IsNullOrWhiteSpace(movie.Overview) &&
                    !string.IsNullOrWhiteSpace(details.Overview))
                {
                    movie.Overview = details.Overview;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(movie.PosterPath) &&
                    !string.IsNullOrWhiteSpace(details.PosterPath))
                {
                    movie.PosterPath = details.PosterPath;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(movie.BackdropPath) &&
                    !string.IsNullOrWhiteSpace(details.BackdropPath))
                {
                    movie.BackdropPath = details.BackdropPath;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(movie.ReleaseDate) &&
                    !string.IsNullOrWhiteSpace(details.ReleaseDate))
                {
                    movie.ReleaseDate = details.ReleaseDate;
                    changed = true;
                }

                if (!movie.Rating.HasValue &&
                    details.VoteAverage.HasValue)
                {
                    movie.Rating = details.VoteAverage;
                    changed = true;
                }

                if (!movie.Runtime.HasValue &&
                    details.Runtime.HasValue)
                {
                    movie.Runtime = details.Runtime;
                    changed = true;
                }

                // ------------------------------------------------
                // Genres
                // ------------------------------------------------

                if (string.IsNullOrWhiteSpace(movie.Genres) &&
                    details.Genres != null &&
                    details.Genres.Count > 0)
                {
                    var genres =
                        string.Join(
                            ",",
                            details.Genres
                                .Where(g => g.Id > 0)
                                .Select(g => g.Id));

                    if (!string.IsNullOrWhiteSpace(genres))
                    {
                        movie.Genres = genres;
                        changed = true;
                    }
                }

                // ------------------------------------------------
                // Save
                // ------------------------------------------------

                if (changed)
                {
                    await _context.SaveChangesAsync(
                        cancellationToken);

                    result.Repaired++;

                    _logger.LogInformation(
                        "Successfully repaired movie {MovieId}, " +
                        "TMDb ID {TmdbId}.",
                        movie.Id,
                        movie.TmdbId);
                }
                else
                {
                    result.Skipped++;

                    _logger.LogWarning(
                        "TMDb details contained no usable missing " +
                        "metadata for movie {MovieId}, " +
                        "TMDb ID {TmdbId}.",
                        movie.Id,
                        movie.TmdbId);
                }
            }

            // ----------------------------------------------------
            // Other HTTP errors
            // ----------------------------------------------------

            catch (HttpRequestException ex)
            {
                result.Failed++;

                _logger.LogError(
                    ex,
                    "Failed to repair movie {MovieId} " +
                    "with TMDb ID {TmdbId}.",
                    movie.Id,
                    movie.TmdbId);
            }

            // ----------------------------------------------------
            // Invalid JSON
            // ----------------------------------------------------

            catch (JsonException ex)
            {
                result.Failed++;

                _logger.LogError(
                    ex,
                    "Invalid TMDb details response for " +
                    "movie {MovieId}, TMDb ID {TmdbId}.",
                    movie.Id,
                    movie.TmdbId);
            }

            // ----------------------------------------------------
            // Unexpected error
            // ----------------------------------------------------

            catch (Exception ex)
            {
                result.Failed++;

                _logger.LogError(
                    ex,
                    "Unexpected error while repairing movie " +
                    "{MovieId}, TMDb ID {TmdbId}.",
                    movie.Id,
                    movie.TmdbId);
            }
        }

        _logger.LogInformation(
            "Movie metadata repair completed. " +
            "Found: {Found}, " +
            "Repaired: {Repaired}, " +
            "Failed: {Failed}, " +
            "Skipped: {Skipped}",
            result.Found,
            result.Repaired,
            result.Failed,
            result.Skipped);

        return result;
    }

    // ============================================================
    // FIND BEST TMDb SEARCH RESULT
    // ============================================================

    private static TmdbMovieDto? FindBestSearchMatch(
        string title,
        List<TmdbMovieDto> results)
    {
        if (results.Count == 0)
        {
            return null;
        }

        var normalizedTitle =
            NormalizeTitle(title);

        // First try exact title match.
        var exactMatch =
            results.FirstOrDefault(
                x =>
                    NormalizeTitle(x.Title) == normalizedTitle ||
                    NormalizeTitle(x.OriginalTitle) ==
                    normalizedTitle);

        if (exactMatch != null)
        {
            return exactMatch;
        }

        // If no exact match exists, use TMDb's first search result.
        return results.FirstOrDefault();
    }

    // ============================================================
    // NORMALIZE TITLE FOR COMPARISON
    // ============================================================

    private static string NormalizeTitle(
        string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        return new string(
                title
                    .Where(char.IsLetterOrDigit)
                    .ToArray())
            .ToLowerInvariant();
    }

    // ============================================================
    // DETECT TMDb 404
    // ============================================================

    private static bool IsNotFound(
        HttpRequestException ex)
    {
        return
            ex.StatusCode == HttpStatusCode.NotFound ||
            ex.Message.Contains(
                "404",
                StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains(
                "Not Found",
                StringComparison.OrdinalIgnoreCase);
    }
}

// ================================================================
// TMDb RESPONSE DTO
// ================================================================

public class TmdbMovieResponse
{
    [JsonPropertyName("results")]
    public List<TmdbMovieDto> Results { get; set; } = [];
}

// ================================================================
// TMDb MOVIE LIST DTO
// ================================================================

public class TmdbMovieDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("original_title")]
    public string? OriginalTitle { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }

    [JsonPropertyName("backdrop_path")]
    public string? BackdropPath { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("vote_average")]
    public double? VoteAverage { get; set; }

    [JsonPropertyName("genre_ids")]
    public List<int>? GenreIds { get; set; }
}

// ================================================================
// TMDb MOVIE DETAILS DTO
// ================================================================

public class TmdbMovieDetailsDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("original_title")]
    public string? OriginalTitle { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }

    [JsonPropertyName("backdrop_path")]
    public string? BackdropPath { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("vote_average")]
    public double? VoteAverage { get; set; }

    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }

    [JsonPropertyName("genres")]
    public List<TmdbGenreDto>? Genres { get; set; }
}

// ================================================================
// TMDb GENRE DTO
// ================================================================

public class TmdbGenreDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

// ================================================================
// IMPORT RESULT
// ================================================================

public class ImportResult
{
    public int Received { get; set; }

    public int Inserted { get; set; }

    public int Skipped { get; set; }
}

// ================================================================
// METADATA REPAIR RESULT
// ================================================================

public class MetadataRepairResult
{
    public int Found { get; set; }

    public int Repaired { get; set; }

    public int Failed { get; set; }

    public int Skipped { get; set; }
}