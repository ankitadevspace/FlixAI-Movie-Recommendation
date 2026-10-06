import React, { useEffect, useRef, useState } from "react";
import "./MovieRow.css";

const TMDB_IMAGE_BASE = "https://image.tmdb.org/t/p/";

const genreMap = {
  28: "Action",
  12: "Adventure",
  16: "Animation",
  35: "Comedy",
  80: "Crime",
  99: "Documentary",
  18: "Drama",
  10751: "Family",
  14: "Fantasy",
  36: "History",
  27: "Horror",
  10402: "Music",
  9648: "Mystery",
  10749: "Romance",
  878: "Sci-Fi",
  10770: "TV Movie",
  53: "Thriller",
  10752: "War",
  37: "Western",
};

function getTitle(movie) {
  return (
    movie?.title ||
    movie?.Title ||
    movie?.name ||
    movie?.Name ||
    "Untitled Movie"
  );
}

function getPoster(movie) {
  const poster =
    movie?.posterPath ||
    movie?.PosterPath ||
    movie?.poster_path ||
    movie?.poster;

  if (!poster) {
    return null;
  }

  if (poster.startsWith("http")) {
    return poster;
  }

  return `${TMDB_IMAGE_BASE}w500${poster}`;
}

function getBackdrop(movie) {
  const backdrop =
    movie?.backdropPath ||
    movie?.BackdropPath ||
    movie?.backdrop_path ||
    movie?.backdrop;

  if (!backdrop) {
    return getPoster(movie);
  }

  if (backdrop.startsWith("http")) {
    return backdrop;
  }

  return `${TMDB_IMAGE_BASE}original${backdrop}`;
}

function getRating(movie) {
  const rating =
    movie?.rating ??
    movie?.Rating ??
    movie?.voteAverage ??
    movie?.VoteAverage ??
    movie?.vote_average;

  if (rating === null || rating === undefined || rating === "") {
    return "—";
  }

  return Number(rating).toFixed(1);
}

function getYear(movie) {
  const date =
    movie?.releaseDate ||
    movie?.ReleaseDate ||
    movie?.release_date ||
    movie?.first_air_date;

  if (!date) {
    return "Movie";
  }

  const year = String(date).substring(0, 4);

  return year || "Movie";
}

function getGenres(movie) {
  const raw =
    movie?.genres ??
    movie?.Genres ??
    movie?.genreIds ??
    movie?.genre_ids;

  if (!raw) {
    return [];
  }

  if (Array.isArray(raw)) {
    return raw
      .map((genre) => {
        if (typeof genre === "number") {
          return genreMap[genre];
        }

        if (typeof genre === "object") {
          return genre.name || genre.Name;
        }

        return String(genre);
      })
      .filter(Boolean)
      .slice(0, 3);
  }

  return String(raw)
    .split(",")
    .map((item) => {
      const value = item.trim();

      if (genreMap[value]) {
        return genreMap[value];
      }

      return value;
    })
    .filter(Boolean)
    .slice(0, 3);
}

function getOverview(movie) {
  return (
    movie?.overview ||
    movie?.Overview ||
    "No description is available for this movie yet."
  );
}

const API_BASE_URL = "http://localhost:5039";

async function getLocalMovieId(movie) {
  // A database movie has both Id and TmdbId.
  const localId = movie?.id ?? movie?.Id;
  const tmdbId = movie?.tmdbId ?? movie?.TmdbId ?? movie?.tmdbID;

  if (localId && tmdbId) {
    return localId;
  }

  if (!tmdbId) {
    throw new Error("TMDb movie ID is missing.");
  }

  const response = await fetch(
    `${API_BASE_URL}/api/movies/tmdb/${tmdbId}`
  );

  if (!response.ok) {
    if (response.status === 404) {
      throw new Error("This movie is not imported into the database yet.");
    }

    throw new Error("Could not find this movie in the database.");
  }

  const localMovie = await response.json();
  const resolvedId = localMovie?.id ?? localMovie?.Id;

  if (!resolvedId) {
    throw new Error("Database movie ID was not returned.");
  }

  return resolvedId;
}

function MoviePlaceholder({ title }) {
  return (
    <div className="movie-placeholder">
      <div className="placeholder-symbol">✦</div>
      <span>{title}</span>
    </div>
  );
}

