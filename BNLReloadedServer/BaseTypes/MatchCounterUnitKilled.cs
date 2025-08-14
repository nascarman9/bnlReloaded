using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchCounterUnitKilled : MatchCounter
{
    public override MatchCounterType Type => MatchCounterType.UnitKilled;

    public Key Unit { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        Key.WriteRecord(writer, Unit);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        Unit = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, MatchCounterUnitKilled value)
    {
        value.Write(writer);
    }

    public static MatchCounterUnitKilled ReadRecord(BinaryReader reader)
    {
        var counterUnitKilled = new MatchCounterUnitKilled();
        counterUnitKilled.Read(reader);
        return counterUnitKilled;
    }
}