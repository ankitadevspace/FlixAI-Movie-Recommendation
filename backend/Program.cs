using System.Text;

using backend.Data;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using NetflixClone.Services;

using Pgvector.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// LOGGING
// =====================================================

builder.Logging.ClearProviders();

builder.Logging.AddConsole();

builder.Logging.AddDebug();

// =====================================================
// DATABASE
// =====================================================

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.UseVector()
    ));

// =====================================================
// MEMORY CACHE
// =====================================================

builder.Services.AddMemoryCache();

// =====================================================
// REDIS / DISTRIBUTED CACHE
// =====================================================

var redisConnection =
    builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "FlixAI:";
});

// =====================================================
// JWT AUTHENTICATION
// =====================================================

var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT signing key is not configured. " +
        "Add Jwt:Key to User Secrets or appsettings."
    );

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? "FlixAI";

var jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? "FlixAI";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)
                ),

            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// =====================================================
// PASSWORD HASHING
// =====================================================

builder.Services.AddScoped<
    Microsoft.AspNetCore.Identity.IPasswordHasher<backend.Models.User>,
    Microsoft.AspNetCore.Identity.PasswordHasher<backend.Models.User>
>();

// =====================================================
// AUTHENTICATION SERVICES
// =====================================================

builder.Services.AddScoped<
    IJwtTokenService,
    JwtTokenService>();

builder.Services.AddScoped<
    IAuthService,
    AuthService>();

// =====================================================
// TMDB HTTP CLIENT
// =====================================================

builder.Services.AddHttpClient<ITmdbService, TmdbService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["TMDb:BaseUrl"]
        ?? "https://api.themoviedb.org/3/"
    );

    client.Timeout = TimeSpan.FromSeconds(15);

    client.DefaultRequestHeaders.Add(
        "Accept",
        "application/json"
    );
});

// =====================================================
// APPLICATION SERVICES
// =====================================================

builder.Services.AddScoped<MovieImportService>();

builder.Services.AddScoped<
    IRecommendationService,
    RecommendationService>();

// =====================================================
// AI / OLLAMA EMBEDDINGS
// =====================================================

builder.Services.AddHttpClient<
    IEmbeddingService,
    OllamaEmbeddingService>(client =>
{
    client.BaseAddress = new Uri(
        "http://localhost:11434/"
    );

    client.Timeout = TimeSpan.FromMinutes(5);
});

// =====================================================
// MOVIE DOCUMENT / EMBEDDING SERVICES
// =====================================================

builder.Services.AddScoped<MovieDocumentService>();

builder.Services.AddScoped<
    IMovieEmbeddingService,
    MovieEmbeddingService>();

// =====================================================
// SEMANTIC SEARCH
// =====================================================

builder.Services.AddScoped<
    ISemanticSearchService,
    SemanticSearchService>();

// =====================================================
// MOVIE SEARCH
// =====================================================

builder.Services.AddScoped<
    IMovieSearchService,
    MovieSearchService>();

// =====================================================
// USER AI CONTEXT
// =====================================================

builder.Services.AddScoped<
    IUserAiContextService,
    UserAiContextService>();

// =====================================================
// RAG
// =====================================================

builder.Services.AddScoped<
    IRagService,
    RagService>();

// =====================================================
// AI / OLLAMA LLM
// =====================================================

builder.Services.AddHttpClient<
    ILocalLlmService,
    OllamaLlmService>(client =>
{
    client.BaseAddress = new Uri(
        "http://localhost:11434/"
    );

    client.Timeout = TimeSpan.FromMinutes(5);
});

// =====================================================
// CORS
// =====================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// =====================================================
// CONTROLLERS + SWAGGER
// =====================================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

//builder.Services.AddSwaggerGen();

// =====================================================
// CONTROLLERS + SWAGGER
// =====================================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.ParameterLocation.Header,
            Description =
                "Enter your JWT token. Example: Bearer {your token}"
        });

    options.AddSecurityRequirement(document =>
        new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            [
                new Microsoft.OpenApi.OpenApiSecuritySchemeReference(
                    "Bearer",
                    document)
            ] = []
        });
});


// =====================================================
// BUILD APPLICATION
// =====================================================

var app = builder.Build();

// =====================================================
// GLOBAL EXCEPTION HANDLING
// =====================================================

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandler =
            context.RequestServices
                .GetRequiredService<
                    ILoggerFactory>()
                .CreateLogger("GlobalExceptionHandler");

        var exception =
            context.Features
                .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()
                ?.Error;

        if (exception != null)
        {
            exceptionHandler.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path
            );
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(
            new
            {
                success = false,
                message = "An unexpected error occurred.",
                statusCode = 500
            }
        );
    });
});

// =====================================================
// SWAGGER
// =====================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

// =====================================================
// HTTPS
// =====================================================

//app.UseHttpsRedirection(); while uploading to render need to change to below

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

// =====================================================
// CORS
// =====================================================

app.UseCors("AllowFrontend");

// =====================================================
// AUTHENTICATION
// =====================================================

app.UseAuthentication();

// =====================================================
// AUTHORIZATION
// =====================================================

app.UseAuthorization();


// =====================================================
// CONTROLLERS
// =====================================================

app.MapControllers();

// =====================================================
// RUN
// =====================================================

app.Run();