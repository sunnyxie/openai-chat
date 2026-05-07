using Microsoft.AspNetCore.Mvc;
using OpenAiChat.Api.Models;
using OpenAiChat.Api.Services;

namespace OpenAiChat.Api.Controllers;

/// <summary>
/// Handles chat completion requests.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Sends a message to OpenAI and returns the generated response.
    /// </summary>
    /// <param name="request">The user's message payload.</param>
    /// <param name="cancellationToken">Bound to the client's HTTP connection lifetime.</param>
    /// <returns>
    /// 200 OK with the OpenAI response, or an error payload on failure.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PostAsync(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "POST /api/chat received. MessageLength={Length}", request.Message.Length);

        var answer = await _chatService.GetChatResponseAsync(request.Message, cancellationToken);

        _logger.LogInformation("POST /api/chat completed successfully.");

        return Ok(new ChatResponse { Response = answer });
    }
}
