using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using McpScheduler.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;

namespace McpScheduler.Infrastructure.Services
{
    /// <summary>
    /// Implementation of the scheduler service using Quartz.NET
    /// </summary>
    public class QuartzSchedulerService : ISchedulerService
    {
        private readonly ILogger<QuartzSchedulerService> _logger;
        private readonly ISchedulerFactory _schedulerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuartzSchedulerService"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="schedulerFactory">The scheduler factory</param>
        public QuartzSchedulerService(ILogger<QuartzSchedulerService> logger, ISchedulerFactory schedulerFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        }

        /// <inheritdoc/>
        public async Task<string> ScheduleConversationJobAsync(Guid conversationId, DateTime scheduledTime)
        {
            try
            {
                _logger.LogInformation("Scheduling conversation job for conversation {ConversationId} at {ScheduleTime}", conversationId, scheduledTime);

                var scheduler = await _schedulerFactory.GetScheduler();

                // Convert Guid to string for Quartz useProperties compatibility
                var jobData = new JobDataMap { { "conversationId", conversationId.ToString() } };

                var job = JobBuilder.Create<ConversationExecutionJob>()
                    .WithIdentity(Guid.NewGuid().ToString())
                    .UsingJobData(jobData)
                    .Build();

                var trigger = TriggerBuilder.Create()
                    .StartAt(scheduledTime)
                    .Build();

                await scheduler.ScheduleJob(job, trigger);

                _logger.LogInformation("Conversation job scheduled with ID {JobId}", job.Key.Name);
                return job.Key.Name;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling conversation job for conversation {ConversationId}", conversationId);
                throw;
            }
        }

        /// <inheritdoc/>
        public bool DeleteJob(string jobId)
        {
            try
            {
                _logger.LogInformation("Attempting to delete job with ID {JobId}", jobId);
                var scheduler = _schedulerFactory.GetScheduler().Result;
                var result = scheduler.DeleteJob(new JobKey(jobId)).Result;
                _logger.LogInformation("Job {JobId} deletion result: {Result}", jobId, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job with ID {JobId}", jobId);
                return false;
            }
        }

        /// <summary>
        /// Job class for executing scheduled conversations
        /// </summary>
        public class ConversationExecutionJob : IJob
        {
            private readonly IConversationService _conversationService;
            private readonly ILogger<ConversationExecutionJob> _logger;

            public ConversationExecutionJob(IConversationService conversationService, ILogger<ConversationExecutionJob> logger)
            {
                _conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public async Task Execute(IJobExecutionContext context)
            {
                var conversationIdString = context.MergedJobDataMap["conversationId"]?.ToString();
                if (string.IsNullOrEmpty(conversationIdString) || !Guid.TryParse(conversationIdString, out var conversationId))
                {
                    _logger.LogError("Invalid conversation ID in job data: {ConversationIdString}", conversationIdString);
                    throw new InvalidOperationException("Invalid conversation ID in job data");
                }

                _logger.LogInformation("Executing conversation job for conversation {ConversationId}", conversationId);
                await _conversationService.ExecuteConversationAsync(conversationId);
            }
        }
    }
}
