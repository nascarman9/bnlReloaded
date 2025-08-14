using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitDataShower : UnitData
{
    public override UnitType Type => UnitType.Shower;

    public float InitDelay { get; set; }

    public float HitDelay { get; set; }

    public float HitDelayRandom { get; set; }

    public float Radius { get; set; }

    public InstEffect? HitEffect { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, HitEffect != null).Write(writer);
      writer.Write(InitDelay);
      writer.Write(HitDelay);
      writer.Write(HitDelayRandom);
      writer.Write(Radius);
      if (HitEffect != null)
        InstEffect.WriteVariant(writer, HitEffect);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      if (bitField[0])
        InitDelay = reader.ReadSingle();
      if (bitField[1])
        HitDelay = reader.ReadSingle();
      if (bitField[2])
        HitDelayRandom = reader.ReadSingle();
      if (bitField[3])
        Radius = reader.ReadSingle();
      HitEffect = bitField[4] ? InstEffect.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, UnitDataShower value)
    {
      value.Write(writer);
    }

    public static UnitDataShower ReadRecord(BinaryReader reader)
    {
      var unitDataShower = new UnitDataShower();
      unitDataShower.Read(reader);
      return unitDataShower;
    }
}