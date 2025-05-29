using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using McpScheduler.Core.Models;

namespace McpScheduler.Core.Interfaces
{
    /// <summary>
    /// Interface for conversation repository operations
    /// </summary>
    public interface IConversationRepository
    {
        /// <summary>
        /// Gets a conversation by its unique identifier
        /// </summary>
        /// <param name="id">The conversation ID</param>
        /// <returns>The conversation if found, null otherwise</returns>
        Task<Conversation?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets all conversations
        /// </summary>
        /// <returns>A list of all conversations</returns>
        Task<IEnumerable<Conversation>> GetAllAsync();

        /// <summary>
        /// Gets conversations that are scheduled for a specific time range
        /// </summary>
        /// <param name="start">The start time</param>
        /// <param name="end">The end time</param>
        /// <returns>A list of conversations scheduled within the specified time range</returns>
        Task<IEnumerable<Conversation>> GetScheduledBetweenAsync(DateTime start, DateTime end);

        /// <summary>
        /// Creates a new conversation
        /// </summary>
        /// <param name="conversation">The conversation to create</param>
        /// <returns>The created conversation with assigned ID</returns>
        Task<Conversation> CreateAsync(Conversation conversation);

        /// <summary>
        /// Updates an existing conversation
        /// </summary>
        /// <param name="conversation">The conversation to update</param>
        /// <returns>True if updated successfully, false otherwise</returns>
        Task<bool> UpdateAsync(Conversation conversation);

        /// <summary>
        /// Deletes a conversation by its unique identifier
        /// </summary>
        /// <param name="id">The conversation ID</param>
        /// <returns>True if deleted successfully, false otherwise</returns>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// Updates the status of a conversation
        /// </summary>
        /// <param name="id">The conversation ID</param>
        /// <param name="status">The new status</param>
        /// <returns>True if updated successfully, false otherwise</returns>
        Task<bool> UpdateStatusAsync(Guid id, ConversationStatus status);

        /// <summary>
        /// Gets conversations with pagination and optional status filter
        /// </summary>
        /// <param name="page">The page number</param>
        /// <param name="pageSize">The page size</param>
        /// <param name="status">Optional status filter</param>
        /// <returns>A paginated list of conversations</returns>
        Task<IEnumerable<Conversation>> GetPagedAsync(int page, int pageSize, ConversationStatus? status = null);

        /// <summary>
        /// Gets the total count of conversations with optional status filter
        /// </summary>
        /// <param name="status">Optional status filter</param>
        /// <returns>The total count of conversations</returns>
        Task<int> GetCountAsync(ConversationStatus? status = null);
    }
}
