using Pgvector;

namespace backend.Models;

public class MovieEmbedding
{
    public int Id { get; set; }

    public int MovieId { get; set; }

    public string Content { get; set; } = string.Empty;

    public Vector Embedding { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Movie Movie { get; set; } = null!;
} 