using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenAiChat.Api.Controllers;
using OpenAiChat.Api.Models;
using OpenAiChat.Api.Services;
using Xunit;

namespace OpenAiChat.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="ChatController"/>.
/// 
/// Strategy: mock IChatService so no real HTTP / OpenAI calls are made.
/// </summary>
public sealed class ChatControllerTests
{
    private readonly Mock<IChatService> _mockChatService;
    private readonly ChatController _sut; // System Under Test

    public ChatControllerTests()
    {
        _mockChatService = new Mock<IChatService>(MockBehavior.Strict);
        _sut = new ChatController(_mockChatService.Object, NullLogger<ChatController>.Instance);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PostAsync_ValidMessage_Returns200WithResponse()
    {
        // Arrange
        const string userMessage = "What is an API?";
        const string aiAnswer   = "An API allows software to talk to other software.";

        _mockChatService
            .Setup(s => s.GetChatResponseAsync(userMessage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiAnswer);

        var request = new ChatRequest { Message = userMessage };

        // Act
        var actionResult = await _sut.PostAsync(request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ChatResponse>().Subject;
        response.Response.Should().Be(aiAnswer);

        _mockChatService.VerifyAll();
    }

    [Fact]
    public async Task PostAsync_ServiceReturnsLongAnswer_Returns200()
    {
        // Arrange
        var longAnswer = new string('A', 2000);

        _mockChatService
            .Setup(s => s.GetChatResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(longAnswer);

        var request = new ChatRequest { Message = "Tell me a lot." };

        // Act
        var actionResult = await _sut.PostAsync(request, CancellationToken.None);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
    }

    // ── Error propagation ─────────────────────────────────────────────────────

    [Fact]
    public async Task PostAsync_ServiceThrowsChatServiceException_PropagatesException()
    {
        // Arrange
        _mockChatService
            .Setup(s => s.GetChatResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ChatServiceException("OpenAI is unavailable."));

        var request = new ChatRequest { Message = "Hello?" };

        // Act & Assert
        // GlobalExceptionMiddleware handles the exception in the real pipeline.
        // In unit tests we verify it propagates correctly from the controller.
        await _sut
            .Invoking(c => c.PostAsync(request, CancellationToken.None))
            .Should()
            .ThrowAsync<ChatServiceException>()
            .WithMessage("OpenAI is unavailable.");
    }

    [Fact]
    public async Task PostAsync_CancellationRequested_PropagatesOperationCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _mockChatService
            .Setup(s => s.GetChatResponseAsync(It.IsAny<string>(), cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        var request = new ChatRequest { Message = "Will be cancelled." };

        // Act & Assert
        await _sut
            .Invoking(c => c.PostAsync(request, cts.Token))
            .Should()
            .ThrowAsync<OperationCanceledException>();
    }

    // ── Service interaction ───────────────────────────────────────────────────

    [Fact]
    public async Task PostAsync_PassesExactMessageToService()
    {
        // Arrange
        const string exactMessage = "  trim me not  ";

        _mockChatService
            .Setup(s => s.GetChatResponseAsync(exactMessage, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        var request = new ChatRequest { Message = exactMessage };

        // Act
        await _sut.PostAsync(request, CancellationToken.None);

        // Assert — verify the controller forwards the raw message without modification
        _mockChatService.Verify(
            s => s.GetChatResponseAsync(exactMessage, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
