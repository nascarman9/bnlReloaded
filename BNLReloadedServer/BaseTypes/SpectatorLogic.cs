using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SpectatorLogic
{
    public float FreeSensitivity { get; set; }

    public float FreeMovementSpeed { get; set; }

    public float FreeMovementDamp { get; set; }

    public float FreeRotationDamp { get; set; }

    public float OrbitRotationSpeed { get; set; }

    public float OrbitRotationDamp { get; set; }

    public float OrbitDistanceToUnitMin { get; set; }

    public float OrbitDistanceToUnitMax { get; set; }

    public float OrbitDistanceToUnitStep { get; set; }

    public Vector3 OrbitOffsetOnUnit { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, true, true, true, true, true).Write(writer);
      writer.Write(FreeSensitivity);
      writer.Write(FreeMovementSpeed);
      writer.Write(FreeMovementDamp);
      writer.Write(FreeRotationDamp);
      writer.Write(OrbitRotationSpeed);
      writer.Write(OrbitRotationDamp);
      writer.Write(OrbitDistanceToUnitMin);
      writer.Write(OrbitDistanceToUnitMax);
      writer.Write(OrbitDistanceToUnitStep);
      writer.Write(OrbitOffsetOnUnit);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(10);
      bitField.Read(reader);
      if (bitField[0])
        FreeSensitivity = reader.ReadSingle();
      if (bitField[1])
        FreeMovementSpeed = reader.ReadSingle();
      if (bitField[2])
        FreeMovementDamp = reader.ReadSingle();
      if (bitField[3])
        FreeRotationDamp = reader.ReadSingle();
      if (bitField[4])
        OrbitRotationSpeed = reader.ReadSingle();
      if (bitField[5])
        OrbitRotationDamp = reader.ReadSingle();
      if (bitField[6])
        OrbitDistanceToUnitMin = reader.ReadSingle();
      if (bitField[7])
        OrbitDistanceToUnitMax = reader.ReadSingle();
      if (bitField[8])
        OrbitDistanceToUnitStep = reader.ReadSingle();
      if (!bitField[9])
        return;
      OrbitOffsetOnUnit = reader.ReadVector3();
    }

    public static void WriteRecord(BinaryWriter writer, SpectatorLogic value)
    {
      value.Write(writer);
    }

    public static SpectatorLogic ReadRecord(BinaryReader reader)
    {
      var spectatorLogic = new SpectatorLogic();
      spectatorLogic.Read(reader);
      return spectatorLogic;
    }
}