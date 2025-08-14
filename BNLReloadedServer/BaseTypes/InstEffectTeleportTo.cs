using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectTeleportTo : InstEffect
{
    public override InstEffectType Type => InstEffectType.TeleportTo;

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (!Impact.HasValue)
        return;
      Key.WriteRecord(writer, Impact.Value);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectTeleportTo value)
    {
      value.Write(writer);
    }

    public static InstEffectTeleportTo ReadRecord(BinaryReader reader)
    {
      var effectTeleportTo = new InstEffectTeleportTo();
      effectTeleportTo.Read(reader);
      return effectTeleportTo;
    }
}