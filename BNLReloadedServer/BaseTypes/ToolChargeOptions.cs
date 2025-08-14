using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ToolChargeOptions
{
    public ToolBullet? Bullet { get; set; }

    public float? Bloom { get; set; }

    public Recoil? Recoil { get; set; }

    public MultipleBullets? Bullets { get; set; }

    public float AttackTime { get; set; }

    public float AmmoRateMultiplier { get; set; }

    public float? Range { get; set; }

    public bool HitOnOutOfRange { get; set; }

    public InstEffect? HitEffect { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Bullet != null, Bloom.HasValue, Recoil != null, Bullets != null, true, true, Range.HasValue, true, HitEffect != null).Write(writer);
      if (Bullet != null) 
        ToolBullet.WriteVariant(writer, Bullet);
      if (Bloom.HasValue)
        writer.Write(Bloom.Value);
      if (Recoil != null)
        Recoil.WriteRecord(writer, Recoil);
      if (Bullets != null)
        MultipleBullets.WriteRecord(writer, Bullets);
      writer.Write(AttackTime);
      writer.Write(AmmoRateMultiplier);
      if (Range.HasValue)
        writer.Write(Range.Value);
      writer.Write(HitOnOutOfRange);
      if (HitEffect == null)
        return;
      InstEffect.WriteVariant(writer, HitEffect);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(9);
      bitField.Read(reader);
      Bullet = bitField[0] ? ToolBullet.ReadVariant(reader) : null;
      Bloom = bitField[1] ? reader.ReadSingle() : null;
      Recoil = bitField[2] ? Recoil.ReadRecord(reader) : null;
      Bullets = bitField[3] ? MultipleBullets.ReadRecord(reader) : null;
      if (bitField[4])
        AttackTime = reader.ReadSingle();
      if (bitField[5])
        AmmoRateMultiplier = reader.ReadSingle();
      Range = bitField[6] ? reader.ReadSingle() : null;
      if (bitField[7])
        HitOnOutOfRange = reader.ReadBoolean();
      HitEffect = bitField[8] ? InstEffect.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ToolChargeOptions value)
    {
      value.Write(writer);
    }

    public static ToolChargeOptions ReadRecord(BinaryReader reader)
    {
      var toolChargeOptions = new ToolChargeOptions();
      toolChargeOptions.Read(reader);
      return toolChargeOptions;
    }
}