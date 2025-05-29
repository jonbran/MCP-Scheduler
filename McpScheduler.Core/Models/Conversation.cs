using System;
using System.Collections.Generic;
using System.Linq;

namespace McpScheduler.Core.Models
{
    /// <summary>
    /// Represents a scheduled conversation that will be initiated at a future time
    /// </summary>
    public class Conversation
    {
        /// <summary>
        /// Unique identifier for the conversation
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The time when the conversation should be initiated
        /// </summary>
        public DateTime ScheduledTime { get; set; }

        /// <summary>
        /// The text content of the conversation to be sent
        /// </summary>
        public required string ConversationText { get; set; }

        /// <summary>
        /// The target where the conversation should be sent
        /// </summary>
        public required ResponseTarget Target { get; set; }

        /// <summary>
        /// Creation timestamp of the conversation request
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last modification timestamp of the conversation request
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Current status of the conversation
        /// </summary>
        public ConversationStatus Status { get; set; }
    }

    /// <summary>
    /// Represents the target endpoint for delivering the conversation
    /// </summary>
    public class ResponseTarget
    {
        /// <summary>
        /// Primary key for the ResponseTarget
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Foreign key reference to the Conversation
        /// </summary>
        public Guid ConversationId { get; set; }

        /// <summary>
        /// The URL endpoint where the conversation should be sent
        /// </summary>
        public required string Endpoint { get; set; }

        /// <summary>
        /// The HTTP method to use when sending the conversation
        /// </summary>
        public required string Method { get; set; }

        /// <summary>
        /// Optional HTTP headers to include when sending the conversation (stored as JSON string)
        /// </summary>
        public string? HeadersJson { get; set; }

        /// <summary>
        /// Additional information or context for the target
        /// </summary>
        public string? AdditionalInfo { get; set; }
    }

    /// <summary>
    /// Represents the possible statuses of a conversation
    /// </summary>
    public enum ConversationStatus
    {
        /// <summary>
        /// The conversation is scheduled for future delivery
        /// </summary>
        Scheduled,

        /// <summary>
        /// The conversation is currently being processed
        /// </summary>
        InProgress,

        /// <summary>
        /// The conversation was successfully delivered
        /// </summary>
        Completed,

        /// <summary>
        /// The conversation delivery failed
        /// </summary>
        Failed,

        /// <summary>
        /// The conversation was cancelled before delivery
        /// </summary>
        Cancelled
    }
}
