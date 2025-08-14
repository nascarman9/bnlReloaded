using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitDataMortar : UnitData
{
    public override UnitType Type => UnitType.Mortar;

    public float Angle { get; set; }

    public Key ProjectileKey { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        writer.Write(Angle);
        Key.WriteRecord(writer, ProjectileKey);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            Angle = reader.ReadSingle();
        if (!bitField[1])
            return;
        ProjectileKey = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, UnitDataMortar value)
    {
        value.Write(writer);
    }

    public static UnitDataMortar ReadRecord(BinaryReader reader)
    {
        var unitDataMortar = new UnitDataMortar();
        unitDataMortar.Read(reader);
        return unitDataMortar;
    }
}