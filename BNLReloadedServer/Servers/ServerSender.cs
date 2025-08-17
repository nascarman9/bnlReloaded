﻿using NetCoreServer;

namespace BNLReloadedServer.Servers;

public class ServerSender(TcpServer server) : ISender
{
    public uint? AssociatedPlayerId { get; set; }

    public void Send(BinaryWriter writer)
    {
        server.Multicast(AppendMessageLength(writer));
    }

    public void Send(byte[] buffer)
    {
        server.Multicast(buffer);
    }

    public void SendSync(BinaryWriter writer)
    {
        server.Multicast(AppendMessageLength(writer));
    }

    public void SendSync(byte[] buffer)
    {
        server.Multicast(buffer);
    }

    private static byte[] AppendMessageLength(BinaryWriter writer)
    {
        var memStream = new MemoryStream();
        var baseStream = writer.BaseStream as MemoryStream;
        using var packetWriter = new BinaryWriter(memStream);
        packetWriter.Write7BitEncodedInt((int)baseStream!.Length);
        packetWriter.Write(baseStream.GetBuffer(), 0, (int)baseStream.Length);
        return (packetWriter.BaseStream as MemoryStream)!.ToArray();
    }
}