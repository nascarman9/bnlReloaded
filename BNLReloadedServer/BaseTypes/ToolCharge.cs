using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ToolCharge : Tool
{
    public override ToolType Type => ToolType.Charge;

    public bool CanHoldCharge { get; set; }

    public float MinChargeTime { get; set; }

    public float MaxChargeTime { get; set; }

    public ToolChargeOptions? MinOptions { get; set; }

    public ToolChargeOptions? MaxOptions { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Ammo != null, true, true, true, true, MinOptions != null, MaxOptions != null).Write(writer);
      if (Ammo != null)
        ToolAmmo.WriteRecord(writer, Ammo);
      writer.Write(AutoSwitch);
      writer.Write(CanHoldCharge);
      writer.Write(MinChargeTime);
      writer.Write(MaxChargeTime);
      if (MinOptions != null)
        ToolChargeOptions.WriteRecord(writer, MinOptions);
      if (MaxOptions != null)
        ToolChargeOptions.WriteRecord(writer, MaxOptions);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      Ammo = bitField[0] ? ToolAmmo.ReadRecord(reader) : null;
      if (bitField[1])
        AutoSwitch = reader.ReadBoolean();
      if (bitField[2])
        CanHoldCharge = reader.ReadBoolean();
      if (bitField[3])
        MinChargeTime = reader.ReadSingle();
      if (bitField[4])
        MaxChargeTime = reader.ReadSingle();
      MinOptions = bitField[5] ? ToolChargeOptions.ReadRecord(reader) : null;
      MaxOptions = bitField[6] ? ToolChargeOptions.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ToolCharge value) => value.Write(writer);

    public static ToolCharge ReadRecord(BinaryReader reader)
    {
      var toolCharge = new ToolCharge();
      toolCharge.Read(reader);
      return toolCharge;
    }
}