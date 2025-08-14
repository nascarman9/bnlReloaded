using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SpawnPoint
{
    public uint Id { get; set; }

    public TeamType Team { get; set; }

    public Vector3 Pos { get; set; }

    public SpawnPointLockType Lock { get; set; }

    public uint? Owner { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, true, Owner.HasValue).Write(writer);
        writer.Write(Id);
        writer.WriteByteEnum(Team);
        writer.Write(Pos);
        writer.WriteByteEnum(Lock);
        if (!Owner.HasValue)
            return;
        writer.Write(Owner.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(5);
        bitField.Read(reader);
        if (bitField[0])
            Id = reader.ReadUInt32();
        if (bitField[1])
            Team = reader.ReadByteEnum<TeamType>();
        if (bitField[2])
            Pos = reader.ReadVector3();
        if (bitField[3])
            Lock = reader.ReadByteEnum<SpawnPointLockType>();
        Owner = bitField[4] ? reader.ReadUInt32() : null;
    }

    public static void WriteRecord(BinaryWriter writer, SpawnPoint value) => value.Write(writer);

    public static SpawnPoint ReadRecord(BinaryReader reader)
    {
        var spawnPoint = new SpawnPoint();
        spawnPoint.Read(reader);
        return spawnPoint;
    }
}