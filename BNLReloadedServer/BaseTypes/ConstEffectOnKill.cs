using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectOnKill : ConstEffect
{
    public override ConstEffectType Type => ConstEffectType.OnKill;

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

    public static void WriteRecord(BinaryWriter writer, ConstEffectOnKill value)
    {
      value.Write(writer);
    }

    public static ConstEffectOnKill ReadRecord(BinaryReader reader)
    {
      var constEffectOnKill = new ConstEffectOnKill();
      constEffectOnKill.Read(reader);
      return constEffectOnKill;
    }
}