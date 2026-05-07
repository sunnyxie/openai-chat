using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Registry;
using OpenAiChat.Api.Configuration;

namespace OpenAiChat.Api.Services;

/// <summary>
/// Centralised Polly policy registry.
/// 
/// Policies registered here:
///   "OpenAiRetry"   — exponential back-off retry (transient HTTP errors + 429 / 5xx)
///   "OpenAiTimeout" — per-attempt pessimistic timeout
/// 
/// Both are combined into a PolicyWrap and added to the DI container so they can
/// be injected into typed HttpClients or used directly in service classes.
/// </summary>
public static class ResiliencePolicies
{
    public const string RetryPolicyName = "OpenAiRetry";
    public const string TimeoutPolicyName = "OpenAiTimeout";
    public const string CombinedPolicyName = "OpenAiCombined";

    /// <summary>
    /// Builds and registers all Polly policies against <paramref name="registry"/>.
    /// </summary>
    public static IPolicyRegistry<string> RegisterPolicies(
        IPolicyRegistry<string> registry,
        ResiliencePolicyOptions opts,
        ILogger logger)
    {
        // ── Retry policy ──────────────────────────────────────────────────────────
        // Retries on:  network failures, HTTP 5xx, HTTP 429 (rate limit)
        // Back-off:    exponential  (1s, 2s, 4s, …) with jitter
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: opts.RetryCount,
                sleepDurationProvider: attempt =>
                {
                    var delay = TimeSpan.FromSeconds(
                        Math.Pow(opts.RetryBaseDelaySeconds, attempt));

                    // Add jitter (±20 %) to avoid thundering-herd problems
                    var jitter = TimeSpan.FromMilliseconds(
                        Random.Shared.Next(-200, 200));

                    return delay + jitter;
                },
                onRetry: (outcome, delay, attempt, _) =>
                {
                    logger.LogWarning(
                        "OpenAI request failed (attempt {Attempt}/{Max}). " +
                        "Status={Status}. Retrying after {Delay}ms.",
                        attempt, opts.RetryCount,
                        outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message,
                        delay.TotalMilliseconds);
                });

        // ── Timeout policy ────────────────────────────────────────────────────────
        // Applies a per-attempt pessimistic timeout so a single slow call never
        // blocks indefinitely.
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            seconds: opts.TimeoutSeconds,
            onTimeoutAsync: (_, timeout, _) =>
            {
                logger.LogWarning(
                    "OpenAI request timed out after {Timeout}s.", timeout.TotalSeconds);
                return Task.CompletedTask;
            });

        registry.Add(RetryPolicyName, retryPolicy);
        registry.Add(TimeoutPolicyName, timeoutPolicy);

        // Combined wrap: outer = retry, inner = timeout
        // Execution order: retry → timeout → actual HTTP call
        var combinedPolicy = Policy.WrapAsync(retryPolicy, timeoutPolicy);
        registry.Add(CombinedPolicyName, combinedPolicy);

        return registry;
    }
}
