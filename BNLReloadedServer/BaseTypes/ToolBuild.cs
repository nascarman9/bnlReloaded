using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ToolBuild : Tool
{
    public override ToolType Type => ToolType.Build;

    public float Range { get; set; }

    public ControlLockType Lock { get; set; }

    public bool ShowGhost { get; set; }

    public bool CastOnRelease { get; set; }

    public ToolBullet? Bullet { get; set; }

    public Key? BuildImpact { get; set; }

    public ToolTiming? Timing { get; set; }

    public float BuildModifier { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Ammo != null, true, true, true, true, true, Bullet != null, BuildImpact.HasValue, Timing != null, true).Write(writer);
      if (Ammo != null)
        ToolAmmo.WriteRecord(writer, Ammo);
      writer.Write(AutoSwitch);
      writer.Write(Range);
      writer.WriteByteEnum(Lock);
      writer.Write(ShowGhost);
      writer.Write(CastOnRelease);
      if (Bullet != null)
        ToolBullet.WriteVariant(writer, Bullet);
      if (BuildImpact.HasValue)
        Key.WriteRecord(writer, BuildImpact.Value);
      if (Timing != null)
        ToolTiming.WriteRecord(writer, Timing);
      writer.Write(BuildModifier);
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
      if (bitField[3])
        Lock = reader.ReadByteEnum<ControlLockType>();
      if (bitField[4])
        ShowGhost = reader.ReadBoolean();
      if (bitField[5])
        CastOnRelease = reader.ReadBoolean();
      Bullet = bitField[6] ? ToolBullet.ReadVariant(reader) : null;
      BuildImpact = bitField[7] ? Key.ReadRecord(reader) : null;
      Timing = bitField[8] ? ToolTiming.ReadRecord(reader) : null;
      if (!bitField[9])
        return;
      BuildModifier = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, ToolBuild value) => value.Write(writer);

    public static ToolBuild ReadRecord(BinaryReader reader)
    {
      var toolBuild = new ToolBuild();
      toolBuild.Read(reader);
      return toolBuild;
    }
}