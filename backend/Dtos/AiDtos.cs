namespace NetflixClone.Dtos;

public class MovieSearchRequest
{
    public string Query { get; set; } = string.Empty;

    public int Count { get; set; } = 5;
}

public class MovieSearchResult
{
    public int MovieId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Overview { get; set; }

    public string? Genres { get; set; }

    public string? PosterPath { get; set; }

    public double? Rating { get; set; }

    public double Similarity { get; set; }
}

public class RagRequest
{
    public string Query { get; set; } = string.Empty;

    public int? UserId { get; set; }

    public int Count { get; set; } = 5;
}

public class RagRecommendation
{
    public int MovieId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Overview { get; set; }

    public string? Genres { get; set; }

    public string? PosterPath { get; set; }

    public double? Rating { get; set; }

    public double Similarity { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public class RagResponse
{
    public string Query { get; set; } = string.Empty;

    public int? UserId { get; set; }

    public string Answer { get; set; } = string.Empty;

    public List<RagRecommendation> Recommendations { get; set; } = [];

    public List<MovieSearchResult> Sources { get; set; } = [];

    public string Mode { get; set; } = string.Empty;

    public string UserContext { get; set; } = string.Empty;
}