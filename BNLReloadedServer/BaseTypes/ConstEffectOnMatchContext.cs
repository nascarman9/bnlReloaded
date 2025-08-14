using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectOnMatchContext : ConstEffect
{
    public override ConstEffectType Type => ConstEffectType.OnMatchContext;

    public List<Key>? EffectsOnLeading { get; set; }

    public List<Key>? EffectsOnLosing { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Targeting != null, EffectsOnLeading != null, EffectsOnLosing != null).Write(writer);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (EffectsOnLeading != null)
        writer.WriteList(EffectsOnLeading, Key.WriteRecord);
      if (EffectsOnLosing != null)
        writer.WriteList(EffectsOnLosing, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Targeting = bitField[0] ? EffectTargeting.ReadRecord(reader) : null;
      EffectsOnLeading = bitField[1] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord): null;
      EffectsOnLosing = bitField[2] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord): null;
    }

    public static void WriteRecord(BinaryWriter writer, ConstEffectOnMatchContext value)
    {
      value.Write(writer);
    }

    public static ConstEffectOnMatchContext ReadRecord(BinaryReader reader)
    {
      var effectOnMatchContext = new ConstEffectOnMatchContext();
      effectOnMatchContext.Read(reader);
      return effectOnMatchContext;
    }
}