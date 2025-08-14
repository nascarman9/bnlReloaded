using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectOnGearSwitch : ConstEffect
{
    public override ConstEffectType Type => ConstEffectType.OnGearSwitch;

    public InstEffect? Effect { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Targeting != null, Effect != null).Write(writer);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Effect != null)
        InstEffect.WriteVariant(writer, Effect);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      Targeting = bitField[0] ? EffectTargeting.ReadRecord(reader) : null;
      Effect = bitField[1] ? InstEffect.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ConstEffectOnGearSwitch value)
    {
      value.Write(writer);
    }

    public static ConstEffectOnGearSwitch ReadRecord(BinaryReader reader)
    {
      var effectOnGearSwitch = new ConstEffectOnGearSwitch();
      effectOnGearSwitch.Read(reader);
      return effectOnGearSwitch;
    }
}