using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectOnReload : ConstEffect
{
    public override ConstEffectType Type => ConstEffectType.OnReload;

    public List<Key>? ConstantEffects { get; set; }

    public InstEffect? ReloadStartEffect { get; set; }

    public InstEffect? ReloadEndEffect { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Targeting != null, ConstantEffects != null, ReloadStartEffect != null, ReloadEndEffect != null).Write(writer);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (ConstantEffects != null)
        writer.WriteList(ConstantEffects, Key.WriteRecord);
      if (ReloadStartEffect != null)
        InstEffect.WriteVariant(writer, ReloadStartEffect);
      if (ReloadEndEffect == null)
        return;
      InstEffect.WriteVariant(writer, ReloadEndEffect);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Targeting = bitField[0] ? EffectTargeting.ReadRecord(reader) : null;
      ConstantEffects = bitField[1] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      ReloadStartEffect = bitField[2] ? InstEffect.ReadVariant(reader) : null;
      ReloadEndEffect = bitField[3] ? InstEffect.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ConstEffectOnReload value)
    {
      value.Write(writer);
    }

    public static ConstEffectOnReload ReadRecord(BinaryReader reader)
    {
      var constEffectOnReload = new ConstEffectOnReload();
      constEffectOnReload.Read(reader);
      return constEffectOnReload;
    }
}