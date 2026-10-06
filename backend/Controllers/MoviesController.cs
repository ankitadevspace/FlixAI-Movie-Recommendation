using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetflixClone.Services;

namespace NetflixClone.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly ITmdbService _tmdbService;
    private readonly AppDbContext _context;
    private readonly ILogger<MoviesController> _logger;
    private readonly MovieImportService _movieImportService;

    public MoviesController(
        ITmdbService tmdbService,
        AppDbContext context,
        MovieImportService movieImportService,
        ILogger<MoviesController> logger)
    {
        _tmdbService = tmdbService;
        _context = context;
        _movieImportService = movieImportService;
        _logger = logger;
    }

    // =========================================================
    // TMDb IMPORT APIs
    // =========================================================

    // POST: api/movies/import/trending
    [HttpPost("import/trending")]
    public async Task<IActionResult> ImportTrending(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _movieImportService
                .ImportTrendingAsync(cancellationToken);

            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Failed to import trending movies from TMDB.");

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message = "Unable to retrieve movies from TMDB."
                });
        }
    }

    // POST: api/movies/import/top-rated
    [HttpPost("import/top-rated")]
    public async Task<IActionResult> ImportTopRated(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _movieImportService
                .ImportTopRatedAsync(cancellationToken);

            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Failed to import top-rated movies from TMDB.");

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message = "Unable to retrieve movies from TMDB."
                });
        }
    }

    // =========================================================
    // REPAIR INCOMPLETE MOVIE METADATA
    // =========================================================

    // POST: api/movies/repair-metadata
    [HttpPost("repair-metadata")]
    public async Task<IActionResult> RepairMovieMetadata(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _movieImportService
                .RepairIncompleteMoviesAsync(
                    cancellationToken);

            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Failed while repairing movie metadata.");

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "Unable to retrieve movie details from TMDb."
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while repairing movie metadata.");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "An error occurred while repairing movie metadata."
                });
        }
    }

    // =========================================================
    // TMDb APIs
    // =========================================================

    // GET: api/movies/trending
    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending(
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            () => _tmdbService.GetTrendingAsync(cancellationToken));
    }

    // GET: api/movies/tmdb-top-rated
    [HttpGet("tmdb-top-rated")]
    public async Task<IActionResult> GetTmdbTopRated(
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            () => _tmdbService.GetTopRatedAsync(cancellationToken));
    }

    // GET: api/movies/tmdb-genre/28
    [HttpGet("tmdb-genre/{genreId:int}")]
    public async Task<IActionResult> GetMoviesByTmdbGenre(
        int genreId,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            () => _tmdbService.GetMoviesByGenreAsync(
                genreId,
                cancellationToken));
    }

    // GET: api/movies/search?query=batman
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new
            {
                message = "Search query is required."
            });
        }

        return await ExecuteAsync(
            () => _tmdbService.SearchMoviesAsync(
                query,
                cancellationToken));
    }

    // =========================================================
    // PostgreSQL / EF Core APIs
    // =========================================================

    // GET: api/movies
    [HttpGet]
    public async Task<ActionResult> GetMovies(
        [FromQuery] string? search,
        [FromQuery] string? genre,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1 || pageSize > 100)
        {
            pageSize = 20;
        }

        IQueryable<Movie> query =
            _context.Movies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.ToLower();

            query = query.Where(m =>
                m.Title != null &&
                m.Title.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            var genreTerm = genre.ToLower();

            query = query.Where(m =>
                m.Genres != null &&
                m.Genres.ToLower().Contains(genreTerm));
        }

        var totalCount = await query.CountAsync();

        var movies = await query
            .OrderBy(m => m.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            totalCount,
            page,
            pageSize,
            data = movies
        });
    }

    // =========================================================
// RECENTLY ADDED
// =========================================================

