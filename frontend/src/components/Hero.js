import React from 'react';
import './Hero.css';

const IMAGE_BASE =
  'https://image.tmdb.org/t/p/original';

function Hero({ movie, loading }) {
  if (loading || !movie) {
    return (
      <section className="hero hero-loading">
        <div className="hero-loading-orb" />
        <div className="hero-loading-content">
          <div className="hero-loading-line small" />
          <div className="hero-loading-line large" />
          <div className="hero-loading-line medium" />
        </div>
      </section>
    );
  }

  const title =
    movie.title ||
    movie.name ||
    movie.original_name ||
    'Featured Movie';

  const backdrop =
    movie.backdrop_path ||
    movie.backdropPath;

  const overview =
    movie.overview ||
    'Discover something new to watch.';

  const year =
    movie.release_date?.slice(0, 4) ||
    movie.first_air_date?.slice(0, 4);

  const rating =
    movie.vote_average !== undefined &&
    movie.vote_average !== null
      ? Number(movie.vote_average).toFixed(1)
      : null;

  return (
    <section
      className="hero"
      style={
        backdrop
          ? {
              '--hero-image': `url(${IMAGE_BASE}${backdrop})`,
            }
          : undefined
      }
    >
      {/* Cinematic background */}
      <div className="hero-background" />

      <div className="hero-purple-glow" />
      <div className="hero-vignette" />

      {/* Decorative atmosphere */}
      <div className="hero-orb hero-orb-one" />
      <div className="hero-orb hero-orb-two" />

      <div className="hero-content">

        {/* AI / FEATURED LABEL */}
        <div className="hero-kicker">
          <span className="hero-kicker-icon">✦</span>
          <span>AI CURATED FOR YOU</span>
        </div>

        {/* TITLE */}
        <h1>{title}</h1>

        {/* META */}
        <div className="hero-meta">

          <span className="hero-match">
            <span className="match-dot" />
            98% Match
          </span>

          {year && (
            <span className="hero-meta-item">
              {year}
            </span>
          )}

          {rating && (
            <span className="hero-rating">
              <span>★</span>
              {rating}
            </span>
          )}

        </div>

        {/* DESCRIPTION */}
        <p className="hero-description">
          {overview}
        </p>

        {/* ACTIONS */}
        <div className="hero-actions">

          <button
            type="button"
            className="primary-action"
          >
            <span>▶</span>
            Play
          </button>

          <button
            type="button"
            className="secondary-action"
          >
            <span>＋</span>
            My List
          </button>

          <button
            type="button"
            className="secondary-action details-action"
          >
            <span>ⓘ</span>
            Details
          </button>

        </div>

        {/* AI INSIGHT */}
        <div className="hero-ai-insight">

          <div className="hero-ai-icon">
            ✦
          </div>

          <div className="hero-ai-text">
            <span>FLIXAI INSIGHT</span>

            <p>
              Selected from your discovery feed
              based on what you're exploring.
            </p>
          </div>

          <div className="hero-ai-pulse" />

        </div>

      </div>

      {/* Bottom cinematic fade */}
      <div className="hero-bottom-fade" />
    </section>
  );
}

export default Hero;