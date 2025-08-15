using System.Net.Sockets;
using NetCoreServer;

namespace BNLReloadedServer.Servers
{
    class MasterSession : TcpSession
    {
        private readonly MasterServiceDispatcher _serviceDispatcher;

        public MasterSession(TcpServer server) : base(server)
        {
            _serviceDispatcher = new MasterServiceDispatcher(new SessionSender(server, this));
        }

        protected override void OnConnected()
        {
            Console.WriteLine($"Master TCP session with Id {Id} connected!");
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Master TCP session with Id {Id} disconnected!");
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
                Console.WriteLine("Master server received packet with incorrect length");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Master TCP session caught an error with code {error}");
        }
    }
}