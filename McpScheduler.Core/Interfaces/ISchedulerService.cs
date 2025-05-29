using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace McpScheduler.Core.Interfaces
{
    /// <summary>
    /// Interface for scheduling background jobs
    /// </summary>
    public interface ISchedulerService
    {
        /// <summary>
        /// Schedules a conversation job to be executed at a specific time
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to execute</param>
        /// <param name="scheduledTime">The time when the job should be executed</param>
        /// <returns>The ID of the scheduled job</returns>
        Task<string> ScheduleConversationJobAsync(Guid conversationId, DateTime scheduledTime);

        /// <summary>
        /// Deletes a previously scheduled job
        /// </summary>
        /// <param name="jobId">The ID of the job to delete</param>
        /// <returns>True if deleted successfully, false otherwise</returns>
        bool DeleteJob(string jobId);
    }
}
