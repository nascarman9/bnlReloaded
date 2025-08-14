using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectImmunity : ConstEffect
{
    public override ConstEffectType Type => ConstEffectType.Immunity;

    public bool Positive { get; set; }

    public bool Negative { get; set; }

    public List<EffectLabel>? EffectLabels { get; set; }

    public List<Key>? EffectKeys { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Targeting != null, true, true, EffectLabels != null, EffectKeys != null).Write(writer);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      writer.Write(Positive);
      writer.Write(Negative);
      if (EffectLabels != null)
        writer.WriteList(EffectLabels, writer.WriteByteEnum);
      if (EffectKeys != null)
        writer.WriteList(EffectKeys, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Targeting = bitField[0] ? EffectTargeting.ReadRecord(reader) : null;
      if (bitField[1])
        Positive = reader.ReadBoolean();
      if (bitField[2])
        Negative = reader.ReadBoolean();
      EffectLabels = bitField[3] ? reader.ReadList<EffectLabel, List<EffectLabel>>(reader.ReadByteEnum<EffectLabel>) : null;
      EffectKeys = bitField[4] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ConstEffectImmunity value)
    {
      value.Write(writer);
    }

    public static ConstEffectImmunity ReadRecord(BinaryReader reader)
    {
      var constEffectImmunity = new ConstEffectImmunity();
      constEffectImmunity.Read(reader);
      return constEffectImmunity;
    }
}