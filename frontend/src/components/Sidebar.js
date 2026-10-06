import React from 'react';
import './Sidebar.css';
import SearchBar from './SearchBar';

const mainItems = [
  ['⌂', 'Home'],
  ['◉', 'Discover'],
  ['✦', 'AI Picks'],
  ['♡', 'My List'],
];

const libraryItems = [
  ['◷', 'Recently Added'],
  ['▶', 'Continue Watching'],
];

function Sidebar({ active, onNavigate, onSearchFocus }) {
  const handleSearchKeyDown = (event) => {
    if (event.key === 'Enter') {
      onSearch?.(event.target.value.trim());
    }
  };

  return (
    <aside className="sidebar">
      <div className="logo">FLIX<span>AI</span></div>

      <div
        className="sidebar-search"
        onClick={() => onSearchFocus?.()}
      >
      <span>⌕</span>
      <span>Search</span>
      </div>

      <p className="nav-label">EXPLORE</p>
      <nav>
        {mainItems.map(([icon, label]) => (
          <button
            key={label}
            type="button"
            className={`nav-item ${active === label ? 'active' : ''}`}
            onClick={() => onNavigate?.(label)}
          >
            <span>{icon}</span>{label}
          </button>
        ))}
      </nav>

      <p className="nav-label library-label">LIBRARY</p>
      <nav>
        {libraryItems.map(([icon, label]) => (
          <button
            key={label}
            type="button"
            className={`nav-item ${active === label ? 'active' : ''}`}
            onClick={() => onNavigate?.(label)}
          >
            <span>{icon}</span>{label}
          </button>
        ))}
      </nav>

      <button
        type="button"
        className="sidebar-ai"
        onClick={() => onNavigate?.('AI Picks')}
      >
        <div className="spark">✦</div>
        <strong>Your movie AI</strong>
        <p>Describe a mood. I'll find the movie.</p>
      </button>
    </aside>
  );
}

export default Sidebar;