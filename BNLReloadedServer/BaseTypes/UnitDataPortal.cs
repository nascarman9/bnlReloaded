using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitDataPortal : UnitData
{
    public override UnitType Type => UnitType.Portal;

    public EffectTargeting? UnitsFilter { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(UnitsFilter != null).Write(writer);
        if (UnitsFilter == null)
            return;
        EffectTargeting.WriteRecord(writer, UnitsFilter);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        UnitsFilter = bitField[0] ? EffectTargeting.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, UnitDataPortal value)
    {
        value.Write(writer);
    }

    public static UnitDataPortal ReadRecord(BinaryReader reader)
    {
        var unitDataPortal = new UnitDataPortal();
        unitDataPortal.Read(reader);
        return unitDataPortal;
    }
}