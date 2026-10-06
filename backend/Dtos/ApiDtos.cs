using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

// ============================================================
// USER DTOs
// ============================================================

public class CreateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    public string? PreferredGenres { get; set; }
}

public class UpdateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    public string? PreferredGenres { get; set; }
}

public class UserResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PreferredGenres { get; set; }

    public DateTime CreatedAt { get; set; }
}


// ============================================================
// RATING DTOs
// ============================================================

public class CreateRatingRequest
{
    [Range(1, 5)]
    public double Score { get; set; }

    //[Required]
   // public int UserId { get; set; }

    [Required]
    public int MovieId { get; set; }
}

public class RatingResponse
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int MovieId { get; set; }

    public double Score { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? MovieTitle { get; set; }
}


// ============================================================
// WATCH HISTORY DTOs
// ============================================================

public class CreateWatchHistoryRequest
{
    //[Required]
    //public int UserId { get; set; }

    [Required]
    public int MovieId { get; set; }

    [Range(0, int.MaxValue)]
    public int WatchDurationSeconds { get; set; }

    public bool Completed { get; set; }
}

public class WatchHistoryResponse
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int MovieId { get; set; }

    public string? MovieTitle { get; set; }

    public DateTime WatchedAt { get; set; }

    public int WatchDurationSeconds { get; set; }

    public bool Completed { get; set; }
}


// ============================================================
// FAVORITE DTOs
// ============================================================

public class CreateFavoriteRequest
{
    //[Required]
    //public int UserId { get; set; }

    [Required]
    public int MovieId { get; set; }
}

public class FavoriteResponse
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int MovieId { get; set; }

    public string? MovieTitle { get; set; }

    public DateTime CreatedAt { get; set; }
}