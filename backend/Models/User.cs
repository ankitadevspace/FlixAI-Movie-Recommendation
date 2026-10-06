namespace backend.Models;

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? PreferredGenres { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Rating> Ratings { get; set; }
        = new List<Rating>();

    public ICollection<WatchHistory> WatchHistory { get; set; }
        = new List<WatchHistory>();

    public ICollection<Favorite> Favorites { get; set; }
        = new List<Favorite>();
}