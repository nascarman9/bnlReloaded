using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ToolShot : Tool
{
    public override ToolType Type => ToolType.Shot;

    public bool AutoFire { get; set; }

    public float Range { get; set; }

    public bool HitOnOutOfRange { get; set; }

    public ToolBullet? Bullet { get; set; }

    public float? Bloom { get; set; }

    public Recoil? Recoil { get; set; }

    public MultipleBullets? Bullets { get; set; }

    public InstEffect? HitEffect { get; set; }

    public ToolTiming? Timing { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Ammo != null, true, true, true, true, Bullet != null, Bloom.HasValue, Recoil != null,
        Bullets != null, HitEffect != null, Timing != null).Write(writer);
      if (Ammo != null)
        ToolAmmo.WriteRecord(writer, Ammo);
      writer.Write(AutoSwitch);
      writer.Write(AutoFire);
      writer.Write(Range);
      writer.Write(HitOnOutOfRange);
      if (Bullet != null)
        ToolBullet.WriteVariant(writer, Bullet);
      if (Bloom.HasValue)
        writer.Write(Bloom.Value);
      if (Recoil != null)
        Recoil.WriteRecord(writer, Recoil);
      if (Bullets != null)
        MultipleBullets.WriteRecord(writer, Bullets);
      if (HitEffect != null)
        InstEffect.WriteVariant(writer, HitEffect);
      if (Timing != null)
        ToolTiming.WriteRecord(writer, Timing);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(11);
      bitField.Read(reader);
      Ammo = bitField[0] ? ToolAmmo.ReadRecord(reader) : null;
      if (bitField[1])
        AutoSwitch = reader.ReadBoolean();
      if (bitField[2])
        AutoFire = reader.ReadBoolean();
      if (bitField[3])
        Range = reader.ReadSingle();
      if (bitField[4])
        HitOnOutOfRange = reader.ReadBoolean();
      Bullet = bitField[5] ? ToolBullet.ReadVariant(reader) : null;
      Bloom = bitField[6] ? reader.ReadSingle() : null;
      Recoil = bitField[7] ? Recoil.ReadRecord(reader) : null;
      Bullets = bitField[8] ? MultipleBullets.ReadRecord(reader) : null;
      HitEffect = bitField[9] ? InstEffect.ReadVariant(reader) : null;
      Timing = bitField[10] ? ToolTiming.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ToolShot value) => value.Write(writer);

    public static ToolShot ReadRecord(BinaryReader reader)
    {
      var toolShot = new ToolShot();
      toolShot.Read(reader);
      return toolShot;
    }
}