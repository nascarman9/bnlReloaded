using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectOnNearbyBlock : ConstEffect
{
    public override ConstEffectType Type => ConstEffectType.OnNearbyBlock;

    public float Radius { get; set; }

    public List<Key>? Blocks { get; set; }

    public List<Key>? Effects { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Targeting != null, true, Blocks != null, Effects != null).Write(writer);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      writer.Write(Radius);
      if (Blocks != null)
        writer.WriteList(Blocks, Key.WriteRecord);
      if (Effects != null)
        writer.WriteList(Effects, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Targeting = bitField[0] ? EffectTargeting.ReadRecord(reader) : null;
      if (bitField[1])
        Radius = reader.ReadSingle();
      Blocks = bitField[2] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Effects = bitField[3] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ConstEffectOnNearbyBlock value)
    {
      value.Write(writer);
    }

    public static ConstEffectOnNearbyBlock ReadRecord(BinaryReader reader)
    {
      var effectOnNearbyBlock = new ConstEffectOnNearbyBlock();
      effectOnNearbyBlock.Read(reader);
      return effectOnNearbyBlock;
    }
}