// GET: api/movies/recently-added
[HttpGet("recently-added")]
public async Task<IActionResult> GetRecentlyAdded(
    [FromQuery] int count = 20)
{
    if (count < 1 || count > 100)
    {
        count = 20;
    }

    var movies = await _context.Movies
        .AsNoTracking()
        .OrderByDescending(m => m.Id)
        .Take(count)
        .ToListAsync();

    return Ok(movies);
}

    // =========================================================
    // GET MOVIE BY DATABASE ID
    // =========================================================

    // GET: api/movies/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetMovie(int id)
    {
        var movie = await _context.Movies
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
        {
            return NotFound(new
            {
                message = "Movie not found."
            });
        }

        return Ok(movie);
    }

    // =========================================================
    // GET MOVIE BY TMDB ID
    // =========================================================

    // GET: api/movies/tmdb/550
    [HttpGet("tmdb/{tmdbId:int}")]
    public async Task<ActionResult> GetMovieByTmdbId(
        int tmdbId)
    {
        var movie = await _context.Movies
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.TmdbId == tmdbId);

        if (movie == null)
        {
            return NotFound(new
            {
                message = "Movie not found in database."
            });
        }

        return Ok(movie);
    }

    // =========================================================
    // DATABASE TOP RATED
    // =========================================================

    // GET: api/movies/database-top-rated?count=10
    [HttpGet("database-top-rated")]
    public async Task<ActionResult> GetDatabaseTopRated(
        [FromQuery] int count = 10)
    {
        if (count < 1 || count > 100)
        {
            count = 10;
        }

        var movies = await _context.Movies
            .AsNoTracking()
            .Where(m => m.Rating != null)
            .OrderByDescending(m => m.Rating)
            .Take(count)
            .ToListAsync();

        return Ok(movies);
    }

    // =========================================================
    // DATABASE MOVIES BY GENRE
    // =========================================================

    // GET: api/movies/database-genre/Action
    [HttpGet("database-genre/{genre}")]
    public async Task<ActionResult> GetDatabaseMoviesByGenre(
        string genre)
    {
        var genreTerm = genre.ToLower();

        var movies = await _context.Movies
            .AsNoTracking()
            .Where(m =>
                m.Genres != null &&
                m.Genres.ToLower().Contains(genreTerm))
            .OrderByDescending(m => m.Rating)
            .ToListAsync();

        return Ok(movies);
    }

    // =========================================================
    // CREATE SINGLE MOVIE
    // =========================================================

    // POST: api/movies
    [HttpPost]
    public async Task<ActionResult> CreateMovie(
        [FromBody] Movie movie)
    {
        var existingMovie = await _context.Movies
            .FirstOrDefaultAsync(m =>
                m.TmdbId == movie.TmdbId);

        if (existingMovie != null)
        {
            return Conflict(new
            {
                message = "Movie already exists.",
                id = existingMovie.Id
            });
        }

        _context.Movies.Add(movie);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetMovie),
            new { id = movie.Id },
            movie);
    }

    // =========================================================
    // CREATE MOVIES IN BULK
    // =========================================================

    // POST: api/movies/bulk
    [HttpPost("bulk")]
    public async Task<ActionResult> CreateMovies(
        [FromBody] IEnumerable<Movie> movies)
    {
        var movieList = movies.ToList();

        if (movieList.Count == 0)
        {
            return BadRequest(new
            {
                message = "No movies supplied."
            });
        }

        var existingTmdbIds = await _context.Movies
            .Select(m => m.TmdbId)
            .ToListAsync();

        var newMovies = movieList
            .Where(m =>
                !existingTmdbIds.Contains(m.TmdbId))
            .ToList();

        if (newMovies.Count == 0)
        {
            return Conflict(new
            {
                message = "All supplied movies already exist."
            });
        }

        _context.Movies.AddRange(newMovies);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            inserted = newMovies.Count,
            movies = newMovies
        });
    }

    // =========================================================
    // UPDATE MOVIE
    // =========================================================

    // PUT: api/movies/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateMovie(
        int id,
        [FromBody] Movie movie)
    {
        if (id != movie.Id)
        {
            return BadRequest(new
            {
                message =
                    "ID in URL and body do not match."
            });
        }

        var existingMovie = await _context.Movies
            .FirstOrDefaultAsync(m => m.Id == id);

        if (existingMovie == null)
        {
            return NotFound(new
            {
                message = "Movie not found."
            });
        }

        existingMovie.TmdbId = movie.TmdbId;
        existingMovie.Title = movie.Title;
        existingMovie.Overview = movie.Overview;
        existingMovie.PosterPath = movie.PosterPath;
        existingMovie.BackdropPath = movie.BackdropPath;
        existingMovie.ReleaseDate = movie.ReleaseDate;
        existingMovie.Rating = movie.Rating;
        existingMovie.Runtime = movie.Runtime;
        existingMovie.Genres = movie.Genres;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // =========================================================
    // DELETE MOVIE
    // =========================================================

    // DELETE: api/movies/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMovie(int id)
    {
        var movie = await _context.Movies
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
        {
            return NotFound(new
            {
                message = "Movie not found."
            });
        }

        _context.Movies.Remove(movie);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/movies/tmdb/123/videos
    [HttpGet("tmdb/{movieId:int}/videos")]
    public async Task<IActionResult> GetMovieVideos(
        int movieId,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            () => _tmdbService.GetMovieVideosAsync(
                movieId,
                cancellationToken));
    }



    // =========================================================
    // TMDb ERROR HANDLING
    // =========================================================

    private async Task<IActionResult> ExecuteAsync(
        Func<Task<string>> action)
    {
        try
        {
            var result = await action();

            return Content(
                result,
                "application/json");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "External movie service request failed.");

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "The movie provider is temporarily unavailable."
                });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(
                ex,
                "Movie service configuration error.");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "Movie service is not configured correctly."
                });
        }
    }
}