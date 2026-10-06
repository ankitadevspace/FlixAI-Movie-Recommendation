const API_BASE_URL = "http://localhost:5039";

const TOKEN_KEY = "flixai_token";
const USER_KEY = "flixai_user";
const EXPIRY_KEY = "flixai_token_expiry";

export async function login(email, password) {
  const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ email, password }),
  });

  const data = await response.json().catch(() => ({}));

  if (!response.ok) {
    throw new Error(data.message || "Login failed.");
  }

  if (!data.token) {
    throw new Error("Login succeeded, but the server did not return a token.");
  }

  localStorage.setItem(TOKEN_KEY, data.token);
  localStorage.setItem(
    USER_KEY,
    JSON.stringify({
      userId: data.userId,
      name: data.name,
      email: data.email,
    })
  );
  localStorage.setItem(EXPIRY_KEY, data.expiresAt);

  return data;
}

export async function register(name, email, password, preferredGenres = null) {
  const response = await fetch(`${API_BASE_URL}/api/auth/register`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      name,
      email,
      password,
      preferredGenres,
    }),
  });

  const data = await response.json().catch(() => ({}));

  if (!response.ok) {
    throw new Error(data.message || "Registration failed.");
  }

  if (!data.token) {
    throw new Error("Registration succeeded, but the server did not return a token.");
  }

  localStorage.setItem(TOKEN_KEY, data.token);
  localStorage.setItem(
    USER_KEY,
    JSON.stringify({
      userId: data.userId,
      name: data.name,
      email: data.email,
    })
  );
  localStorage.setItem(EXPIRY_KEY, data.expiresAt);

  return data;
}

export function getToken() {
  const token = localStorage.getItem(TOKEN_KEY);
  const expiry = localStorage.getItem(EXPIRY_KEY);

  if (!token) return null;

  if (expiry && new Date(expiry).getTime() <= Date.now()) {
    logout();
    return null;
  }

  return token;
}

export function getCurrentUser() {
  const user = localStorage.getItem(USER_KEY);

  try {
    return user ? JSON.parse(user) : null;
  } catch {
    return null;
  }
}

export function logout() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
  localStorage.removeItem(EXPIRY_KEY);
}

export async function authFetch(url, options = {}) {
  const token = getToken();

  if (!token) {
    throw new Error("Please log in to use My List.");
  }

  const headers = new Headers(options.headers || {});
  headers.set("Authorization", `Bearer ${token}`);

  return fetch(url, {
    ...options,
    headers,
  });
}