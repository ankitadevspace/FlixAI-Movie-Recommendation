import axios from 'axios';

const api = axios.create({
  baseURL:
    process.env.REACT_APP_API_BASE_URL ||
    'http://localhost:5039/api',

  timeout: 60000,

  headers: {
    'Content-Type': 'application/json',
  },
});

// Attach JWT only when one exists.
// Public movie endpoints continue working without login.
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('flixai_token');

  if (token) {
    config.headers = config.headers || {};
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

export async function askRag(query, userId = 1, count = 5) {
  const response = await api.post('/Rag/ask', {
    query,
    userId,
    count,
  });

  return response.data;
}

export default api;