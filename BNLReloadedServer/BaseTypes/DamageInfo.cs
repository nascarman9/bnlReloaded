using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class DamageInfo
{
    public uint TargetUnitId { get; set; }

    public uint? SourceUnitId { get; set; }

    public Vector3? SourcePosition { get; set; }

    public Key? Impact { get; set; }

    public float Damage { get; set; }

    public float InitialDamage { get; set; }

    public bool Crit { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, SourceUnitId.HasValue, SourcePosition.HasValue, Impact.HasValue, true, true, true).Write(writer);
      writer.Write(TargetUnitId);
      if (SourceUnitId.HasValue)
        writer.Write(SourceUnitId.Value);
      if (SourcePosition.HasValue)
        writer.Write(SourcePosition.Value);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      writer.Write(Damage);
      writer.Write(InitialDamage);
      writer.Write(Crit);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      if (bitField[0])
        TargetUnitId = reader.ReadUInt32();
      SourceUnitId = bitField[1] ? reader.ReadUInt32() : null;
      SourcePosition = bitField[2] ? reader.ReadVector3() : null;
      Impact = bitField[3] ? Key.ReadRecord(reader) : null;
      if (bitField[4])
        Damage = reader.ReadSingle();
      if (bitField[5])
        InitialDamage = reader.ReadSingle();
      if (!bitField[6])
        return;
      Crit = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, DamageInfo value) => value.Write(writer);

    public static DamageInfo ReadRecord(BinaryReader reader)
    {
      var damageInfo = new DamageInfo();
      damageInfo.Read(reader);
      return damageInfo;
    }
}