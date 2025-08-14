using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ToolThrow : Tool
{
    public override ToolType Type => ToolType.Throw;

    public ToolBullet? Bullet { get; set; }

    public MultipleBullets? Bullets { get; set; }

    public float? Bloom { get; set; }

    public InstEffect? HitEffect { get; set; }

    public ToolTiming? Timing { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Ammo != null, true, Bullet != null, Bullets != null, Bloom.HasValue, HitEffect != null, Timing != null).Write(writer);
      if (Ammo != null)
        ToolAmmo.WriteRecord(writer, Ammo);
      writer.Write(AutoSwitch);
      if (Bullet != null)
        ToolBullet.WriteVariant(writer, Bullet);
      if (Bullets != null)
        MultipleBullets.WriteRecord(writer, Bullets);
      if (Bloom.HasValue)
        writer.Write(Bloom.Value);
      if (HitEffect != null)
        InstEffect.WriteVariant(writer, HitEffect);
      if (Timing != null)
        ToolTiming.WriteRecord(writer, Timing);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      Ammo = bitField[0] ? ToolAmmo.ReadRecord(reader) : null;
      if (bitField[1])
        AutoSwitch = reader.ReadBoolean();
      Bullet = bitField[2] ? ToolBullet.ReadVariant(reader) : null;
      Bullets = bitField[3] ? MultipleBullets.ReadRecord(reader) : null;
      Bloom = bitField[4] ? reader.ReadSingle() : null;
      HitEffect = bitField[5] ? InstEffect.ReadVariant(reader) : null;
      Timing = bitField[6] ? ToolTiming.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ToolThrow value) => value.Write(writer);

    public static ToolThrow ReadRecord(BinaryReader reader)
    {
      var toolThrow = new ToolThrow();
      toolThrow.Read(reader);
      return toolThrow;
    }
}