namespace OpenAiChat.Api.Services;

/// <summary>
/// Thrown when the OpenAI service layer encounters a non-recoverable error,
/// such as an invalid API key, quota exhaustion, or unexpected response shape.
/// </summary>
public sealed class ChatServiceException : Exception
{
    public ChatServiceException(string message) : base(message) { }

    public ChatServiceException(string message, Exception inner) : base(message, inner) { }
}