function MovieCard({ movie, onDetails, onAddToList }) {
  const title = getTitle(movie);
  const poster = getPoster(movie);
  const rating = getRating(movie);
  const year = getYear(movie);
  const genres = getGenres(movie);

  return (
    <article className="movie-card">
      <div className="movie-card-poster">
        {poster ? (
          <img
            src={poster}
            alt={title}
            loading="lazy"
            onError={(event) => {
              event.currentTarget.style.display = "none";
              const placeholder =
                event.currentTarget.parentElement.querySelector(
                  ".movie-placeholder"
                );

              if (placeholder) {
                placeholder.style.display = "flex";
              }
            }}
          />
        ) : null}

        <MoviePlaceholder title={title} />

        <div className="movie-card-gradient" />

        <div className="movie-rating">
          <span>★</span>
          {rating}
        </div>

        <div className="movie-hover">
          <div className="movie-hover-actions">
            <button
              className="card-action play"
              aria-label={`Play ${title}`}
              onClick={() => onDetails(movie)}
            >
              ▶
            </button>

            <button
              className="card-action"
              aria-label={`Add ${title} to My List`}
              onClick={() => onAddToList?.(movie)}
            >
              +
            </button>

            <button
              className="card-action"
              aria-label={`View details for ${title}`}
              onClick={() => onDetails(movie)}
            >
              ⓘ
            </button>
          </div>

          <div className="movie-hover-info">
            <strong>{title}</strong>

            <div className="movie-meta">
              <span className="match-badge">
                {rating !== "—"
                  ? `${Math.round(Number(rating) * 10)}% Match`
                  : "Featured"}
              </span>

              <span>{year}</span>

              {genres.length > 0 && (
                <span>{genres.slice(0, 2).join(" · ")}</span>
              )}
            </div>
          </div>
        </div>
      </div>

      <div className="movie-card-info">
        <h3 title={title}>{title}</h3>

        <div className="movie-card-subtitle">
          <span>{year}</span>

          {genres.length > 0 && (
            <>
              <span className="dot">•</span>
              <span>{genres[0]}</span>
            </>
          )}
        </div>
      </div>
    </article>
  );
}

