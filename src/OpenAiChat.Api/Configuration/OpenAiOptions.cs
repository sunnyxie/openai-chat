namespace OpenAiChat.Api.Configuration;

/// <summary>
/// Strongly-typed binding for the "OpenAI" section in appsettings.json.
/// The API key is intentionally NOT stored here — it is read from the
/// OPENAI_API_KEY environment variable (or a GitHub Actions secret surfaced
/// as an environment variable at runtime).
/// </summary>
public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    /// <summary>
    /// The OpenAI model to use, e.g. "gpt-4o", "gpt-4o-mini", "gpt-3.5-turbo".
    /// Configurable per environment via appsettings.{Environment}.json or
    /// an environment variable override: OpenAI__ModelName
    /// </summary>
    public string ModelName { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Maximum tokens the model may generate in a single response.
    /// </summary>
    public int MaxTokens { get; set; } = 1024;

    /// <summary>
    /// System prompt sent with every request to guide model behaviour.
    /// </summary>
    public string SystemPrompt { get; set; } =
        "You are a helpful, concise assistant. Respond clearly and professionally.";
}

/// <summary>
/// Strongly-typed binding for the "ResiliencePolicy" section.
/// Controls Polly retry and timeout behaviour for outbound OpenAI calls.
/// </summary>
public sealed class ResiliencePolicyOptions
{
    public const string SectionName = "ResiliencePolicy";

    /// <summary>Number of times to retry a failed request (after the first attempt).</summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>Base delay in seconds between retries (exponential back-off multiplier).</summary>
    public double RetryBaseDelaySeconds { get; set; } = 1.0;

    /// <summary>Overall per-attempt timeout in seconds for an OpenAI HTTP call.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}
