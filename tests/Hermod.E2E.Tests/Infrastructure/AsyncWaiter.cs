namespace Hermod.E2E.Tests.Infrastructure;

/// <summary>
/// Polls a condition until it becomes true or a timeout expires.
/// Used to wait for asynchronous Wolverine cascades and NATS transport to complete.
/// </summary>
public static class AsyncWaiter
{
    public static async Task WaitForConditionAsync(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        string? message = null)
    {
        timeout ??= TimeSpan.FromSeconds(15);
        pollInterval ??= TimeSpan.FromMilliseconds(500);

        using var cts = new CancellationTokenSource(timeout.Value);
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (await condition())
                    return;
                await Task.Delay(pollInterval.Value, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Fall through to TimeoutException
        }

        throw new TimeoutException(
            message ?? $"Condition was not met within {timeout.Value.TotalSeconds}s.");
    }
}
