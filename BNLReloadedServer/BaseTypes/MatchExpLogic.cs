using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchExpLogic
{
    public float XpPerMinute { get; set; }

    public float MinXpCap { get; set; }

    public float MaxXpCap { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true).Write(writer);
        writer.Write(XpPerMinute);
        writer.Write(MinXpCap);
        writer.Write(MaxXpCap);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            XpPerMinute = reader.ReadSingle();
        if (bitField[1])
            MinXpCap = reader.ReadSingle();
        if (!bitField[2])
            return;
        MaxXpCap = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, MatchExpLogic value) => value.Write(writer);

    public static MatchExpLogic ReadRecord(BinaryReader reader)
    {
        var matchExpLogic = new MatchExpLogic();
        matchExpLogic.Read(reader);
        return matchExpLogic;
    }
}