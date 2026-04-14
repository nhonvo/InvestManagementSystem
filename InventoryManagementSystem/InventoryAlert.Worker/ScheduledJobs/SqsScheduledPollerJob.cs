using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Interfaces;
using InventoryAlert.Worker.Models;

namespace InventoryAlert.Worker.ScheduledJobs;

/// <summary>
/// Hangfire recurring job: polls SQS once per execution and dispatches messages.
/// Uses the unified IIntegrationEventDispatcher for reliable processing.
/// </summary>
public class SqsScheduledPollerJob(
    ISqsHelper sqsHelper,
    IProcessQueueJob processQueueJob,
    WorkerSettings settings,
    ILogger<SqsScheduledPollerJob> logger)
{
    public async Task<JobResult> ExecuteAsync(CancellationToken ct = default)
    {
        logger.LogInformation("[SqsScheduledPoller] Starting poll execution.");
        try
        {
            var messages = await sqsHelper.ReceiveMessagesAsync(settings.Aws.SqsQueueUrl, ct: ct);
            if (messages == null || messages.Count == 0)
            {
                logger.LogInformation("[SqsScheduledPoller] No messages found.");
                return new JobResult(JobStatus.Success);
            }

            await processQueueJob.ProcessBatchAsync(messages, ct);
            logger.LogInformation("[SqsScheduledPoller] Finished processing {Count} messages.", messages.Count);


            return new JobResult(JobStatus.Success);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SqsScheduledPoller] Job execution failed.");
            return new JobResult(JobStatus.Failed, Error: ex);
        }
    }
}
