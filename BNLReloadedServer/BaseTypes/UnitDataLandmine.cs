using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitDataLandmine : UnitData
{
    public override UnitType Type => UnitType.Landmine;

    public float? Timeout { get; set; }

    public float TriggerRadius { get; set; }

    public float BeepRadius { get; set; }

    public bool HitOnTimeout { get; set; }

    public InstEffect? TriggerEffect { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Timeout.HasValue, true, true, true, TriggerEffect != null).Write(writer);
      if (Timeout.HasValue)
        writer.Write(Timeout.Value);
      writer.Write(TriggerRadius);
      writer.Write(BeepRadius);
      writer.Write(HitOnTimeout);
      if (TriggerEffect != null)
        InstEffect.WriteVariant(writer, TriggerEffect);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Timeout = bitField[0] ? reader.ReadSingle() : null;
      if (bitField[1])
        TriggerRadius = reader.ReadSingle();
      if (bitField[2])
        BeepRadius = reader.ReadSingle();
      if (bitField[3])
        HitOnTimeout = reader.ReadBoolean();
      TriggerEffect = bitField[4] ? InstEffect.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, UnitDataLandmine value)
    {
      value.Write(writer);
    }

    public static UnitDataLandmine ReadRecord(BinaryReader reader)
    {
      var unitDataLandmine = new UnitDataLandmine();
      unitDataLandmine.Read(reader);
      return unitDataLandmine;
    }
}