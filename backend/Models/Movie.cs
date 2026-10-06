namespace backend.Models
{
    public class Movie
    {
        public int Id { get; set; }

        public int TmdbId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Overview { get; set; }

        public string? PosterPath { get; set; }

        public string? BackdropPath { get; set; }

        public string? ReleaseDate { get; set; }

        public double? Rating { get; set; }

        public int? Runtime { get; set; }

        public string? Genres { get; set; }
    }
}