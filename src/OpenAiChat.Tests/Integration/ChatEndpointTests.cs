using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenAiChat.Api.Models;
using OpenAiChat.Api.Services;
using Xunit;

namespace OpenAiChat.Tests.Integration;

/// <summary>
/// Integration tests that spin up the full ASP.NET Core pipeline in-memory
/// via <see cref="WebApplicationFactory{TEntryPoint}"/>.
/// 
/// The real <see cref="IChatService"/> is replaced with a mock so no actual
/// OpenAI calls are made, but routing, middleware, and model validation run
/// against the real pipeline.
/// </summary>
public sealed class ChatEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChatEndpointTests(WebApplicationFactory<Program> factory)
    {
        // Set the env var to a dummy value so OpenAiChatService constructor
        // does not throw when the DI container is built.
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key-integration");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real OpenAiChatService registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IChatService));
                if (descriptor is not null) services.Remove(descriptor);

                // Register a controllable mock
                var mockService = new Mock<IChatService>();
                mockService
                    .Setup(s => s.GetChatResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("Mocked OpenAI response.");

                services.AddScoped<IChatService>(_ => mockService.Object);
            });
        });
    }

    [Fact]
    public async Task Post_ValidPayload_Returns200AndResponse()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new ChatRequest { Message = "What is an API?" };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ChatResponse>();
        body.Should().NotBeNull();
        body!.Response.Should().Be("Mocked OpenAI response.");
    }

    [Fact]
    public async Task Post_EmptyMessage_Returns400()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new { message = "" };   // empty string fails [Required]

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_MissingBody_Returns400()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync(
            "/api/chat",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Assert — "message" is required, so binding should fail with 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ServiceThrows_Returns502()
    {
        // Arrange — build a separate factory where the mock throws
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key-integration");

        var errorFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IChatService));
                if (descriptor is not null) services.Remove(descriptor);

                var failingMock = new Mock<IChatService>();
                failingMock
                    .Setup(s => s.GetChatResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new ChatServiceException("OpenAI is down."));

                services.AddScoped<IChatService>(_ => failingMock.Object);
            });
        });

        var client = errorFactory.CreateClient();
        var payload = new ChatRequest { Message = "Hello?" };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", payload);

        // Assert — GlobalExceptionMiddleware maps ChatServiceException → 502
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Error.Should().Be("OpenAI request failed.");
    }
}
