namespace backend.Models;

public class WatchHistory
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int MovieId { get; set; }

    public DateTime WatchedAt { get; set; } = DateTime.UtcNow;

    public int WatchDurationSeconds { get; set; }

    public bool Completed { get; set; }

    public User? User { get; set; }

    public Movie? Movie { get; set; }
}

