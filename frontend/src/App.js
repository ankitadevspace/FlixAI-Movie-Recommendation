import React, { useEffect, useMemo, useState } from 'react';

import api from './services/api';

import Sidebar from './components/Sidebar';
import Hero from './components/Hero';
import MovieRow from './components/MovieRow';
import SearchBar from './components/SearchBar';
import AiDiscovery from './components/AiDiscovery';

import './App.css';

const sections = [
  {
    key: 'trending',
    title: 'Trending Now',
    subtitle: 'What everyone is watching',
    endpoint: '/movies/trending',
  },
  {
    key: 'top',
    title: 'Top Rated',
    subtitle: 'Highly rated by movie lovers',
    endpoint: '/movies/tmdb-top-rated',
  },
  {
    key: 'action',
    title: 'Action',
    subtitle: 'High intensity. No distractions.',
    endpoint: '/movies/tmdb-genre/28',
  },
  {
    key: 'comedy',
    title: 'Comedy',
    subtitle: 'Something lighter for tonight',
    endpoint: '/movies/tmdb-genre/35',
  },
  {
    key: 'horror',
    title: 'Horror',
    subtitle: 'For when you want something darker',
    endpoint: '/movies/tmdb-genre/27',
  },
];

function normalizeMovies(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.results)) return data.results;
  if (Array.isArray(data?.movies)) return data.movies;
  return [];
}

