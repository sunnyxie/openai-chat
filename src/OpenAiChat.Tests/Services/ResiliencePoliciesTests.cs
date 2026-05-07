using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAiChat.Api.Configuration;
using OpenAiChat.Api.Services;
using Polly.Registry;
using Xunit;

namespace OpenAiChat.Tests.Services;

/// <summary>
/// Verifies that <see cref="ResiliencePolicies.RegisterPolicies"/> correctly
/// populates the policy registry with the expected named entries.
/// </summary>
public sealed class ResiliencePoliciesTests
{
    [Fact]
    public void RegisterPolicies_DefaultOptions_AddsAllThreeNamedPolicies()
    {
        // Arrange
        var registry = new PolicyRegistry();
        var opts = new ResiliencePolicyOptions
        {
            RetryCount = 2,
            RetryBaseDelaySeconds = 0.5,
            TimeoutSeconds = 5
        };
        var logger = NullLogger.Instance;

        // Act
        ResiliencePolicies.RegisterPolicies(registry, opts, logger);

        // Assert — all three keys must exist
        registry.ContainsKey(ResiliencePolicies.RetryPolicyName)
            .Should().BeTrue("retry policy should be registered");

        registry.ContainsKey(ResiliencePolicies.TimeoutPolicyName)
            .Should().BeTrue("timeout policy should be registered");

        registry.ContainsKey(ResiliencePolicies.CombinedPolicyName)
            .Should().BeTrue("combined policy should be registered");
    }

    [Fact]
    public void RegisterPolicies_ReturnsTheSameRegistryInstance()
    {
        // Arrange
        var registry = new PolicyRegistry();

        // Act
        var returned = ResiliencePolicies.RegisterPolicies(
            registry,
            new ResiliencePolicyOptions(),
            NullLogger.Instance);

        // Assert
        returned.Should().BeSameAs(registry);
    }
}
