using System.Net.Sockets;
using BNLReloadedServer.Database;
using NetCoreServer;

namespace BNLReloadedServer.Servers
{
    internal class RegionSession : TcpSession
    {
        private readonly RegionServiceDispatcher _serviceDispatcher;
        private readonly ISender _sender;

        public RegionSession(TcpServer server) : base(server)
        {
            _sender = new SessionSender(this);
            _serviceDispatcher = new RegionServiceDispatcher(_sender, Id);
        }

        protected override void OnConnected()
        {
            Console.WriteLine($"Region TCP session with Id {Id} connected!");
        }

        protected override void OnDisconnected()
        {
            if (_sender.AssociatedPlayerId != null)
                Databases.RegionServerDatabase.RemoveUser(_sender.AssociatedPlayerId.Value);
            Databases.RegionServerDatabase.RemoveServices(Id);
            Console.WriteLine($"Region TCP session with Id {Id} disconnected!");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            if (size <= 0) return;
            
            var memStream = new MemoryStream(buffer, (int)offset, (int)size);
            using var reader = new BinaryReader(memStream);

            try
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    // The first part of every packet is an 7 bit encoded int of its length.
                    var packetLength = reader.Read7BitEncodedInt();
                    var currentPosition = reader.BaseStream.Position;
                    if (reader.BaseStream.Position + packetLength <= reader.BaseStream.Length)
                    {
                        Console.WriteLine($"Packet length: {packetLength}");
                        _serviceDispatcher.Dispatch(reader);
                        Console.WriteLine();
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
            Console.WriteLine($"Region TCP session caught an error with code {error}");
        }
    }
}