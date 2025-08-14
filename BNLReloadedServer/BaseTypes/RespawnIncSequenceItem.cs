using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class RespawnIncSequenceItem
{
    public float MatchSeconds { get; set; }

    public float RespawnTimeIncSeconds { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        writer.Write(MatchSeconds);
        writer.Write(RespawnTimeIncSeconds);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            MatchSeconds = reader.ReadSingle();
        if (!bitField[1])
            return;
        RespawnTimeIncSeconds = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, RespawnIncSequenceItem value)
    {
        value.Write(writer);
    }

    public static RespawnIncSequenceItem ReadRecord(BinaryReader reader)
    {
        var respawnIncSequenceItem = new RespawnIncSequenceItem();
        respawnIncSequenceItem.Read(reader);
        return respawnIncSequenceItem;
    }
}