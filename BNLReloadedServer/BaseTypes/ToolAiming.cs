using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ToolAiming : Tool
{
    public override ToolType Type => ToolType.Aiming;

    public ConeOfFire? ConeOfFire { get; set; }

    public float CameraFov { get; set; }

    public float MoveSpeedMod { get; set; } = 1f;

    public bool Scope { get; set; }

    public float? Sway { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Ammo != null, true, ConeOfFire != null, true, true, true, Sway.HasValue).Write(writer);
      if (Ammo != null)
        ToolAmmo.WriteRecord(writer, Ammo);
      writer.Write(AutoSwitch);
      if (ConeOfFire != null)
        ConeOfFire.WriteRecord(writer, ConeOfFire);
      writer.Write(CameraFov);
      writer.Write(MoveSpeedMod);
      writer.Write(Scope);
      if (!Sway.HasValue)
        return;
      writer.Write(Sway.Value);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      Ammo = bitField[0] ? ToolAmmo.ReadRecord(reader) : null;
      if (bitField[1])
        AutoSwitch = reader.ReadBoolean();
      ConeOfFire = bitField[2] ? ConeOfFire.ReadRecord(reader) : null;
      if (bitField[3])
        CameraFov = reader.ReadSingle();
      if (bitField[4])
        MoveSpeedMod = reader.ReadSingle();
      if (bitField[5])
        Scope = reader.ReadBoolean();
      Sway = bitField[6] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, ToolAiming value) => value.Write(writer);

    public static ToolAiming ReadRecord(BinaryReader reader)
    {
      var toolAiming = new ToolAiming();
      toolAiming.Read(reader);
      return toolAiming;
    }
}