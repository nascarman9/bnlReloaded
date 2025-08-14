using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ImpactData
{
    public Vector3 InsidePoint { get; set; }

    public Vector3s Normal { get; set; }

    public uint? CasterUnitId { get; set; }

    public uint? CasterPlayerId { get; set; }

    public Key? Impact { get; set; }

    public Key? SourceKey { get; set; }

    public List<uint>? HitUnits { get; set; }

    public Vector3 ShotPos { get; set; }

    public bool Crit { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, CasterUnitId.HasValue, CasterPlayerId.HasValue, Impact.HasValue, SourceKey.HasValue,
        HitUnits != null, true, true).Write(writer);
      writer.Write(InsidePoint);
      writer.Write(Normal);
      if (CasterUnitId.HasValue)
        writer.Write(CasterUnitId.Value);
      if (CasterPlayerId.HasValue)
        writer.Write(CasterPlayerId.Value);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      if (SourceKey.HasValue)
        Key.WriteRecord(writer, SourceKey.Value);
      if (HitUnits != null)
        writer.WriteList(HitUnits, writer.Write);
      writer.Write(ShotPos);
      writer.Write(Crit);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(9);
      bitField.Read(reader);
      if (bitField[0])
        InsidePoint = reader.ReadVector3();
      if (bitField[1])
        Normal = reader.ReadVector3s();
      CasterUnitId = bitField[2] ? reader.ReadUInt32() : null;
      CasterPlayerId = bitField[3] ? reader.ReadUInt32() : null;
      Impact = bitField[4] ? Key.ReadRecord(reader) : null;
      SourceKey = bitField[5] ? Key.ReadRecord(reader) : null;
      HitUnits = bitField[6] ? reader.ReadList<uint, List<uint>>(reader.ReadUInt32) : null;
      if (bitField[7])
        ShotPos = reader.ReadVector3();
      if (!bitField[8])
        return;
      Crit = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, ImpactData value) => value.Write(writer);

    public static ImpactData ReadRecord(BinaryReader reader)
    {
      var impactData = new ImpactData();
      impactData.Read(reader);
      return impactData;
    }
}