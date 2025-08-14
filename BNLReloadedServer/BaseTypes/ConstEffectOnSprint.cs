using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectOnSprint : ConstEffect
{
    public override ConstEffectType Type => ConstEffectType.OnSprint;

    public List<Key>? ConstantEffects { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Targeting != null, ConstantEffects != null).Write(writer);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (ConstantEffects != null)
        writer.WriteList(ConstantEffects, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      Targeting = bitField[0] ? EffectTargeting.ReadRecord(reader) : null;
      ConstantEffects = bitField[1] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ConstEffectOnSprint value)
    {
      value.Write(writer);
    }

    public static ConstEffectOnSprint ReadRecord(BinaryReader reader)
    {
      var constEffectOnSprint = new ConstEffectOnSprint();
      constEffectOnSprint.Read(reader);
      return constEffectOnSprint;
    }
}