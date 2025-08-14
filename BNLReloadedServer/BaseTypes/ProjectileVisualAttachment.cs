using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ProjectileVisualAttachment
{
    public string? Name { get; set; }

    public Vector3 Offset { get; set; }

    public float ChaseDistance { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Name != null, true, true).Write(writer);
      if (Name != null)
        writer.Write(Name);
      writer.Write(Offset);
      writer.Write(ChaseDistance);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Name = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Offset = reader.ReadVector3();
      if (!bitField[2])
        return;
      ChaseDistance = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, ProjectileVisualAttachment value)
    {
      value.Write(writer);
    }

    public static ProjectileVisualAttachment ReadRecord(BinaryReader reader)
    {
      var visualAttachment = new ProjectileVisualAttachment();
      visualAttachment.Read(reader);
      return visualAttachment;
    }
}