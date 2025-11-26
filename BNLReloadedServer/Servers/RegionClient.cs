using System.Net.Sockets;
using BNLReloadedServer.Database;
using TcpClient = NetCoreServer.TcpClient;

namespace BNLReloadedServer.Servers;

public class RegionClient : TcpClient
{
    private readonly RegionClientServiceDispatcher _serviceDispatcher;

    public RegionClient(string address, int port) : base(address, port)
    {
        var sender = new ClientSender(this);
        _serviceDispatcher = new RegionClientServiceDispatcher(sender);
    }
    
    public void DisconnectAndStop()
    {
        _stop = true;
        DisconnectAsync();
        while (IsConnected)
            Thread.Yield();
    }

    protected override void OnConnected()
    {
        if (Databases.ConfigDatabase.DebugMode())
        {
            Console.WriteLine($"Region TCP client connected a new session with Id {Id}");
        }

        var host = Databases.ConfigDatabase.RegionHost();
        var guiInfo = Databases.ConfigDatabase.GetRegionInfo();
        
        Databases.PlayerDatabase.SetRegionServerService(_serviceDispatcher.ServiceRegionServer);
        _serviceDispatcher.ServiceRegionServer.SendRegionInfo(host, guiInfo);
    }

    protected override void OnDisconnected()
    {
        if (Databases.ConfigDatabase.DebugMode())
        {
            Console.WriteLine($"Region TCP client disconnected a session with Id {Id}");
        }

        // Wait for a while...
        Thread.Sleep(1000);

        // Try to connect again
        if (!_stop)
            ConnectAsync();
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        if (size <= 0) return;
            
        var memStream = new MemoryStream(buffer, (int)offset, (int)size);
        using var reader = new BinaryReader(memStream);

        var debugMode = Databases.ConfigDatabase.DebugMode();
        try
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                // The first part of every packet is an 7 bit encoded int of its length.
                var packetLength = reader.Read7BitEncodedInt();
                var currentPosition = reader.BaseStream.Position;
                if (reader.BaseStream.Position + packetLength <= reader.BaseStream.Length)
                {
                    if (debugMode)
                    {
                        Console.WriteLine($"Packet length: {packetLength}");
                        _serviceDispatcher.Dispatch(reader);
                        Console.WriteLine();
                    }
                    else
                    {
                        _serviceDispatcher.Dispatch(reader);
                    }
                }
                else
                    break;

                if (reader.BaseStream.Position < currentPosition + packetLength)
                {
                    reader.ReadBytes((int) (currentPosition + packetLength - reader.BaseStream.Position));
                }
            }
        }
        catch (EndOfStreamException)
        {
            Console.WriteLine("Region server received packet with incorrect length");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"Chat TCP client caught an error with code {error}");
    }

    private bool _stop;
}
