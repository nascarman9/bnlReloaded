using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ZoneTransform
{
    public Vector3 Position { get; set; }

    public Vector3s Rotation { get; set; }

    public Vector3s LocalVelocity { get; set; }

    public bool IsCrouch { get; set; }

    public bool IsJump { get; set; }

    public bool IsSprint { get; set; }

    public bool IsWallClimb { get; set; }

    public bool IsDash { get; set; }

    public bool IsGroundSlam { get; set; }

    public bool NoInterpolation { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, true, true, true, true, true).Write(writer);
      writer.Write(Position);
      writer.Write(Rotation);
      writer.Write(LocalVelocity);
      writer.Write(IsCrouch);
      writer.Write(IsJump);
      writer.Write(IsSprint);
      writer.Write(IsWallClimb);
      writer.Write(IsDash);
      writer.Write(IsGroundSlam);
      writer.Write(NoInterpolation);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(10);
      bitField.Read(reader);
      if (bitField[0])
        Position = reader.ReadVector3();
      if (bitField[1])
        Rotation = reader.ReadVector3s();
      if (bitField[2])
        LocalVelocity = reader.ReadVector3s();
      if (bitField[3])
        IsCrouch = reader.ReadBoolean();
      if (bitField[4])
        IsJump = reader.ReadBoolean();
      if (bitField[5])
        IsSprint = reader.ReadBoolean();
      if (bitField[6])
        IsWallClimb = reader.ReadBoolean();
      if (bitField[7])
        IsDash = reader.ReadBoolean();
      if (bitField[8])
        IsGroundSlam = reader.ReadBoolean();
      if (!bitField[9])
        return;
      NoInterpolation = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, ZoneTransform value) => value.Write(writer);

    public static ZoneTransform ReadRecord(BinaryReader reader)
    {
      var zoneTransform = new ZoneTransform();
      zoneTransform.Read(reader);
      return zoneTransform;
    }
}