using System.Diagnostics;

namespace PlateGuard.Cloud;

public interface ISyncLog
{
    void Info(string message);
}

public sealed class TraceSyncLog : ISyncLog
{
    public void Info(string message)
    {
        Trace.WriteLine(message);
    }
}
