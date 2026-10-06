using System.ComponentModel.DataAnnotations;

namespace NetflixClone.Dtos;

public sealed class SemanticSearchRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(500)]
    public string Query { get; set; } = string.Empty;

    [Range(1, 20)]
    public int Count { get; set; } = 5;
}

public sealed class SemanticSearchResult
{
    public int MovieId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Overview { get; set; }

    public string? PosterPath { get; set; }

    public string? BackdropPath { get; set; }

    public string? ReleaseDate { get; set; }

    public double? Rating { get; set; }

    public string? Genres { get; set; }

    public double Similarity { get; set; }

    public string Content { get; set; } = string.Empty;
}

public sealed class SemanticSearchResponse
{
    public string Query { get; set; } = string.Empty;

    public int Count { get; set; }

    public List<SemanticSearchResult> Results { get; set; } = [];
}