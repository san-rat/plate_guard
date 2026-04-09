namespace PlateGuard.IntegrationTests.Infrastructure;

internal static class TestWait
{
    public static async Task UntilAsync(Func<bool> condition, string message, int timeoutMs = 5000, int pollMs = 50)
    {
        var startedAt = DateTime.UtcNow;
        while ((DateTime.UtcNow - startedAt).TotalMilliseconds < timeoutMs)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(pollMs);
        }

        throw new InvalidOperationException(message);
    }
}
