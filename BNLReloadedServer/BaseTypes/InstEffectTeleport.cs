using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectTeleport : InstEffect
{
    public override InstEffectType Type => InstEffectType.Teleport;

    public UnitLabel Anchor { get; set; }

    public bool OwnedAnchorOnly { get; set; }

    public bool DestroyAnchor { get; set; }

    public float? RangeLimit { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true, true, true, RangeLimit.HasValue).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      writer.WriteByteEnum(Anchor);
      writer.Write(OwnedAnchorOnly);
      writer.Write(DestroyAnchor);
      if (!RangeLimit.HasValue)
        return;
      writer.Write(RangeLimit.Value);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (bitField[3])
        Anchor = reader.ReadByteEnum<UnitLabel>();
      if (bitField[4])
        OwnedAnchorOnly = reader.ReadBoolean();
      if (bitField[5])
        DestroyAnchor = reader.ReadBoolean();
      RangeLimit = bitField[6] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectTeleport value)
    {
      value.Write(writer);
    }

    public static InstEffectTeleport ReadRecord(BinaryReader reader)
    {
      var instEffectTeleport = new InstEffectTeleport();
      instEffectTeleport.Read(reader);
      return instEffectTeleport;
    }
}