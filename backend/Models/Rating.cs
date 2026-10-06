namespace backend.Models;

public class Rating
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int MovieId { get; set; }

    public double Score { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }

    public Movie? Movie { get; set; }
}