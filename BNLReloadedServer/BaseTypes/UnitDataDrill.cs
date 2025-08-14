using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitDataDrill : UnitData
{
    public override UnitType Type => UnitType.Drill;

    public float StartDelay { get; set; }

    public float MoveSpeed { get; set; }

    public float HitInterval { get; set; }

    public int HitsLimit { get; set; }

    public InstEffect? HitEffect { get; set; }

    public InstEffect? DeathEffect { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, HitEffect != null, DeathEffect != null).Write(writer);
      writer.Write(StartDelay);
      writer.Write(MoveSpeed);
      writer.Write(HitInterval);
      writer.Write(HitsLimit);
      if (HitEffect != null)
        InstEffect.WriteVariant(writer, HitEffect);
      if (DeathEffect != null)
        InstEffect.WriteVariant(writer, DeathEffect);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      if (bitField[0])
        StartDelay = reader.ReadSingle();
      if (bitField[1])
        MoveSpeed = reader.ReadSingle();
      if (bitField[2])
        HitInterval = reader.ReadSingle();
      if (bitField[3])
        HitsLimit = reader.ReadInt32();
      HitEffect = bitField[4] ? InstEffect.ReadVariant(reader) : null;
      DeathEffect = bitField[5] ? InstEffect.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, UnitDataDrill value) => value.Write(writer);

    public static UnitDataDrill ReadRecord(BinaryReader reader)
    {
      var unitDataDrill = new UnitDataDrill();
      unitDataDrill.Read(reader);
      return unitDataDrill;
    }
}