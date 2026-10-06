import React, { useEffect, useState } from 'react';
import api from '../services/api';
import './SearchBar.css';

function SearchBar({ onResults, compact = false, onFocus }) {
  const [query, setQuery] = useState('');
  const [mode, setMode] = useState('ai');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const value = query.trim();

    if (!value) {
      onResults([]);
      return undefined;
    }

    const timer = setTimeout(async () => {
      setBusy(true);
      try {
        const response = mode === 'ai'
          ? await api.post('/Ai/search', { query: value, count: 12 })
          : await api.get('/movies/search', { params: { query: value } });

        const data = response.data;
        onResults(data.results || data.movies || []);
      } catch (error) {
        console.error('Search failed:', error);
        onResults([]);
      } finally {
        setBusy(false);
      }
    }, 450);

    return () => clearTimeout(timer);
  }, [query, mode, onResults]);

  return (
    <div className={`search-wrapper ${compact ? 'search-wrapper-compact' : ''}`}>
      <span className="search-icon">⌕</span>
      <input
        className="search-input"
        value={query}
        onChange={e => setQuery(e.target.value)}
        onFocus={onFocus}
        placeholder={compact ? 'Search' : 'Search movies or describe a mood...'}
        aria-label="Movie search"
      />
      {!compact && busy && <span className="search-status">...</span>}
      {!compact && (
        <button
          type="button"
          className={`search-mode ${mode === 'ai' ? 'selected' : ''}`}
          onClick={() => setMode(mode === 'ai' ? 'classic' : 'ai')}
          title="Toggle semantic AI search"
        >
          {mode === 'ai' ? '✦ AI' : '⌕ Classic'}
        </button>
      )}
    </div>
  );
}

export default SearchBar;