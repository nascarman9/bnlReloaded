using System.Threading.Channels;
using NetCoreServer;

namespace BNLReloadedServer.Servers;

public class AsyncSenderTask
{
    private readonly Channel<byte[]> _packetBuffer = Channel.CreateUnbounded<byte[]>();
    public Guid Id { get; }

    public AsyncSenderTask(TcpSession session)
    {
        Id = session.Id;
        _ = RunSendTask(session, _packetBuffer.Reader);
    }

    public void SendPacket(byte[] packet) => _packetBuffer.Writer.TryWrite(packet);

    private static async Task RunSendTask(TcpSession session, ChannelReader<byte[]> packets)
    {
        try
        {
            await foreach (var packet in packets.ReadAllAsync())
            {
                try
                {
                    session.Send(packet);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    public void Stop() => _packetBuffer.Writer.TryComplete();
}