function MovieDetailsModal({ movie, onClose, onAddToList }) {
  const [trailerKey, setTrailerKey] = useState("");
  const [trailerLoading, setTrailerLoading] = useState(false);
  const [showTrailer, setShowTrailer] = useState(false);

  const movieId =
    movie?.tmdbId ??
    movie?.TmdbId ??
    movie?.tmdbID ??
    movie?.id ??
    movie?.Id;

  useEffect(() => {
    if (!movieId) {
      setTrailerKey("");
      setTrailerLoading(false);
      return undefined;
    }

    const controller = new AbortController();

    const loadTrailer = async () => {
      setTrailerLoading(true);
      setTrailerKey("");
      setShowTrailer(false);

      try {
        const response = await fetch(
          `http://localhost:5039/api/movies/tmdb/${movieId}/videos`,
          { signal: controller.signal }
        );

        if (!response.ok) {
          throw new Error(`Trailer request failed: ${response.status}`);
        }

        const data = await response.json();
        const videos = data?.results || data?.Results || [];

        const trailer = videos.find(
          (video) =>
            String(video.site || "").toLowerCase() === "youtube" &&
            String(video.type || "").toLowerCase() === "trailer" &&
            video.key
        );

        setTrailerKey(trailer?.key || "");
      } catch (error) {
        if (error.name !== "AbortError") {
          console.error("Trailer loading failed:", error);
          setTrailerKey("");
        }
      } finally {
        if (!controller.signal.aborted) {
          setTrailerLoading(false);
        }
      }
    };

    loadTrailer();

    return () => controller.abort();
  }, [movieId]);

  if (!movie) {
    return null;
  }
 
  const title = getTitle(movie);
  const backdrop = getBackdrop(movie);
  const poster = getPoster(movie);
  const rating = getRating(movie);
  const year = getYear(movie);
  const genres = getGenres(movie);
  const overview = getOverview(movie);

  return (
    <div
      className="movie-modal-backdrop"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onClose();
        }
      }}
    >
      <div className="movie-modal">
        <button
          className="movie-modal-close"
          onClick={onClose}
          aria-label="Close movie details"
        >
          ×
        </button>

        <div className="movie-modal-hero">
          {backdrop && (
            <img
              src={backdrop}
              alt=""
              className="movie-modal-backdrop-image"
            />
          )}

          <div className="movie-modal-overlay" />

          <div className="movie-modal-content">
            <div className="movie-modal-poster">
              {poster ? (
                <img src={poster} alt={title} />
              ) : (
                <MoviePlaceholder title={title} />
              )}
            </div>

            <div className="movie-modal-info">
              <span className="modal-eyebrow">FLIXAI DISCOVERY</span>

              <h2>{title}</h2>

              <div className="modal-meta">
                {rating !== "—" && (
                  <span className="modal-rating">★ {rating}</span>
                )}

                <span>{year}</span>

                {genres.length > 0 && (
                  <span>{genres.join(" · ")}</span>
                )}
              </div>

              <p>{overview}</p>

              <div className="modal-actions">
                <button
                  className="modal-play"
                  onClick={() => setShowTrailer(true)}
                  disabled={!trailerKey || trailerLoading}
                  title={
                    trailerLoading
                      ? "Loading trailer"
                      : trailerKey
                        ? "Watch official trailer"
                        : "No YouTube trailer available"
                  }
                >
                  {trailerLoading
                    ? "Loading trailer..."
                    : trailerKey
                      ? "▶ Watch Trailer"
                      : "Trailer Unavailable"}
                </button>

                <button
                  className="modal-list"
                  onClick={() => onAddToList?.(movie)}
                >
                  + My List
                </button>
              </div>
            </div>
          </div>
        </div>

        <div className="movie-modal-bottom">
          <span>FLIXAI</span>
          <p>
            Discover movies based on your taste, mood and viewing history.
          </p>
        </div>

        {showTrailer && trailerKey && (
          <div
            className="trailer-overlay"
            role="dialog"
            aria-modal="true"
            aria-label={`${title} trailer`}
            onMouseDown={(event) => {
              if (event.target === event.currentTarget) {
                setShowTrailer(false);
              }
            }}
          >
            <div className="trailer-container">
              <button
                className="trailer-close"
                onClick={() => setShowTrailer(false)}
                aria-label="Close trailer"
              >
                ×
              </button>
              <iframe
                src={`https://www.youtube-nocookie.com/embed/${encodeURIComponent(trailerKey)}?autoplay=1`}
                title={`${title} trailer`}
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
                referrerPolicy="strict-origin-when-cross-origin"
                allowFullScreen
              />
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

function MovieRow({ title, movies = [], loading = false, onAddToList }) {
  const rowRef = useRef(null);
  const [selectedMovie, setSelectedMovie] = useState(null);

  const scrollRow = (direction) => {
    if (!rowRef.current) {
      return;
    }

    const amount = rowRef.current.clientWidth * 0.82;

    rowRef.current.scrollBy({
      left: direction === "left" ? -amount : amount,
      behavior: "smooth",
    });
  };

  if (!loading && (!movies || movies.length === 0)) {
    return (
      <section className="movie-section movie-section-empty">
        <div className="movie-section-header">
          <div>
            <h2>{title}</h2>
            <p>Nothing here yet.</p>
          </div>
        </div>
      </section>
    );
  }

  return (
    <>
      <section className="movie-section">
        <div className="movie-section-header">
          <div>
            <h2>{title}</h2>

            <p>
              {title === "Trending Now"
                ? "What everyone is watching"
                : title === "Top Rated"
                  ? "Highly rated by movie lovers"
                  : title === "Action"
                    ? "High intensity. No distractions."
                    : title === "Comedy"
                      ? "Something lighter for tonight"
                      : title === "Horror"
                        ? "For when you want something darker"
                        : "Curated for you"}
            </p>
          </div>

          {!loading && movies.length > 0 && (
            <div className="movie-row-controls">
              <button
                className="row-arrow"
                onClick={() => scrollRow("left")}
                aria-label={`Scroll ${title} left`}
              >
                ‹
              </button>

              <button
                className="row-arrow"
                onClick={() => scrollRow("right")}
                aria-label={`Scroll ${title} right`}
              >
                ›
              </button>
            </div>
          )}
        </div>

        <div className="movie-row-wrapper">
          <div className="movie-row" ref={rowRef}>
            {loading
              ? Array.from({ length: 6 }).map((_, index) => (
                  <div className="movie-card skeleton-card" key={index}>
                    <div className="skeleton-poster" />
                    <div className="skeleton-line skeleton-title" />
                    <div className="skeleton-line skeleton-subtitle" />
                  </div>
                ))
              : movies.map((movie, index) => (
                  <MovieCard
                        key={
                        movie?.id ||
                        movie?.Id ||
                        movie?.tmdbId ||
                        movie?.TmdbId ||
                        index
                        }
                            movie={movie}
                            onDetails={setSelectedMovie}
                            onAddToList={onAddToList}
                   />
                ))}
          </div>
        </div>
      </section>

      <MovieDetailsModal
        movie={selectedMovie}
        onClose={() => setSelectedMovie(null)}
        onAddToList={onAddToList}
      />
    </>
  );
}

export default MovieRow;