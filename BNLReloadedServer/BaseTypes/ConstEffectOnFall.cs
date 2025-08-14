using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectOnFall : ConstEffect
{
    public override ConstEffectType Type => ConstEffectType.OnFall;

    public bool ForceOnly { get; set; }

    public float MinHeight { get; set; }

    public InstEffect? Effect { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Targeting != null, true, true, Effect != null).Write(writer);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      writer.Write(ForceOnly);
      writer.Write(MinHeight);
      if (Effect != null)
        InstEffect.WriteVariant(writer, Effect);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Targeting = bitField[0] ? EffectTargeting.ReadRecord(reader) : null;
      if (bitField[1])
        ForceOnly = reader.ReadBoolean();
      if (bitField[2])
        MinHeight = reader.ReadSingle();
      Effect = bitField[3] ? InstEffect.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ConstEffectOnFall value)
    {
      value.Write(writer);
    }

    public static ConstEffectOnFall ReadRecord(BinaryReader reader)
    {
      var constEffectOnFall = new ConstEffectOnFall();
      constEffectOnFall.Read(reader);
      return constEffectOnFall;
    }
}