using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConeOfFire
{
    public float Angle { get; set; }

    public float MaxBloom { get; set; }

    public float CrouchMod { get; set; }

    public float MoveMod { get; set; }

    public float JumpMod { get; set; }

    public float ResetDelay { get; set; }

    public float ResetSpeed { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, true, true).Write(writer);
      writer.Write(Angle);
      writer.Write(MaxBloom);
      writer.Write(CrouchMod);
      writer.Write(MoveMod);
      writer.Write(JumpMod);
      writer.Write(ResetDelay);
      writer.Write(ResetSpeed);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      if (bitField[0])
        Angle = reader.ReadSingle();
      if (bitField[1])
        MaxBloom = reader.ReadSingle();
      if (bitField[2])
        CrouchMod = reader.ReadSingle();
      if (bitField[3])
        MoveMod = reader.ReadSingle();
      if (bitField[4])
        JumpMod = reader.ReadSingle();
      if (bitField[5])
        ResetDelay = reader.ReadSingle();
      if (!bitField[6])
        return;
      ResetSpeed = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, ConeOfFire value) => value.Write(writer);

    public static ConeOfFire ReadRecord(BinaryReader reader)
    {
      var coneOfFire = new ConeOfFire();
      coneOfFire.Read(reader);
      return coneOfFire;
    }
}