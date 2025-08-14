using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class FriedlyFire
{
    public float DirectDamageReduction { get; set; }

    public float SplashDamageReduction { get; set; }

    public float SelfSplashDamageReduction { get; set; }

    public float DevicesMiningDamageReduction { get; set; }

    public float DevicesDirectDamageReduction { get; set; }

    public float DevicesSplashDamageReduction { get; set; }

    public float ObjectivesDamageReduction { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, true, true).Write(writer);
      writer.Write(DirectDamageReduction);
      writer.Write(SplashDamageReduction);
      writer.Write(SelfSplashDamageReduction);
      writer.Write(DevicesMiningDamageReduction);
      writer.Write(DevicesDirectDamageReduction);
      writer.Write(DevicesSplashDamageReduction);
      writer.Write(ObjectivesDamageReduction);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      if (bitField[0])
        DirectDamageReduction = reader.ReadSingle();
      if (bitField[1])
        SplashDamageReduction = reader.ReadSingle();
      if (bitField[2])
        SelfSplashDamageReduction = reader.ReadSingle();
      if (bitField[3])
        DevicesMiningDamageReduction = reader.ReadSingle();
      if (bitField[4])
        DevicesDirectDamageReduction = reader.ReadSingle();
      if (bitField[5])
        DevicesSplashDamageReduction = reader.ReadSingle();
      if (!bitField[6])
        return;
      ObjectivesDamageReduction = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, FriedlyFire value) => value.Write(writer);

    public static FriedlyFire ReadRecord(BinaryReader reader)
    {
      var friedlyFire = new FriedlyFire();
      friedlyFire.Read(reader);
      return friedlyFire;
    }
}