namespace backend.Models;

public class TvShow
{
    public int Id { get; set; }

    public int TmdbId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Overview { get; set; }

    public string? PosterPath { get; set; }

    public string? BackdropPath { get; set; }

    public string? FirstAirDate { get; set; }

    public string? LastAirDate { get; set; }

    public double? Rating { get; set; }

    public int? NumberOfSeasons { get; set; }

    public int? NumberOfEpisodes { get; set; }

    public string? Genres { get; set; }
}