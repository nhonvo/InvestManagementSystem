using System.Diagnostics;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryAlert.Worker.Filters;

/// <summary>
/// Global Hangfire job filter that intercepts all job failures and routes them
/// through ILogger — consistent with the rest of the system.
/// Also tracks retry attempt count and emits a structured warning per retry.
/// Logs handler start and completion events.
/// </summary>
public sealed class HangfireJobLoggingFilter : JobFilterAttribute, IApplyStateFilter, IServerFilter
{
    // ILoggerFactory cannot be constructor-injected because Hangfire creates
    // filter instances before the DI container is ready. Set it once at startup.
    private static ILoggerFactory? _loggerFactory;

    public static void SetLoggerFactory(ILoggerFactory loggerFactory)
        => _loggerFactory = loggerFactory;

    private static ILogger GetLogger()
        => _loggerFactory?.CreateLogger("HangfireJobFilter")
           ?? NullLogger.Instance;

    public void OnPerforming(PerformingContext filterContext)
    {
        filterContext.Items["Stopwatch"] = Stopwatch.StartNew();
        GetLogger().LogInformation("handler.started: Job {JobName} execution started.", filterContext.BackgroundJob.Job.Type.Name);
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        var logger = GetLogger();
        var jobName = filterContext.BackgroundJob.Job.Type.Name;
        var stopwatch = filterContext.Items["Stopwatch"] as Stopwatch;
        stopwatch?.Stop();
        var elapsedMs = stopwatch?.Elapsed.TotalMilliseconds ?? 0;
        
        bool succeeded = filterContext.Exception == null && !filterContext.Canceled;

        logger.LogInformation("handler.completed: Job {JobName} execution finished. Succeeded={Succeeded} | ElapsedMs={ElapsedMs:F3}", 
            jobName, succeeded, elapsedMs);
    }

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
