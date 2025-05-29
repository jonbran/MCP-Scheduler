using System;
using System.Threading.Tasks;
using FluentAssertions;
using McpScheduler.Core.Interfaces;
using McpScheduler.Core.Models;
using McpScheduler.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace McpScheduler.Tests
{
    public class ConversationServiceTests
    {
        private readonly Mock<IConversationRepository> _mockRepository;
        private readonly Mock<IHttpClientService> _mockHttpClient;
        private readonly Mock<ISchedulerService> _mockScheduler;
        private readonly Mock<ILogger<ConversationService>> _mockLogger;
        private readonly ConversationService _service;

        public ConversationServiceTests()
        {
            _mockRepository = new Mock<IConversationRepository>();
            _mockHttpClient = new Mock<IHttpClientService>();
            _mockScheduler = new Mock<ISchedulerService>();
            _mockLogger = new Mock<ILogger<ConversationService>>();

            _service = new ConversationService(
                _mockRepository.Object,
                _mockHttpClient.Object,
                _mockScheduler.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task ScheduleConversationAsync_WithValidConversation_ShouldReturnConversationWithId()
        {
            // Arrange
            var futureTime = DateTime.UtcNow.AddHours(1);
            var conversation = new Conversation
            {
                ConversationText = "Test message",
                ScheduledTime = futureTime,
                Target = new ResponseTarget
                {
                    Endpoint = "https://test.api/callback",
                    Method = "POST"
                }
            };

            var savedConversation = new Conversation
            {
                Id = Guid.NewGuid(),
                ConversationText = conversation.ConversationText,
                ScheduledTime = conversation.ScheduledTime,
                Target = conversation.Target,
                Status = ConversationStatus.Scheduled,
                CreatedAt = DateTime.UtcNow
            };

            _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Conversation>()))
                .ReturnsAsync(savedConversation);

            _mockScheduler.Setup(s => s.ScheduleConversationJobAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>()))
                .ReturnsAsync("job-id-123");

            // Act
            var result = await _service.ScheduleConversationAsync(conversation);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeEmpty();
            result.Status.Should().Be(ConversationStatus.Scheduled);

            _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Conversation>()), Times.Once);
            _mockScheduler.Verify(s => s.ScheduleConversationJobAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task ScheduleConversationAsync_WithPastTime_ShouldThrowArgumentException()
        {
            // Arrange
            var pastTime = DateTime.UtcNow.AddHours(-1);
            var conversation = new Conversation
            {
                ConversationText = "Test message",
                ScheduledTime = pastTime,
                Target = new ResponseTarget
                {
                    Endpoint = "https://test.api/callback",
                    Method = "POST"
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ScheduleConversationAsync(conversation));

            _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Conversation>()), Times.Never);
            _mockScheduler.Verify(s => s.ScheduleConversationJobAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task CancelConversationAsync_WithExistingConversation_ShouldReturnTrue()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var conversation = new Conversation
            {
                Id = conversationId,
                ConversationText = "Test message",
                ScheduledTime = DateTime.UtcNow.AddHours(1),
                Status = ConversationStatus.Scheduled,
                Target = new ResponseTarget
                {
                    Endpoint = "https://test.api/callback",
                    Method = "POST"
                }
            };

            _mockRepository.Setup(r => r.GetByIdAsync(conversationId))
                .ReturnsAsync(conversation);

            _mockRepository.Setup(r => r.UpdateStatusAsync(conversationId, ConversationStatus.Cancelled))
                .ReturnsAsync(true);

            // Act
            var result = await _service.CancelConversationAsync(conversationId);

            // Assert
            result.Should().BeTrue();

            _mockRepository.Verify(r => r.GetByIdAsync(conversationId), Times.Once);
            _mockRepository.Verify(r => r.UpdateStatusAsync(conversationId, ConversationStatus.Cancelled), Times.Once);
        }

        [Fact]
        public async Task ExecuteConversationAsync_WithValidConversation_ShouldSendRequestAndUpdateStatus()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var conversation = new Conversation
            {
                Id = conversationId,
                ConversationText = "Test message",
                ScheduledTime = DateTime.UtcNow,
                Status = ConversationStatus.Scheduled,
                Target = new ResponseTarget
                {
                    Endpoint = "https://test.api/callback",
                    Method = "POST"
                }
            };

            _mockRepository.Setup(r => r.GetByIdAsync(conversationId))
                .ReturnsAsync(conversation);

            _mockRepository.Setup(r => r.UpdateStatusAsync(conversationId, It.IsAny<ConversationStatus>()))
                .ReturnsAsync(true);

            _mockHttpClient.Setup(h => h.SendRequestAsync(
                    It.IsAny<string>(),
                    It.IsAny<System.Net.Http.HttpMethod>(),
                    It.IsAny<string>(),
                    It.IsAny<System.Collections.Generic.IDictionary<string, string>>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ExecuteConversationAsync(conversationId);

            // Assert
            result.Should().BeTrue();

            _mockRepository.Verify(r => r.GetByIdAsync(conversationId), Times.Once);
            _mockRepository.Verify(r => r.UpdateStatusAsync(conversationId, ConversationStatus.InProgress), Times.Once);
            _mockRepository.Verify(r => r.UpdateStatusAsync(conversationId, ConversationStatus.Completed), Times.Once);
            _mockHttpClient.Verify(h => h.SendRequestAsync(
                It.IsAny<string>(),
                It.IsAny<System.Net.Http.HttpMethod>(),
                It.IsAny<string>(),
                It.IsAny<System.Collections.Generic.IDictionary<string, string>>()), Times.Once);
        }
    }
}
