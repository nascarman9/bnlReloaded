using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ProjectileCollisionMask
{
    public bool Player { get; set; }

    public bool Walls { get; set; }

    public bool Ground { get; set; }

    public bool Other { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true).Write(writer);
      writer.Write(Player);
      writer.Write(Walls);
      writer.Write(Ground);
      writer.Write(Other);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      if (bitField[0])
        Player = reader.ReadBoolean();
      if (bitField[1])
        Walls = reader.ReadBoolean();
      if (bitField[2])
        Ground = reader.ReadBoolean();
      if (!bitField[3])
        return;
      Other = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, ProjectileCollisionMask value)
    {
      value.Write(writer);
    }

    public static ProjectileCollisionMask ReadRecord(BinaryReader reader)
    {
      var projectileCollisionMask = new ProjectileCollisionMask();
      projectileCollisionMask.Read(reader);
      return projectileCollisionMask;
    }
}