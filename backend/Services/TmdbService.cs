using System.Net.Http.Headers;

using Microsoft.Extensions.Caching.Distributed;

namespace NetflixClone.Services;

public sealed class TmdbService : ITmdbService
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TmdbService> _logger;

    public TmdbService(
        HttpClient httpClient,
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<TmdbService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;

        var token = _configuration["TMDb:ReadAccessToken"];

        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // =========================================================
    // TRENDING MOVIES + TV
    // =========================================================

    public Task<string> GetTrendingAsync(
        CancellationToken cancellationToken = default)
    {
        return GetCachedAsync(
            "tmdb:trending",
            "trending/all/week",
            cancellationToken);
    }

    // =========================================================
    // TOP RATED MOVIES
    // =========================================================

    public Task<string> GetTopRatedAsync(
        CancellationToken cancellationToken = default)
    {
        return GetCachedAsync(
            "tmdb:top-rated",
            "movie/top_rated",
            cancellationToken);
    }

    // =========================================================
    // MOVIES BY GENRE
    // =========================================================

    public Task<string> GetMoviesByGenreAsync(
        int genreId,
        CancellationToken cancellationToken = default)
    {
        return GetCachedAsync(
            $"tmdb:genre:{genreId}",
            $"discover/movie?with_genres={genreId}",
            cancellationToken);
    }

    // =========================================================
    // SEARCH MOVIES
    // =========================================================

    public Task<string> SearchMoviesAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim();

        return GetCachedAsync(
            $"tmdb:search:{normalizedQuery.ToLowerInvariant()}",
            $"search/movie?query={Uri.EscapeDataString(normalizedQuery)}",
            cancellationToken);
    }

    // =========================================================
    // MOVIE DETAILS
    // =========================================================

    public Task<string> GetMovieDetailsAsync(
        int movieId,
        CancellationToken cancellationToken = default)
    {
        return GetCachedAsync(
            $"tmdb:movie-details:{movieId}",
            $"movie/{movieId}",
            cancellationToken);
    }

    // =========================================================
    // MOVIE VIDEOS (TRAILERS)
    // =========================================================

    public Task<string> GetMovieVideosAsync(
        int movieId,
        CancellationToken cancellationToken = default)
    {
        return GetCachedAsync(
            $"tmdb:movie-videos:{movieId}",
            $"movie/{movieId}/videos",
            cancellationToken);
    }

    // =========================================================
    // TV SEARCH
    // =========================================================

    public Task<string> SearchTvShowsAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim();

        return GetCachedAsync(
            $"tmdb:tv-search:{normalizedQuery.ToLowerInvariant()}",
            $"search/tv?query={Uri.EscapeDataString(normalizedQuery)}",
            cancellationToken);
    }

    // =========================================================
    // TV DETAILS
    // =========================================================

    public Task<string> GetTvShowDetailsAsync(
        int tvId,
        CancellationToken cancellationToken = default)
    {
        return GetCachedAsync(
            $"tmdb:tv-details:{tvId}",
            $"tv/{tvId}",
            cancellationToken);
    }

    // =========================================================
    // GENERIC CACHED HTTP REQUEST
    // =========================================================

    private async Task<string> GetCachedAsync(
        string cacheKey,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var cachedData = await _cache.GetStringAsync(
            cacheKey,
            cancellationToken);

        if (cachedData is not null)
        {
            _logger.LogDebug(
                "TMDB distributed cache hit for {CacheKey}",
                cacheKey);

            return cachedData;
        }

        var token =
            _configuration["TMDb:ReadAccessToken"];

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "TMDB Read Access Token is not configured. " +
                "Set TMDb:ReadAccessToken using user-secrets " +
                "or an environment variable.");
        }

        _logger.LogInformation(
            "Calling TMDB endpoint {Endpoint}",
            relativePath);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                relativePath);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));

        using var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        var content =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "TMDB request failed with status {StatusCode} " +
                "for {Endpoint}. Response: {Response}",
                (int)response.StatusCode,
                relativePath,
                content);

            throw new HttpRequestException(
                $"TMDB returned {(int)response.StatusCode} " +
                $"({response.ReasonPhrase}).");
        }

        var cacheMinutes =
            _configuration.GetValue(
                "Caching:DefaultMinutes",
                10);

        await _cache.SetStringAsync(
            cacheKey,
            content,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromMinutes(cacheMinutes)
            },
            cancellationToken);

        return content;
    }
}