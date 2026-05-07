namespace OpenAiChat.Api.Services;

/// <summary>
/// Abstraction over the OpenAI chat completion call.
/// Keeping this as an interface lets unit tests inject fakes without
/// hitting the real OpenAI API.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Sends <paramref name="userMessage"/> to OpenAI and returns the model's reply.
    /// </summary>
    /// <param name="userMessage">The user's plain-text question or instruction.</param>
    /// <param name="cancellationToken">Propagates cancellation from the HTTP request pipeline.</param>
    /// <returns>The text content of the first choice returned by OpenAI.</returns>
    /// <exception cref="ChatServiceException">
    /// Thrown when the OpenAI call fails after all Polly retries are exhausted,
    /// or when the response contains no usable content.
    /// </exception>
    Task<string> GetChatResponseAsync(string userMessage, CancellationToken cancellationToken = default);
}
