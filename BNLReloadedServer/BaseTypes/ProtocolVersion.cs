using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ProtocolVersion
{
    public int Version { get; set; } = 310;

    public int Hash { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        writer.Write(Version);
        writer.Write(Hash);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            Version = reader.ReadInt32();
        if (!bitField[1])
            return;
        Hash = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, ProtocolVersion value)
    {
        value.Write(writer);
    }

    public static ProtocolVersion ReadRecord(BinaryReader reader)
    {
        var protocolVersion = new ProtocolVersion();
        protocolVersion.Read(reader);
        return protocolVersion;
    }
}