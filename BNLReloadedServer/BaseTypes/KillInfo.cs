using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class KillInfo
{
    public uint DeadUnitId { get; set; }

    public uint? Dead { get; set; }

    public uint? Killer { get; set; }

    public List<uint>? Assistants { get; set; }

    public Key DamageSource { get; set; }

    public Vector3? SourcePosition { get; set; }

    public bool Crit { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, Dead.HasValue, Killer.HasValue, Assistants != null, true, SourcePosition.HasValue, true).Write(writer);
      writer.Write(DeadUnitId);
      if (Dead.HasValue)
        writer.Write(Dead.Value);
      if (Killer.HasValue)
        writer.Write(Killer.Value);
      if (Assistants != null)
        writer.WriteList(Assistants, writer.Write);
      Key.WriteRecord(writer, DamageSource);
      if (SourcePosition.HasValue)
        writer.Write(SourcePosition.Value);
      writer.Write(Crit);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      if (bitField[0])
        DeadUnitId = reader.ReadUInt32();
      Dead = bitField[1] ? reader.ReadUInt32() : null;
      Killer = bitField[2] ? reader.ReadUInt32() : null;
      Assistants = bitField[3] ? reader.ReadList<uint, List<uint>>(reader.ReadUInt32) : null;
      if (bitField[4])
        DamageSource = Key.ReadRecord(reader);
      SourcePosition = bitField[5] ? reader.ReadVector3() : null;
      if (!bitField[6])
        return;
      Crit = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, KillInfo value) => value.Write(writer);

    public static KillInfo ReadRecord(BinaryReader reader)
    {
      var killInfo = new KillInfo();
      killInfo.Read(reader);
      return killInfo;
    }
}