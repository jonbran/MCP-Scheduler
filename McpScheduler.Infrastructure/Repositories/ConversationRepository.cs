using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McpScheduler.Core.Interfaces;
using McpScheduler.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace McpScheduler.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for conversation entity operations
    /// </summary>
    public class ConversationRepository : IConversationRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ConversationRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConversationRepository"/> class
        /// </summary>
        /// <param name="dbContext">The database context</param>
        /// <param name="logger">The logger</param>
        public ConversationRepository(ApplicationDbContext dbContext, ILogger<ConversationRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<Conversation?> GetByIdAsync(Guid id)
        {
            try
            {
                return await _dbContext.Conversations
                    .Include(c => c.Target)
                    .FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversation with ID {ConversationId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Conversation>> GetAllAsync()
        {
            try
            {
                return await _dbContext.Conversations
                    .Include(c => c.Target)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all conversations");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Conversation>> GetScheduledBetweenAsync(DateTime start, DateTime end)
        {
            try
            {
                return await _dbContext.Conversations
                    .Include(c => c.Target)
                    .Where(c => c.ScheduledTime >= start && c.ScheduledTime <= end && c.Status == ConversationStatus.Scheduled)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scheduled conversations between {StartTime} and {EndTime}", start, end);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Conversation> CreateAsync(Conversation conversation)
        {
            if (conversation == null)
                throw new ArgumentNullException(nameof(conversation));

            try
            {
                conversation.Id = conversation.Id == Guid.Empty ? Guid.NewGuid() : conversation.Id;
                conversation.CreatedAt = DateTime.UtcNow;
                conversation.UpdatedAt = conversation.CreatedAt;
                conversation.Status = ConversationStatus.Scheduled;

                _dbContext.Conversations.Add(conversation);
                await _dbContext.SaveChangesAsync();
                return conversation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating conversation {@Conversation}", conversation);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateAsync(Conversation conversation)
        {
            if (conversation == null)
                throw new ArgumentNullException(nameof(conversation));

            try
            {
                var existingConversation = await _dbContext.Conversations
                    .Include(c => c.Target)
                    .FirstOrDefaultAsync(c => c.Id == conversation.Id);

                if (existingConversation == null)
                    return false;

                // Update properties
                existingConversation.ConversationText = conversation.ConversationText;
                existingConversation.ScheduledTime = conversation.ScheduledTime;
                existingConversation.UpdatedAt = DateTime.UtcNow;

                // Update target if it exists
                if (conversation.Target != null)
                {
                    existingConversation.Target.Endpoint = conversation.Target.Endpoint;
                    existingConversation.Target.Method = conversation.Target.Method;
                    existingConversation.Target.AdditionalInfo = conversation.Target.AdditionalInfo;

                    // Update headers
                    if (!string.IsNullOrEmpty(conversation.Target.HeadersJson))
                    {
                        existingConversation.Target.HeadersJson = conversation.Target.HeadersJson;
                    }
                }

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating conversation with ID {ConversationId}", conversation.Id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var conversation = await _dbContext.Conversations.FindAsync(id);
                if (conversation == null)
                    return false;

                _dbContext.Conversations.Remove(conversation);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation with ID {ConversationId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateStatusAsync(Guid id, ConversationStatus status)
        {
            try
            {
                var conversation = await _dbContext.Conversations.FindAsync(id);
                if (conversation == null)
                    return false;

                conversation.Status = status;
                conversation.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status to {Status} for conversation with ID {ConversationId}", status, id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Conversation>> GetPagedAsync(int page, int pageSize, ConversationStatus? status = null)
        {
            try
            {
                var query = _dbContext.Conversations
                    .Include(c => c.Target)
                    .AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(c => c.Status == status.Value);
                }

                return await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged conversations (page: {Page}, pageSize: {PageSize}, status: {Status})", page, pageSize, status);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetCountAsync(ConversationStatus? status = null)
        {
            try
            {
                var query = _dbContext.Conversations.AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(c => c.Status == status.Value);
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversations count (status: {Status})", status);
                throw;
            }
        }
    }
}
