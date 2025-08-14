using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectChargeTesla : InstEffect
{
    public override InstEffectType Type => InstEffectType.ChargeTesla;

    public int Charges { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      writer.Write(Charges);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (!bitField[3])
        return;
      Charges = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectChargeTesla value)
    {
      value.Write(writer);
    }

    public static InstEffectChargeTesla ReadRecord(BinaryReader reader)
    {
      var effectChargeTesla = new InstEffectChargeTesla();
      effectChargeTesla.Read(reader);
      return effectChargeTesla;
    }
}