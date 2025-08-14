using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectZoneEffect : InstEffect
{
    public override InstEffectType Type => InstEffectType.ZoneEffect;

    public float Duration { get; set; }

    public List<Key>? Effects { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true, Effects != null).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      writer.Write(Duration);
      if (Effects != null)
        writer.WriteList(Effects, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (bitField[3])
        Duration = reader.ReadSingle();
      Effects = bitField[4] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectZoneEffect value)
    {
      value.Write(writer);
    }

    public static InstEffectZoneEffect ReadRecord(BinaryReader reader)
    {
      var effectZoneEffect = new InstEffectZoneEffect();
      effectZoneEffect.Read(reader);
      return effectZoneEffect;
    }
}