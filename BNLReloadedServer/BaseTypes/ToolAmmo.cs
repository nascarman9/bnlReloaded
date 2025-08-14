using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ToolAmmo
{
    public float Rate { get; set; }

    public int AmmoIndex { get; set; }

    public bool AutoReloadOnEmptyGunSwitch { get; set; }

    public bool AutoReloadOnEmptyGunFire { get; set; }

    public bool AutoReloadAfterLastShot { get; set; }

    public int LowAmmoBulletsCount { get; set; }

    public int ReloadMessageBulletsCount { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, true, true).Write(writer);
      writer.Write(Rate);
      writer.Write(AmmoIndex);
      writer.Write(AutoReloadOnEmptyGunSwitch);
      writer.Write(AutoReloadOnEmptyGunFire);
      writer.Write(AutoReloadAfterLastShot);
      writer.Write(LowAmmoBulletsCount);
      writer.Write(ReloadMessageBulletsCount);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      if (bitField[0])
        Rate = reader.ReadSingle();
      if (bitField[1])
        AmmoIndex = reader.ReadInt32();
      if (bitField[2])
        AutoReloadOnEmptyGunSwitch = reader.ReadBoolean();
      if (bitField[3])
        AutoReloadOnEmptyGunFire = reader.ReadBoolean();
      if (bitField[4])
        AutoReloadAfterLastShot = reader.ReadBoolean();
      if (bitField[5])
        LowAmmoBulletsCount = reader.ReadInt32();
      if (!bitField[6])
        return;
      ReloadMessageBulletsCount = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, ToolAmmo value) => value.Write(writer);

    public static ToolAmmo ReadRecord(BinaryReader reader)
    {
      var toolAmmo = new ToolAmmo();
      toolAmmo.Read(reader);
      return toolAmmo;
    }
}