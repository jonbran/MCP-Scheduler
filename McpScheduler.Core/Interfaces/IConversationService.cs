using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using McpScheduler.Core.Models;

namespace McpScheduler.Core.Interfaces
{
    /// <summary>
    /// Interface for conversation service operations
    /// </summary>
    public interface IConversationService
    {
        /// <summary>
        /// Schedules a new conversation
        /// </summary>
        /// <param name="conversation">The conversation to schedule</param>
        /// <returns>The scheduled conversation with assigned ID</returns>
        Task<Conversation> ScheduleConversationAsync(Conversation conversation);

        /// <summary>
        /// Gets a conversation by its unique identifier
        /// </summary>
        /// <param name="id">The conversation ID</param>
        /// <returns>The conversation if found, null otherwise</returns>
        Task<Conversation?> GetConversationAsync(Guid id);

        /// <summary>
        /// Cancels a scheduled conversation
        /// </summary>
        /// <param name="id">The conversation ID</param>
        /// <returns>True if cancelled successfully, false otherwise</returns>
        Task<bool> CancelConversationAsync(Guid id);

        /// <summary>
        /// Executes a conversation by sending it to its target endpoint
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <returns>True if executed successfully, false otherwise</returns>
        Task<bool> ExecuteConversationAsync(Guid conversationId);

        /// <summary>
        /// Gets conversations with pagination
        /// </summary>
        /// <param name="page">The page number</param>
        /// <param name="pageSize">The page size</param>
        /// <param name="status">Optional status filter</param>
        /// <returns>The list of conversations</returns>
        Task<IEnumerable<Conversation>> GetConversationsAsync(int page, int pageSize, string? status = null);

        /// <summary>
        /// Gets the total count of conversations
        /// </summary>
        /// <param name="status">Optional status filter</param>
        /// <returns>The total count of conversations</returns>
        Task<int> GetConversationsCountAsync(string? status = null);
    }
}
