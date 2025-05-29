using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using McpScheduler.Core.Interfaces;
using McpScheduler.Core.Models;
using Microsoft.Extensions.Logging;

namespace McpScheduler.Infrastructure.Services
{
    /// <summary>
    /// Implementation of the conversation service
    /// </summary>
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _repository;
        private readonly IHttpClientService _httpClientService;
        private readonly ISchedulerService _schedulerService;
        private readonly ILogger<ConversationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConversationService"/> class
        /// </summary>
        /// <param name="repository">The conversation repository</param>
        /// <param name="httpClientService">The HTTP client service</param>
        /// <param name="schedulerService">The scheduler service</param>
        /// <param name="logger">The logger</param>
        public ConversationService(
            IConversationRepository repository,
            IHttpClientService httpClientService,
            ISchedulerService schedulerService,
            ILogger<ConversationService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
            _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<Conversation> ScheduleConversationAsync(Conversation conversation)
        {
            if (conversation == null)
                throw new ArgumentNullException(nameof(conversation));

            try
            {
                // Validate conversation
                if (conversation.ScheduledTime <= DateTime.UtcNow)
                {
                    throw new ArgumentException("Scheduled time must be in the future", nameof(conversation.ScheduledTime));
                }

                if (conversation.Target == null || string.IsNullOrEmpty(conversation.Target.Endpoint))
                {
                    throw new ArgumentException("Target endpoint is required", nameof(conversation.Target));
                }

                // Save conversation to database
                var savedConversation = await _repository.CreateAsync(conversation);

                // Schedule the job
                var jobId = await _schedulerService.ScheduleConversationJobAsync(savedConversation.Id, savedConversation.ScheduledTime);

                _logger.LogInformation("Conversation {ConversationId} scheduled for {ScheduledTime} with job ID {JobId}",
                    savedConversation.Id, savedConversation.ScheduledTime, jobId);

                return savedConversation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling conversation {@Conversation}", conversation);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Conversation?> GetConversationAsync(Guid id)
        {
            try
            {
                return await _repository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation with ID {ConversationId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CancelConversationAsync(Guid id)
        {
            try
            {
                var conversation = await _repository.GetByIdAsync(id);
                if (conversation == null)
                {
                    _logger.LogWarning("Attempted to cancel non-existent conversation with ID {ConversationId}", id);
                    return false;
                }

                if (conversation.Status != ConversationStatus.Scheduled)
                {
                    _logger.LogWarning("Cannot cancel conversation {ConversationId} with status {Status}", id, conversation.Status);
                    return false;
                }

                // Update status to cancelled
                var updateResult = await _repository.UpdateStatusAsync(id, ConversationStatus.Cancelled);
                if (!updateResult)
                {
                    _logger.LogWarning("Failed to update status for conversation {ConversationId}", id);
                    return false;
                }

                _logger.LogInformation("Conversation {ConversationId} cancelled successfully", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling conversation with ID {ConversationId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExecuteConversationAsync(Guid conversationId)
        {
            try
            {
                _logger.LogInformation("Executing conversation {ConversationId}", conversationId);

                // Get conversation
                var conversation = await _repository.GetByIdAsync(conversationId);
                if (conversation == null)
                {
                    _logger.LogWarning("Attempted to execute non-existent conversation with ID {ConversationId}", conversationId);
                    return false;
                }

                if (conversation.Status != ConversationStatus.Scheduled)
                {
                    _logger.LogWarning("Cannot execute conversation {ConversationId} with status {Status}", conversationId, conversation.Status);
                    return false;
                }

                // Update status to in progress
                await _repository.UpdateStatusAsync(conversationId, ConversationStatus.InProgress);

                // Parse HTTP method
                var httpMethod = conversation.Target.Method.ToUpper() switch
                {
                    "GET" => HttpMethod.Get,
                    "POST" => HttpMethod.Post,
                    "PUT" => HttpMethod.Put,
                    "DELETE" => HttpMethod.Delete,
                    "PATCH" => HttpMethod.Patch,
                    _ => HttpMethod.Post // Default to POST if method is not recognized
                };

                // Parse headers from JSON
                var headers = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(conversation.Target.HeadersJson))
                {
                    try
                    {
                        headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(conversation.Target.HeadersJson)
                                 ?? new Dictionary<string, string>();
                    }
                    catch
                    {
                        headers = new Dictionary<string, string>();
                    }
                }

                // Send request
                var result = await _httpClientService.SendRequestAsync(
                    conversation.Target.Endpoint,
                    httpMethod,
                    conversation.ConversationText,
                    headers);

                // Update status based on result
                var status = result ? ConversationStatus.Completed : ConversationStatus.Failed;
                await _repository.UpdateStatusAsync(conversationId, status);

                _logger.LogInformation("Conversation {ConversationId} execution completed with status {Status}", conversationId, status);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing conversation with ID {ConversationId}", conversationId);

                // Update status to failed
                try
                {
                    await _repository.UpdateStatusAsync(conversationId, ConversationStatus.Failed);
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Failed to update status for conversation {ConversationId} after execution error", conversationId);
                }

                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Conversation>> GetConversationsAsync(int page, int pageSize, string? status = null)
        {
            try
            {
                ConversationStatus? statusFilter = null;
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<ConversationStatus>(status, true, out var parsedStatus))
                {
                    statusFilter = parsedStatus;
                }

                return await _repository.GetPagedAsync(page, pageSize, statusFilter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations with page {Page}, pageSize {PageSize}, status {Status}", page, pageSize, status);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetConversationsCountAsync(string? status = null)
        {
            try
            {
                ConversationStatus? statusFilter = null;
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<ConversationStatus>(status, true, out var parsedStatus))
                {
                    statusFilter = parsedStatus;
                }

                return await _repository.GetCountAsync(statusFilter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations count with status {Status}", status);
                throw;
            }
        }
    }
}