function App() {
  const [active, setActive] = useState('Home');
  const [searchResults, setSearchResults] = useState([]);
  const [rows, setRows] = useState({});
  const [heroMovie, setHeroMovie] = useState(null);
  const [loading, setLoading] = useState(true);

  const [recentlyAdded, setRecentlyAdded] = useState([]); //recentwatch i added this 
  const [recentlyAddedLoading, setRecentlyAddedLoading] = useState(false);
  const [recentlyAddedError, setRecentlyAddedError] = useState('');

  

  const [continueWatching, setContinueWatching] = useState([]);
  const [continueWatchingLoading, setContinueWatchingLoading] = useState(false);
  const [continueWatchingError, setContinueWatchingError] = useState('');

  

  const [myList, setMyList] = useState([]);
  const [myListLoading, setMyListLoading] = useState(false);
  const [myListError, setMyListError] = useState('');

  const [showAuth, setShowAuth] = useState(false);
  const [authMode, setAuthMode] = useState('login');
  const [authName, setAuthName] = useState('');
  const [authEmail, setAuthEmail] = useState('');
  const [authPassword, setAuthPassword] = useState('');
  const [authLoading, setAuthLoading] = useState(false);
  const [authError, setAuthError] = useState('');
  const [currentUser, setCurrentUser] = useState(() => {
    try {
      return JSON.parse(localStorage.getItem('flixai_user') || 'null');
    } catch {
      return null;
    }
  });

  const loadRecentlyAdded = async () => {
  setRecentlyAddedLoading(true);
  setRecentlyAddedError('');

  try {
    const response = await api.get('/movies/recently-added?count=20');

    setRecentlyAdded(
      Array.isArray(response.data)
        ? response.data
        : response.data?.data || []
    );
  } catch (error) {
    console.error('Failed to load Recently Added:', error);
    setRecentlyAddedError(
      'Could not load recently added movies. Please try again.'
    );
  } finally {
    setRecentlyAddedLoading(false);
  }
};

const loadContinueWatching = async () => {
  if (!localStorage.getItem('flixai_token')) {
    setContinueWatching([]);
    setContinueWatchingError('Please log in to view Continue Watching.');
    return;
  }

  setContinueWatchingLoading(true);
  setContinueWatchingError('');

  try {
    const response = await api.get('/WatchHistory/continue-watching');

    setContinueWatching(
      Array.isArray(response.data)
        ? response.data
        : response.data?.data || []
    );
  } catch (error) {
    console.error('Failed to load Continue Watching:', error);

    setContinueWatchingError(
      error.response?.status === 401
        ? 'Your session has expired. Please log in again.'
        : 'Could not load Continue Watching. Please try again.'
    );
  } finally {
    setContinueWatchingLoading(false);
  }
};


  // Load homepage sections.
  useEffect(() => {
    let cancelled = false;

    async function loadHome() {
      setLoading(true);

      try {
        const responses = await Promise.allSettled(
          sections.map((section) => api.get(section.endpoint))
        );

        const nextRows = {};

        responses.forEach((result, index) => {
          const section = sections[index];

          if (result.status === 'fulfilled') {
            nextRows[section.key] = normalizeMovies(result.value.data);
          } else {
            nextRows[section.key] = [];
            console.error(`Failed to load ${section.title}`, result.reason);
          }
        });

        if (!cancelled) {
          setRows(nextRows);
          const trending = nextRows.trending || [];
          if (trending.length > 0) setHeroMovie(trending[0]);
        }
      } catch (error) {
        console.error('Failed to load FLIXAI home page', error);
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    loadHome();

    return () => {
      cancelled = true;
    };
  }, []);

  // Load the logged-in user's favorites and match them to local movie records.
  const loadMyList = async () => {
    if (!localStorage.getItem('flixai_token')) {
      setMyList([]);
      setMyListError('Please log in to view your list.');
      return;
    }

    setMyListLoading(true);
    setMyListError('');

    try {
      const [favoritesResponse, moviesResponse] = await Promise.all([
        api.get('/Favorites'),
        api.get('/movies?page=1&pageSize=100'),
      ]);

      const favorites = Array.isArray(favoritesResponse.data)
        ? favoritesResponse.data
        : [];

      const movies = moviesResponse.data?.data || [];

      const moviesById = new Map(
        movies.map((movie) => [Number(movie.id ?? movie.Id), movie])
      );

      setMyList(
        favorites
          .map((favorite) =>
            moviesById.get(Number(favorite.movieId ?? favorite.MovieId))
          )
          .filter(Boolean)
      );
    } catch (error) {
      console.error('Failed to load My List:', error);
      setMyListError(
        error.response?.status === 401
          ? 'Your session has expired. Please log in again.'
          : 'Could not load My List. Please try again.'
      );
    } finally {
      setMyListLoading(false);
    }
  };

  // Login or register using the existing backend endpoints.
  const handleAuthSubmit = async (event) => {
    event.preventDefault();
    setAuthLoading(true);
    setAuthError('');

    try {
      const endpoint = authMode === 'register' ? '/auth/register' : '/auth/login';

      const payload =
        authMode === 'register'
          ? {
              name: authName.trim(),
              email: authEmail.trim(),
              password: authPassword,
            }
          : {
              email: authEmail.trim(),
              password: authPassword,
            };

      const response = await api.post(endpoint, payload);
      const data = response.data;

      if (!data?.token) {
        throw new Error('The server response did not contain a token.');
      }

      const user = {
        userId: data.userId,
        name: data.name,
        email: data.email,
      };

      localStorage.setItem('flixai_token', data.token);
      localStorage.setItem('flixai_user', JSON.stringify(user));

      setCurrentUser(user);
      setShowAuth(false);
      setAuthPassword('');
      setAuthError('');

      if (active === 'My List') {
        await loadMyList();
      }
    } catch (error) {
      console.error('Authentication failed:', error);

      setAuthError(
        error.response?.data?.message ||
          error.response?.data?.title ||
          error.message ||
          'Login or registration failed. Please try again.'
      );
    } finally {
      setAuthLoading(false);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('flixai_token');
    localStorage.removeItem('flixai_user');

    setCurrentUser(null);
    setMyList([]);
    setMyListError('');
    setShowAuth(false);
    setActive('Home');
  };

  const handleAddToList = async (movie) => {
    if (!localStorage.getItem('flixai_token')) {
      setAuthMode('login');
      setAuthError('Please log in to add movies to My List.');
      setShowAuth(true);
      return;
    }

    try {
      const tmdbId =
        movie.tmdbId ??
        movie.TmdbId ??
        movie.tmdbID ??
        movie.id ??
        movie.Id;

      if (!tmdbId) {
        alert('Could not identify this movie.');
        return;
      }

      const movieResponse = await api.get(`/movies/tmdb/${tmdbId}`);
      const localMovieId =
        movieResponse.data?.id ?? movieResponse.data?.Id;

      if (!localMovieId) {
        alert('This movie is not available in the database yet.');
        return;
      }

      await api.post('/Favorites', { movieId: localMovieId });

      if (active === 'My List') await loadMyList();

      alert('Added to My List.');
    } catch (error) {
      if (error.response?.status === 409) {
        alert('This movie is already in My List.');
        if (active === 'My List') await loadMyList();
      } else if (error.response?.status === 401) {
        setAuthMode('login');
        setAuthError('Your session has expired. Please log in again.');
        setShowAuth(true);
      } else if (error.response?.status === 404) {
        alert('This movie is not in your local database yet. Import it first.');
      } else {
        console.error('Failed to add movie:', error);
        alert('Could not add this movie. Please try again.');
      }
    }
  };
  //Continue watch button needs to save trailers
  const handleWatched = async (movie, seconds = 30, completed = false) => {
  if (!localStorage.getItem('flixai_token')) return; // only logged-in users

  try {
    const tmdbId = movie.tmdbId ?? movie.TmdbId ?? movie.id ?? movie.Id;

    // WatchHistory needs YOUR database movie id, not the TMDB id
    const movieResponse = await api.get(`/movies/tmdb/${tmdbId}`);
    const localMovieId = movieResponse.data?.id ?? movieResponse.data?.Id;
    if (!localMovieId) return;

    await api.post('/WatchHistory', {
      movieId: localMovieId,
      watchDurationSeconds: seconds,
      completed,
    });
  } catch (error) {
    console.error('Failed to save watch history:', error);
  }
};

  const handleRemoveFromList = async (movie) => {
    const localMovieId = movie.id ?? movie.Id;
    if (!localMovieId) return;

    try {
      await api.delete(`/Favorites/movie/${localMovieId}`);
      setMyList((current) =>
        current.filter(
          (item) => Number(item.id ?? item.Id) !== Number(localMovieId)
        )
      );
    } catch (error) {
      console.error('Failed to remove movie:', error);
      alert('Could not remove this movie. Please try again.');
    }
  };

  const showSearch = searchResults.length > 0;

  const totalMovies = useMemo(() => {
    const uniqueMovieIds = new Set();

    Object.values(rows).forEach((movies) => {
      movies.forEach((movie) => {
        const id =
          movie.id ??
          movie.movieId ??
          movie.tmdbId ??
          movie.tmdb_id;

        if (id !== undefined && id !== null) {
          uniqueMovieIds.add(String(id));
        }
      });
    });

    return uniqueMovieIds.size;
  }, [rows]);

  const handleNavigate = (page) => {
    setActive(page);

    if (page === 'My List') loadMyList();

    if (page === 'Recently Added') loadRecentlyAdded();

    if (page === 'Continue Watching') loadContinueWatching();

    if (page !== 'Home') {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  };

  
  const focusSearch = () => {
    document.querySelector('.search-input')?.focus();
  };

  const clearSearch = () => {
    setSearchResults([]);
    setActive('Home');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  return (
    <div className="app-shell">
      <div className="ambient ambient-one" />
      <div className="ambient ambient-two" />

      <Sidebar
        active={active}
        onNavigate={handleNavigate}
        onSearchFocus={focusSearch}
     />

      <main className="main-content">
        <header className="topbar">
          <div className="brand-mobile">
            FLIX<span>AI</span>
          </div>

          <SearchBar onResults={setSearchResults} />

          <div className="topbar-actions">
            <button
              className="ai-top-button"
              onClick={focusSearch}
              type="button"
            >
              <span className="ai-spark">✦</span>
              <span>Ask AI</span>
            </button>

            <button
              className="profile-button"
              aria-label="Profile"
              type="button"
              onClick={() => {
                setAuthError('');
                setShowAuth(true);
              }}
            >
              {currentUser?.name?.charAt(0)?.toUpperCase() || 'A'}
            </button>
          </div>
        </header>

        {active === 'My List' ? (
          <section className="content-section">
            <div className="movie-section-heading">
              <div>
                <h2>My List</h2>
                <p>Your saved movies</p>
              </div>

              <button
                type="button"
                className="view-all-button"
                onClick={loadMyList}
              >
                Refresh <span>↻</span>
              </button>
            </div>

            {myListLoading ? (
              <p>Loading your list...</p>
            ) : myListError ? (
              <p>{myListError}</p>
            ) : myList.length === 0 ? (
              <p>Your list is empty. Add movies using the + button.</p>
            ) : (
              <MovieRow
                title=""
                movies={myList}
                onAddToList={handleAddToList}
                onRemoveFromList={handleRemoveFromList}
              />
            )}
          </section>
          ) : active === 'Recently Added' ? (
  <section className="content-section">

    <div className="movie-section-heading">
      <div>
        <h2>Recently Added</h2>
        <p>Fresh additions to the FLIXAI library</p>
      </div>

      <button
        type="button"
        className="view-all-button"
        onClick={loadRecentlyAdded}
      >
        Refresh <span>↻</span>
      </button>
    </div>

    {recentlyAddedLoading ? (
      <p>Loading recently added movies...</p>
    ) : recentlyAddedError ? (
      <p>{recentlyAddedError}</p>
    ) : recentlyAdded.length === 0 ? (
      <p>No recently added movies yet.</p>
    ) : (
      <MovieRow
        title=""
        movies={recentlyAdded}
        onAddToList={handleAddToList}
      />
    )}

  </section>
  ) : active === 'Continue Watching' ? (
  <section className="content-section">
    <div className="movie-section-heading">
      <div>
        <h2>Continue Watching</h2>
        <p>Pick up where you left off</p>
      </div>
      <button type="button" className="view-all-button" onClick={loadContinueWatching}>
        Refresh <span>↻</span>
      </button>
    </div>

    {continueWatchingLoading ? (
      <p>Loading...</p>
    ) : continueWatchingError ? (
      <p>{continueWatchingError}</p>
    ) : continueWatching.length === 0 ? (
      <p>Nothing to continue watching yet. Play a movie while logged in.</p>
    ) : (
      <MovieRow title="" movies={continueWatching} onAddToList={handleAddToList} />
    )}
  </section>
          ) : active === 'AI Picks' ? (
            <section className="content-section">
            <AiDiscovery onAddToMyList={handleAddToList} />
            </section>
            ) : active === 'Discover' ? (
           <section className="content-section">
           <h2>Discover Movies</h2>

    <div className="movie-sections">
      {sections.map((section) => (
        <div className="movie-section-wrapper" key={section.key}>
          <MovieRow
            title={section.title}
            movies={rows[section.key] || []}
            loading={loading}
            onAddToList={handleAddToList}
          />
        </div>
      ))}
    </div>
  </section>
        ) : showSearch ? (
          <section className="search-page">
            <div className="search-result-header">
              <div>
                <span className="eyebrow">FLIXAI SEMANTIC SEARCH</span>
                <h1>Movies that match your mood.</h1>
                <p>
                  Results are generated using meaning-based movie search
                  rather than simple keyword matching.
                </p>
              </div>

              <button
                className="clear-search"
                onClick={clearSearch}
                type="button"
              >
                Back to Home
              </button>
            </div>

            <MovieRow
              title="AI Search Results"
              movies={searchResults}
              onAddToList={handleAddToList}
            />
          </section>
        ) : (
          <>
            <Hero movie={heroMovie} loading={loading} />

            <section className="content-section">
              <AiDiscovery />

              {!loading && totalMovies > 0 && (
                <div className="library-status">
                  <div className="status-line" />
                  <span>FLIXAI LIBRARY</span>
                  <strong>{totalMovies}+ titles discovered</strong>
                  <div className="status-line" />
                </div>
              )}

              <div className="movie-sections">
                {sections.map((section) => (
                  <div
                    className="movie-section-wrapper"
                    key={section.key}
                  >
                    <div className="movie-section-heading">
                      <div>
                        <h2>{section.title}</h2>
                        <p>{section.subtitle}</p>
                      </div>

                      <button
                        type="button"
                        className="view-all-button"
                        onClick={() => setActive(section.key)}
                      >
                        View all <span>→</span>
                      </button>
                    </div>

                    <MovieRow
                      title=""
                      movies={rows[section.key] || []}
                      loading={loading}
                      onAddToList={handleAddToList}
                    />
                  </div>
                ))}
              </div>
            </section>
          </>
        )}
      </main>

      {showAuth && (
        <div
          className="auth-overlay"
          onClick={(event) => {
            if (event.target === event.currentTarget) {
              setShowAuth(false);
            }
          }}
        >
          <section className="auth-panel">
            <button
              className="auth-close"
              type="button"
              aria-label="Close"
              onClick={() => setShowAuth(false)}
            >
              ×
            </button>

            <span className="eyebrow">FLIXAI ACCOUNT</span>
            <h2>{currentUser ? 'Your account' : authMode === 'login' ? 'Welcome back' : 'Create account'}</h2>

            {currentUser ? (
              <>
                <p>Signed in as {currentUser.email}</p>
                <button
                  className="auth-submit"
                  type="button"
                  onClick={handleLogout}
                >
                  Log out
                </button>
              </>
            ) : (
              <form onSubmit={handleAuthSubmit}>
                {authMode === 'register' && (
                  <input
                    type="text"
                    placeholder="Name"
                    value={authName}
                    onChange={(event) => setAuthName(event.target.value)}
                    required
                  />
                )}

                <input
                  type="email"
                  placeholder="Email"
                  value={authEmail}
                  onChange={(event) => setAuthEmail(event.target.value)}
                  required
                />

                <input
                  type="password"
                  placeholder="Password"
                  value={authPassword}
                  onChange={(event) => setAuthPassword(event.target.value)}
                  required
                />

                {authError && <p className="auth-error">{authError}</p>}

                <button
                  className="auth-submit"
                  type="submit"
                  disabled={authLoading}
                >
                  {authLoading
                    ? 'Please wait...'
                    : authMode === 'login'
                      ? 'Log in'
                      : 'Create account'}
                </button>

                <p className="auth-switch">
                  {authMode === 'login'
                    ? "Don't have an account?"
                    : 'Already have an account?'}{' '}
                  <button
                    type="button"
                    onClick={() => {
                      setAuthMode(authMode === 'login' ? 'register' : 'login');
                      setAuthError('');
                    }}
                  >
                    {authMode === 'login' ? 'Register' : 'Log in'}
                  </button>
                </p>
              </form>
            )}
          </section>
        </div>
      )}
            {active === 'Home' && (
        <>
          <button
            className="scroll-button scroll-up"
            type="button"
            aria-label="Scroll to top"
            onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}
          >
            ↓
          </button>

          <button
            className="scroll-button scroll-down"
            type="button"
            aria-label="Scroll to bottom"
            onClick={() =>
              window.scrollTo({
                top: document.documentElement.scrollHeight,
                behavior: 'smooth'
              })
            }
          >
             ↑
          </button>
        </>
      )}
    </div>
  );
}
export default App;