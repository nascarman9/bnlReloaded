using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitDataSkybeam : UnitData
{
    public override UnitType Type => UnitType.Skybeam;

    public float HitInterval { get; set; }

    public InstEffect? HitEffect { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, HitEffect != null).Write(writer);
        writer.Write(HitInterval);
        if (HitEffect != null)
            InstEffect.WriteVariant(writer, HitEffect);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            HitInterval = reader.ReadSingle();
        HitEffect = bitField[1] ? InstEffect.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, UnitDataSkybeam value)
    {
        value.Write(writer);
    }

    public static UnitDataSkybeam ReadRecord(BinaryReader reader)
    {
        var unitDataSkybeam = new UnitDataSkybeam();
        unitDataSkybeam.Read(reader);
        return unitDataSkybeam;
    }
}