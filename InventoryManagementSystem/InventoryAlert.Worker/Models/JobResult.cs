namespace InventoryAlert.Worker.Models;

public enum JobStatus
{
    Success,
    Failed,
    Skipped,
    PartiallySucceeded
}
public record JobResult(
    JobStatus Status,
    string Message = "",
    int ProcessedCount = 0,
    Exception? Error = null);



