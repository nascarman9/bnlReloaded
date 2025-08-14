using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectPull : ConstEffect
{
    public override ConstEffectType Type => ConstEffectType.Pull;

    public float Force { get; set; }

    public bool BindToUnit { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Targeting != null, true, true).Write(writer);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      writer.Write(Force);
      writer.Write(BindToUnit);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Targeting = bitField[0] ? EffectTargeting.ReadRecord(reader) : null;
      if (bitField[1])
        Force = reader.ReadSingle();
      if (!bitField[2])
        return;
      BindToUnit = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, ConstEffectPull value)
    {
      value.Write(writer);
    }

    public static ConstEffectPull ReadRecord(BinaryReader reader)
    {
      var constEffectPull = new ConstEffectPull();
      constEffectPull.Read(reader);
      return constEffectPull;
    }
}