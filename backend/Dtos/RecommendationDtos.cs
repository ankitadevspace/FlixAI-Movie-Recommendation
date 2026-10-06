namespace backend.Dtos;

public class RecommendationResponse
{
    public int MovieId { get; set; }

    public int TmdbId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Overview { get; set; }

    public string? PosterPath { get; set; }

    public string? BackdropPath { get; set; }

    public string? Genres { get; set; }

    public double? Rating { get; set; }

    public double RecommendationScore { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public class RecommendationResult
{
    public int UserId { get; set; }

    public string Strategy { get; set; } = string.Empty;

    public List<RecommendationResponse> Recommendations { get; set; } = [];
}