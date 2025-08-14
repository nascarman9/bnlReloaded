using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectPurge : InstEffect
{
    public override InstEffectType Type => InstEffectType.Purge;

    public bool Positive { get; set; }

    public bool Negative { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true, true).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      writer.Write(Positive);
      writer.Write(Negative);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (bitField[3])
        Positive = reader.ReadBoolean();
      if (!bitField[4])
        return;
      Negative = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectPurge value)
    {
      value.Write(writer);
    }

    public static InstEffectPurge ReadRecord(BinaryReader reader)
    {
      var instEffectPurge = new InstEffectPurge();
      instEffectPurge.Read(reader);
      return instEffectPurge;
    }
}