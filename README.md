# FlixAI — AI-Powered Streaming Platform

A full-stack movie discovery platform built with React and ASP.NET Core. This project is being evolved from a Netflix-style clone into an AI-powered recommendation and semantic search system.

#FRONTEND LINK - https://flixai-movie-streaming-platform.netlify.app/

## Current stack

- React 19
- ASP.NET Core / .NET 10
- C#
- REST APIs
- TMDB API
- IMemoryCache
- HttpClientFactory
- Swagger / OpenAPI

## Roadmap

1. Backend service layer and secure configuration ✅
2. PostgreSQL + EF Core
3. Authentication with JWT / ASP.NET Core Identity
4. Redis distributed caching
5. Semantic movie search with embeddings
6. Vector database / pgvector
7. RAG pipeline
8. LLM-powered movie assistant
9. Personalized recommendations
10. Docker + CI/CD + Azure
11. OpenTelemetry and production observability

## Local configuration

Set the TMDB API key through .NET user-secrets or an environment variable. Never commit the real key to Git.

Example user-secrets command:

```bash
dotnet user-secrets init
dotnet user-secrets set "Tmdb:ApiKey" "YOUR_TMDB_API_KEY"
```

For the React app, copy `.env.example` to `.env` if the API is running on a different address.
