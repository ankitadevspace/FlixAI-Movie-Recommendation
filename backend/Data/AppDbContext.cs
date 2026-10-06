using backend.Models;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Movie> Movies => Set<Movie>();

    public DbSet<TvShow> TvShows => Set<TvShow>();

    public DbSet<MovieEmbedding> MovieEmbeddings { get; set; }

    public DbSet<User> Users => Set<User>();

    public DbSet<Rating> Ratings => Set<Rating>();

    public DbSet<WatchHistory> WatchHistory => Set<WatchHistory>();

    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =====================================================
        // USER
        // =====================================================

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // =====================================================
        // RATING
        // =====================================================

        modelBuilder.Entity<Rating>()
            .HasIndex(r => new
            {
                r.UserId,
                r.MovieId
            })
            .IsUnique();

        modelBuilder.Entity<Rating>()
            .Property(r => r.Score)
            .HasPrecision(3, 1);

        modelBuilder.Entity<Rating>()
            .HasOne(r => r.User)
            .WithMany(u => u.Ratings)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Rating>()
            .HasOne(r => r.Movie)
            .WithMany()
            .HasForeignKey(r => r.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        // =====================================================
        // WATCH HISTORY
        // =====================================================

        modelBuilder.Entity<WatchHistory>()
            .HasIndex(w => new
            {
                w.UserId,
                w.MovieId
            });

        modelBuilder.Entity<WatchHistory>()
            .HasIndex(w => w.WatchedAt);

        modelBuilder.Entity<WatchHistory>()
            .HasOne(w => w.User)
            .WithMany(u => u.WatchHistory)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WatchHistory>()
            .HasOne(w => w.Movie)
            .WithMany()
            .HasForeignKey(w => w.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        // =====================================================
        // FAVORITES
        // =====================================================

        modelBuilder.Entity<Favorite>()
            .HasIndex(f => new
            {
                f.UserId,
                f.MovieId
            })
            .IsUnique();

        modelBuilder.Entity<Favorite>()
            .HasOne(f => f.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Favorite>()
            .HasOne(f => f.Movie)
            .WithMany()
            .HasForeignKey(f => f.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

          // =====================================================
          //  TV SHOWS
          // =====================================================

        modelBuilder.Entity<TvShow>()
        .HasIndex(t => t.TmdbId)
        .IsUnique();    

        // =====================================================
        // PGVECTOR / MOVIE EMBEDDINGS
        // =====================================================

        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<MovieEmbedding>()
            .Property(x => x.Embedding)
            .HasColumnType("vector(768)");

        modelBuilder.Entity<MovieEmbedding>()
            .HasIndex(x => x.MovieId)
            .IsUnique();

        modelBuilder.Entity<MovieEmbedding>()
            .HasOne(x => x.Movie)
            .WithMany()
            .HasForeignKey(x => x.MovieId)
            .OnDelete(DeleteBehavior.Cascade);
    }
} 