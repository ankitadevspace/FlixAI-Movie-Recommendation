import React, { useState } from 'react';
import { askRag } from '../services/api';

function AiDiscovery({ onAddToMyList }) {
  const [query, setQuery] = useState('');
  const [response, setResponse] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const suggestions = [
    'Dark science fiction movie about survival',
    'Action movie with a strong hero',
    'Thriller with a mysterious story',
    'Emotional movie with a powerful story',
  ];

  const genreMap = {
    28: 'Action',
    12: 'Adventure',
    16: 'Animation',
    35: 'Comedy',
    80: 'Crime',
    99: 'Documentary',
    18: 'Drama',
    10751: 'Family',
    14: 'Fantasy',
    36: 'History',
    27: 'Horror',
    10402: 'Music',
    9648: 'Mystery',
    10749: 'Romance',
    878: 'Sci-Fi',
    10770: 'TV Movie',
    53: 'Thriller',
    10752: 'War',
    37: 'Western',
  };

  const getReadableGenres = (genres) => {
    if (!genres) {
      return [];
    }

    let values = [];

    if (Array.isArray(genres)) {
      values = genres.map((genre) => {
        if (typeof genre === 'object' && genre !== null) {
          return genre.name || genre.Name || '';
        }

        return String(genre).trim();
      });
    } else {
      values = String(genres)
        .split(',')
        .map((genre) => genre.trim());
    }

    return values
      .map((genre) => genreMap[genre] || genre)
      .filter(Boolean)
      .filter(
        (genre, index, array) =>
          array.findIndex(
            (item) =>
              item.toLowerCase() === genre.toLowerCase()
          ) === index
      )
      .slice(0, 3);
  };

  const getPosterUrl = (posterPath) => {
    if (!posterPath) {
      return null;
    }

    if (
      posterPath.startsWith('http://') ||
      posterPath.startsWith('https://')
    ) {
      return posterPath;
    }

    return `https://image.tmdb.org/t/p/w500${posterPath}`;
  };

  const getSimilarity = (similarity) => {
    const value = Number(similarity);

    if (!Number.isFinite(value)) {
      return 0;
    }

    return Math.max(
      0,
      Math.min(100, Math.round(value * 100))
    );
  };

  const getReason = (movie) => {
    const genres = getReadableGenres(movie.genres);

    if (movie.reason) {
      const reason = String(movie.reason)
        .replace(/\r?\n/g, ' ')
        .trim();

      if (
        reason &&
        !reason.toLowerCase().includes('semantic match')
      ) {
        return reason;
      }
    }

    if (genres.length > 0) {
      return `Matches your request with ${genres
        .slice(0, 2)
        .join(' and ')} elements.`;
    }

    return 'Matches the themes and mood in your request.';
  };

  const handleSearch = async (searchQuery = query) => {
    const trimmedQuery = searchQuery.trim();

    if (!trimmedQuery || loading) {
      return;
    }

    setQuery(trimmedQuery);
    setLoading(true);
    setError('');
    setResponse(null);

    try {
      console.log('Sending RAG request:', {
        query: trimmedQuery,
        userId: 1,
        count: 5,
      });

      const data = await askRag(
        trimmedQuery,
        1,
        5
      );

      console.log('RAG response:', data);

      setResponse(data);
    } catch (err) {
      console.error(
        'AI recommendation error:',
        err
      );

      let message =
        'Unable to get AI recommendations. Please try again.';

      if (err.code === 'ECONNABORTED') {
        message =
          'The AI request took too long. Please try again.';
      } else if (err.response) {
        console.error(
          'Backend status:',
          err.response.status
        );

        console.error(
          'Backend response:',
          err.response.data
        );

        message =
          err.response.data?.message ||
          err.response.data?.error ||
          `AI service returned HTTP ${err.response.status}.`;
      } else if (err.request) {
        message =
          'Could not connect to the AI service. Make sure the backend is running.';
      } else if (err.message) {
        message = err.message;
      }

      setError(message);
      setResponse(null);
    } finally {
      setLoading(false);
    }
  };

  const handleKeyDown = (event) => {
    if (
      event.key === 'Enter' &&
      !loading
    ) {
      handleSearch();
    }
  };

  return (
    <section className="ai3d">

      {/* BACKGROUND DEPTH */}
      <div className="ai3d-background">
        <div className="ai3d-grid" />
        <div className="ai3d-glow ai3d-glow-one" />
        <div className="ai3d-glow ai3d-glow-two" />
        <div className="ai3d-orbit ai3d-orbit-one" />
        <div className="ai3d-orbit ai3d-orbit-two" />
      </div>

      {/* HERO */}
      <div className="ai3d-hero">

        <div className="ai3d-hero-copy">

          <div className="ai3d-eyebrow">
            <span className="ai3d-pulse" />
            FLIXAI INTELLIGENCE
          </div>

          <h2>
            Find a movie
            <span> beyond the obvious.</span>
          </h2>

          <p>
            Tell FlixAI what you feel like watching.
            Semantic search, your preferences and AI
            work together to discover your next movie.
          </p>

        </div>

        {/* AI ORB */}
        <div className="ai3d-orb-container">
          <div className="ai3d-orb-ring ring-one" />
          <div className="ai3d-orb-ring ring-two" />
          <div className="ai3d-orb-ring ring-three" />

          <div className="ai3d-orb">
            <div className="ai3d-orb-core">
              ✦
            </div>
          </div>

          <div className="ai3d-orb-label">
            AI
          </div>
        </div>

      </div>

      {/* SEARCH */}
      <div className="ai3d-search-area">

        <div className="ai3d-search">

          <div className="ai3d-search-icon">
            ✦
          </div>

          <input
            type="text"
            value={query}
            onChange={(event) =>
              setQuery(event.target.value)
            }
            onKeyDown={handleKeyDown}
            placeholder="Describe the movie you're looking for..."
            disabled={loading}
          />

          <button
            type="button"
            onClick={() => handleSearch()}
            disabled={
              loading ||
              !query.trim()
            }
          >
            <span>
              {loading ? 'Thinking' : 'Discover'}
            </span>

            <span className="ai3d-button-arrow">
              →
            </span>
          </button>

        </div>

        {/* SUGGESTIONS */}
        <div className="ai3d-suggestions">

          <span className="ai3d-suggestion-label">
            TRY
          </span>

          {suggestions.map((suggestion) => (
            <button
              key={suggestion}
              type="button"
              onClick={() =>
                handleSearch(suggestion)
              }
              disabled={loading}
            >
              {suggestion}
            </button>
          ))}

        </div>

      </div>

      {/* ERROR */}
      {error && (
        <div className="ai3d-error">
          <div className="ai3d-error-icon">
            !
          </div>

          <div>
            <strong>
              AI discovery failed
            </strong>

            <span>
              {error}
            </span>
          </div>
        </div>
      )}

      {/* LOADING */}
      {loading && (
        <div className="ai3d-loading">

          <div className="ai3d-loading-visual">
            <div />
            <div />
            <div />
          </div>

          <div>
            <strong>
              Searching the cinematic universe
            </strong>

            <span>
              Understanding your request and finding
              the closest matches...
            </span>
          </div>

        </div>
      )}

      {/* RESULTS */}
      {response && !loading && (
        <div className="ai3d-results">

          {/* AI ANSWER */}
          {response.answer && (
            <div className="ai3d-answer">

              <div className="ai3d-answer-orb">
                ✦
              </div>

              <div className="ai3d-answer-content">

                <div className="ai3d-answer-label">
                  FLIXAI'S TAKE
                </div>

                <p>
                  {response.answer}
                </p>

              </div>

            </div>
          )}

          {/* RECOMMENDATIONS */}
          {response.recommendations?.length > 0 && (
            <div className="ai3d-recommendation-section">

              <div className="ai3d-section-heading">

                <div>
                  <span>
                    AI DISCOVERY
                  </span>

                  <h3>
                    Your next obsession
                  </h3>
                </div>

                <div className="ai3d-result-count">
                  {response.recommendations.length}
                  {' '}
                  matches
                </div>

              </div>

              <div className="ai3d-cards">

                {response.recommendations.map(
                  (movie, index) => {

                    const posterUrl =
                      getPosterUrl(
                        movie.posterPath
                      );

                    const genres =
                      getReadableGenres(
                        movie.genres
                      );

                    const similarity =
                      getSimilarity(
                        movie.similarity
                      );

                    return (
                      <article
                        className={`ai3d-card ai3d-card-${index + 1}`}
                        key={movie.movieId}
                      >

                        {/* POSTER */}
                        <div className="ai3d-card-poster">

                          {posterUrl ? (
                            <img
                              src={posterUrl}
                              alt={movie.title}
                              loading="lazy"
                              onError={(event) => {
                                event.currentTarget.style.display =
                                  'none';
                              }}
                            />
                          ) : (
                            <div className="ai3d-no-poster">
                              <span>
                                ✦
                              </span>

                              <small>
                                No artwork
                              </small>
                            </div>
                          )}

                          <div className="ai3d-poster-overlay" />

                          {/* MATCH */}
                          <div className="ai3d-match">

                            <div
                              className="ai3d-match-ring"
                              style={{
                                '--match': `${similarity}%`,
                              }}
                            >
                              <span>
                                {similarity}
                                <small>
                                  %
                                </small>
                              </span>
                            </div>

                            <div>
                              <strong>
                                AI MATCH
                              </strong>

                              <span>
                                semantic fit
                              </span>
                            </div>

                          </div>

                          {/* NUMBER */}
                          <div className="ai3d-card-number">
                            0{index + 1}
                          </div>

                        </div>

                        {/* INFO */}
                        <div className="ai3d-card-info">

                          <div className="ai3d-card-topline">
                            <span>
                              {movie.rating != null
                                ? `★ ${Number(
                                    movie.rating
                                  ).toFixed(1)}`
                                : 'FEATURED'}
                            </span>

                            <span>
                              FLIXAI
                            </span>
                          </div>

                          <h4>
                            {movie.title}
                          </h4>

                          {genres.length > 0 && (
                            <div className="ai3d-genres">
                              {genres.map(
                                (genre) => (
                                  <span
                                    key={genre}
                                  >
                                    {genre}
                                  </span>
                                )
                              )}
                            </div>
                          )}

                          {movie.overview && (
                            <p className="ai3d-overview">
                              {movie.overview}
                            </p>
                          )}

                          <div className="ai3d-reason">
                            <span>
                              WHY THIS MATCHES
                            </span>

                            <p>
                              {getReason(movie)}
                            </p>
                          </div>

                        </div>

                      </article>
                    );
                  }
                )}

              </div>

            </div>
          )}

          {/* NO RESULTS */}
          {(!response.recommendations ||
            response.recommendations.length === 0) && (
            <div className="ai3d-empty">

              <div>
                ✦
              </div>

              <h3>
                Nothing matched this time.
              </h3>

              <p>
                Try describing a mood, genre,
                character or type of story.
              </p>

            </div>
          )}

        </div>
      )}

    </section>
  );
}

export default AiDiscovery;