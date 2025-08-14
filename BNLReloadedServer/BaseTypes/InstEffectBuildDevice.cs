using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectBuildDevice : InstEffect
{
  public override InstEffectType Type => InstEffectType.BuildDevice;

    public Key DeviceKey { get; set; }

    public float TotalCost { get; set; }

    public int Level { get; set; } = 1;

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true, true, true).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      Key.WriteRecord(writer, DeviceKey);
      writer.Write(TotalCost);
      writer.Write(Level);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (bitField[3])
        DeviceKey = Key.ReadRecord(reader);
      if (bitField[4])
        TotalCost = reader.ReadSingle();
      if (!bitField[5])
        return;
      Level = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectBuildDevice value)
    {
      value.Write(writer);
    }

    public static InstEffectBuildDevice ReadRecord(BinaryReader reader)
    {
      var effectBuildDevice = new InstEffectBuildDevice();
      effectBuildDevice.Read(reader);
      return effectBuildDevice;
    }
}