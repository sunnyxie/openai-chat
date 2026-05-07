using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAiChat.Api.Configuration;
using System.ClientModel;

namespace OpenAiChat.Api.Services;

/// <summary>
/// Sends chat completion requests to the OpenAI API using the official .NET SDK.
/// Resilience (retries + timeout) is handled at the HttpClient/Polly layer;
/// this class focuses purely on request construction and response mapping.
/// </summary>
public sealed class OpenAiChatService : IChatService
{
    private readonly ChatClient _chatClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiChatService> _logger;

    public OpenAiChatService(
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiChatService> logger)
    {
        _options = options.Value;
        _logger = logger;

        // API key comes exclusively from the environment variable OPENAI_API_KEY.
        // Never commit a real key to source control.
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException(
                "The OPENAI_API_KEY environment variable is not set. " +
                "Set it via a GitHub Actions secret, a Docker --env flag, or your local shell.");

        _chatClient = new ChatClient(_options.ModelName, apiKey);
    }

    /// <inheritdoc />
    public async Task<string> GetChatResponseAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending chat request to OpenAI. Model={Model}, MessageLength={Length}",
            _options.ModelName, userMessage.Length);

        try
        {
            var messages = new List<OpenAI.Chat.ChatMessage>
            {
                new SystemChatMessage(_options.SystemPrompt),
                new UserChatMessage(userMessage)
            };

            var completionOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = _options.MaxTokens
            };

            ClientResult<ChatCompletion> result = await _chatClient
                .CompleteChatAsync(messages, completionOptions, cancellationToken);

            ChatCompletion completion = result.Value;

            var content = completion.Content.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("OpenAI returned an empty or null content block.");
                throw new ChatServiceException("OpenAI returned an empty response.");
            }

            _logger.LogInformation(
                "OpenAI response received. FinishReason={Reason}, TokensUsed={Tokens}",
                completion.FinishReason,
                completion.Usage?.TotalTokenCount ?? 0);

            return content;
        }
        catch (ChatServiceException)
        {
            // Already a domain exception — re-throw without wrapping
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("OpenAI request was cancelled by the client.");
            throw new ChatServiceException("The request was cancelled before OpenAI could respond.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling the OpenAI API.");
            throw new ChatServiceException(
                $"OpenAI call failed: {ex.Message}", ex);
        }
    }
}
