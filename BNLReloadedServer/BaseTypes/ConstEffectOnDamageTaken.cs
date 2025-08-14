using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectOnDamageTaken : ConstEffect
{
    public override ConstEffectType Type => ConstEffectType.OnDamageTaken;

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

    public static void WriteRecord(BinaryWriter writer, ConstEffectOnDamageTaken value)
    {
      value.Write(writer);
    }

    public static ConstEffectOnDamageTaken ReadRecord(BinaryReader reader)
    {
      var effectOnDamageTaken = new ConstEffectOnDamageTaken();
      effectOnDamageTaken.Read(reader);
      return effectOnDamageTaken;
    }
}