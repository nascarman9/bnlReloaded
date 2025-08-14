using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ToolGroundSlam : Tool
{
    public override ToolType Type => ToolType.GroundSlam;

    public float MinHeight { get; set; }

    public float ForceDown { get; set; }

    public InstEffect? HitEffect { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Ammo != null, true, true, true, HitEffect != null).Write(writer);
      if (Ammo != null)
        ToolAmmo.WriteRecord(writer, Ammo);
      writer.Write(AutoSwitch);
      writer.Write(MinHeight);
      writer.Write(ForceDown);
      if (HitEffect != null)
        InstEffect.WriteVariant(writer, HitEffect);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Ammo = bitField[0] ? ToolAmmo.ReadRecord(reader) : null;
      if (bitField[1])
        AutoSwitch = reader.ReadBoolean();
      if (bitField[2])
        MinHeight = reader.ReadSingle();
      if (bitField[3])
        ForceDown = reader.ReadSingle();
      HitEffect = bitField[4] ? InstEffect.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ToolGroundSlam value)
    {
      value.Write(writer);
    }

    public static ToolGroundSlam ReadRecord(BinaryReader reader)
    {
      var toolGroundSlam = new ToolGroundSlam();
      toolGroundSlam.Read(reader);
      return toolGroundSlam;
    }
}