using System.Threading.Channels;
using InventoryAlert.Worker.Interfaces;

namespace InventoryAlert.Worker.Infrastructure.Messaging;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

    public BackgroundTaskQueue(int capacity)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
    }

    public async ValueTask<Func<CancellationToken, ValueTask>?> DequeueAsync(CancellationToken ct)
    {
        if(await _queue.Reader.WaitToReadAsync(ct))
        {
            if(_queue.Reader.TryRead(out var workItem))
            {
                return workItem;
            }
        }
        return null;
    }

    public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        await _queue.Writer.WriteAsync(workItem);
    }
}
