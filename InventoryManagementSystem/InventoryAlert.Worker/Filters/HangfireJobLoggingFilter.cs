using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryAlert.Worker.Filters;

/// <summary>
/// Global Hangfire job filter that intercepts all job failures and routes them
/// through ILogger — consistent with the rest of the system.
/// Also tracks retry attempt count and emits a structured warning per retry.
/// </summary>
public sealed class HangfireJobLoggingFilter : JobFilterAttribute, IApplyStateFilter
{
    // ILoggerFactory cannot be constructor-injected because Hangfire creates
    // filter instances before the DI container is ready. Set it once at startup.
    private static ILoggerFactory? _loggerFactory;

    public static void SetLoggerFactory(ILoggerFactory loggerFactory)
        => _loggerFactory = loggerFactory;

    private static ILogger GetLogger()
        => _loggerFactory?.CreateLogger("HangfireJobFilter")
           ?? NullLogger.Instance;

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        var logger = GetLogger();

        switch (context.NewState)
        {
            case FailedState failed:
                logger.LogError(
                    failed.Exception,
                    "[HangfireJob] ❌ Job {JobName} failed. Reason: {Reason}",
                    context.BackgroundJob.Job.Type.Name,
                    failed.Reason);
                break;

            case EnqueuedState when context.OldStateName == FailedState.StateName:
                // Job is being re-queued after failure (retry)
                logger.LogWarning(
                    "[HangfireJob] ♻ Job {JobName} scheduled for retry.",
                    context.BackgroundJob.Job.Type.Name);
                break;
        }
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        // No-op: transitions handled in OnStateApplied.
    }
}
