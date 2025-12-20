namespace BNLReloadedServer.Servers;

public class SessionReader(IServiceDispatcher dispatcher, bool debugMode, string onError)
{
    private bool _packetInBuffer;
    private const int BodyMaxSize = 100000000;
    private MemoryStream _buffer = new();

    public void ProcessPacket(byte[] buffer, long offset, long size)
    {
        MemoryStream memStream;
        if (_packetInBuffer)
        {
            if (_buffer.Length > BodyMaxSize)
            {
                WipeBuffer();
                return;
            }
           
            var bufferPos = _buffer.Position;
            _buffer.Seek(_buffer.Length - _buffer.Position, SeekOrigin.Current);
            _buffer.Write(buffer, (int)offset, (int)size);
            _buffer.Position = bufferPos;
            memStream = new MemoryStream(_buffer.GetBuffer(), (int)_buffer.Position, (int)_buffer.Length);
        }
        else
        {
            memStream = new MemoryStream(buffer, (int)offset, (int)size);
        }
        
        using var reader = new BinaryReader(memStream);
        try
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                // The first part of every packet is an 7 bit encoded int of its length.
                var startPosition = reader.BaseStream.Position;
                var startLength = reader.BaseStream.Length;
                
                var packetLength = reader.Read7BitEncodedInt();
                if (reader.BaseStream.Position + packetLength > reader.BaseStream.Length)
                {
                    if (Math.Max(startLength - startPosition, 0) > 0)
                    {
                        _packetInBuffer = true;
                        memStream.Position = startPosition;
                        _buffer.SetLength(0);
                        memStream.CopyTo(_buffer);
                        _buffer.Position = 0;
                    }
                    
                    break;
                }
                
                var currentPosition = reader.BaseStream.Position;
                if (debugMode)
                {
                    Console.WriteLine($"Packet length: {packetLength}");
                    var res = dispatcher.Dispatch(reader);
                    Console.WriteLine();
                    if (!res)
                    {
                        if (_packetInBuffer)
                            WipeBuffer();
                        break;
                    }
                }
                else
                {
                    if (!dispatcher.Dispatch(reader))
                    {
                        if (_packetInBuffer)
                            WipeBuffer();
                        break;
                    }
                }

                if (reader.BaseStream.Position < currentPosition + packetLength)
                {
                    reader.ReadBytes((int)(currentPosition + packetLength - reader.BaseStream.Position));
                }

                if (_packetInBuffer) 
                    WipeBuffer();
            }
        }
        catch (EndOfStreamException)
        {
            Console.WriteLine(onError);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void WipeBuffer()
    {
        _buffer = new MemoryStream();
        _packetInBuffer = false;
    }
}