using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ToolSpinup : Tool
{
    public override ToolType Type => ToolType.Spinup;

    public float Range { get; set; }

    public ToolBullet? Bullet { get; set; }

    public float? Bloom { get; set; }

    public Recoil? Recoil { get; set; }

    public MultipleBullets? Bullets { get; set; }

    public float SpinupTime { get; set; }

    public InstEffect? HitEffect { get; set; }

    public ToolTiming? Timing { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Ammo != null, true, true, Bullet != null, Bloom.HasValue, Recoil != null, Bullets != null, true, HitEffect != null, Timing != null).Write(writer);
      if (Ammo != null)
        ToolAmmo.WriteRecord(writer, Ammo);
      writer.Write(AutoSwitch);
      writer.Write(Range);
      if (Bullet != null)
        ToolBullet.WriteVariant(writer, Bullet);
      if (Bloom.HasValue)
        writer.Write(Bloom.Value);
      if (Recoil != null)
        Recoil.WriteRecord(writer, Recoil);
      if (Bullets != null)
        MultipleBullets.WriteRecord(writer, Bullets);
      writer.Write(SpinupTime);
      if (HitEffect != null)
        InstEffect.WriteVariant(writer, HitEffect);
      if (Timing != null)
        ToolTiming.WriteRecord(writer, Timing);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(10);
      bitField.Read(reader);
      Ammo = bitField[0] ? ToolAmmo.ReadRecord(reader) : null;
      if (bitField[1])
        AutoSwitch = reader.ReadBoolean();
      if (bitField[2])
        Range = reader.ReadSingle();
      Bullet = bitField[3] ? ToolBullet.ReadVariant(reader) : null;
      Bloom = bitField[4] ? reader.ReadSingle() : null;
      Recoil = bitField[5] ? Recoil.ReadRecord(reader) : null;
      Bullets = bitField[6] ? MultipleBullets.ReadRecord(reader) : null;
      if (bitField[7])
        SpinupTime = reader.ReadSingle();
      HitEffect = bitField[8] ? InstEffect.ReadVariant(reader) : null;
      Timing = bitField[9] ? ToolTiming.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ToolSpinup value) => value.Write(writer);

    public static ToolSpinup ReadRecord(BinaryReader reader)
    {
      var toolSpinup = new ToolSpinup();
      toolSpinup.Read(reader);
      return toolSpinup;
    }
}