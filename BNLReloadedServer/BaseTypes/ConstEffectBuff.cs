using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectBuff : ConstEffect
{
    public override ConstEffectType Type => ConstEffectType.Buff;

    public Dictionary<BuffType, float>? Buffs { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Targeting != null, Buffs != null).Write(writer);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Buffs != null)
        writer.WriteMap(Buffs, writer.WriteByteEnum, writer.Write);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      Targeting = bitField[0] ? EffectTargeting.ReadRecord(reader) : null;
      Buffs = bitField[1] ? reader.ReadMap<BuffType, float, Dictionary<BuffType, float>>(reader.ReadByteEnum<BuffType>, reader.ReadSingle) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ConstEffectBuff value)
    {
      value.Write(writer);
    }

    public static ConstEffectBuff ReadRecord(BinaryReader reader)
    {
      var constEffectBuff = new ConstEffectBuff();
      constEffectBuff.Read(reader);
      return constEffectBuff;
    }
}