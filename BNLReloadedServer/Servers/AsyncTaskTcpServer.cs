using System.Net;
using NetCoreServer;

namespace BNLReloadedServer.Servers;

public class AsyncTaskTcpServer(IPAddress address, int port) : TcpServer(address, port)
{
    private readonly Dictionary<Guid, AsyncSenderTask> _senderTasks = new();

    public void AddSenderTask(Guid senderId, AsyncSenderTask task)
    {
        if (_senderTasks.TryGetValue(senderId, out var value))
        {
            value.Stop();
        }

        _senderTasks[senderId] = task;
    }

    private void RemoveSenderTask(Guid senderId)
    {
        if (_senderTasks.Remove(senderId, out var value))
        {
            value.Stop();
        }
    }
    
    protected override void OnDisconnected(TcpSession session)
    {
        RemoveSenderTask(session.Id);
    }

    public AsyncSenderTask? FindAsyncSenderTask(Guid guid) => _senderTasks.GetValueOrDefault(guid);
}