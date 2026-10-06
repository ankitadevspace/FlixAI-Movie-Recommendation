using Microsoft.AspNetCore.Mvc;
using NetflixClone.Dtos;
using NetflixClone.Services;

namespace NetflixClone.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IMovieEmbeddingService _embeddingService;
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly ILocalLlmService _localLlmService;

    public AiController(
        IMovieEmbeddingService embeddingService,
        ISemanticSearchService semanticSearchService,
        ILocalLlmService localLlmService)
    {
        _embeddingService = embeddingService;
        _semanticSearchService = semanticSearchService;
        _localLlmService = localLlmService;
    }

    [HttpPost("index-movies")]
    public async Task<IActionResult> IndexMovies(
        CancellationToken cancellationToken)
    {
        try
        {
            var count =
                await _embeddingService.IndexMoviesAsync(
                    cancellationToken);

            return Ok(new
            {
                message =
                    "Movie embeddings generated successfully.",
                indexed = count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message =
                    "Failed to generate movie embeddings.",
                error = ex.Message
            });
        }
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search(
        [FromBody] SemanticSearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var response =
                await _semanticSearchService.SearchAsync(
                    request,
                    cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message =
                    "Semantic movie search failed.",
                error = ex.Message
            });
        }
    }

    [HttpPost("local-llm-test")]
    public async Task<IActionResult> LocalLlmTest(
        CancellationToken cancellationToken)
    {
        try
        {
            var answer = await _localLlmService.GenerateAsync(
                """
                You are a movie recommendation assistant.

                Explain in 2 short sentences why someone might enjoy
                a dark science-fiction movie involving survival.
                """,
                cancellationToken);

            return Ok(new
            {
                message = "Ollama local LLM is working.",
                answer
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Ollama local LLM test failed.",
                error = ex.Message
            });
        }
    }
}