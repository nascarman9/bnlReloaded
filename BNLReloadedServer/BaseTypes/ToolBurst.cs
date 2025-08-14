using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ToolBurst : Tool
{
    public override ToolType Type => ToolType.Burst;

    public bool AutoFire { get; set; }

    public float Range { get; set; }

    public bool HitOnOutOfRange { get; set; }

    public List<ToolBullet>? Bullet { get; set; }

    public float? Bloom { get; set; }

    public Recoil? Recoil { get; set; }

    public MultipleBullets? Bullets { get; set; }

    public InstEffect? HitEffect { get; set; }

    public ToolTiming? Timing { get; set; }

    public float BurstShotDelay { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Ammo != null, true, true, true, true, Bullet != null, Bloom.HasValue, Recoil != null,
        Bullets != null, HitEffect != null, Timing != null, true).Write(writer);
      if (Ammo != null)
        ToolAmmo.WriteRecord(writer, Ammo);
      writer.Write(AutoSwitch);
      writer.Write(AutoFire);
      writer.Write(Range);
      writer.Write(HitOnOutOfRange);
      if (Bullet != null)
        writer.WriteList(Bullet, ToolBullet.WriteVariant);
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
      writer.Write(BurstShotDelay);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(12);
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
      Bullet = bitField[5] ? reader.ReadList<ToolBullet, List<ToolBullet>>(ToolBullet.ReadVariant) : null;
      Bloom = bitField[6] ? reader.ReadSingle() : null;
      Recoil = bitField[7] ? Recoil.ReadRecord(reader) : null;
      Bullets = bitField[8] ? MultipleBullets.ReadRecord(reader) : null;
      HitEffect = bitField[9] ? InstEffect.ReadVariant(reader) : null;
      Timing = bitField[10] ? ToolTiming.ReadRecord(reader) : null;
      if (!bitField[11])
        return;
      BurstShotDelay = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, ToolBurst value) => value.Write(writer);

    public static ToolBurst ReadRecord(BinaryReader reader)
    {
      var toolBurst = new ToolBurst();
      toolBurst.Read(reader);
      return toolBurst;
    }
}