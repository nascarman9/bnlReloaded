using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ProjectileInfo
{
    public Key ProjectileKey { get; set; }

    public ZoneTransform? Transform { get; set; }

    public float Speed { get; set; }

    public uint OwnerUnitId { get; set; }

    public TeamType OwnerTeam { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, Transform != null, true, true, true).Write(writer);
        Key.WriteRecord(writer, ProjectileKey);
        if (Transform != null)
            ZoneTransform.WriteRecord(writer, Transform);
        writer.Write(Speed);
        writer.Write(OwnerUnitId);
        writer.WriteByteEnum(OwnerTeam);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(5);
        bitField.Read(reader);
        if (bitField[0])
            ProjectileKey = Key.ReadRecord(reader);
        Transform = bitField[1] ? ZoneTransform.ReadRecord(reader) : null;
        if (bitField[2])
            Speed = reader.ReadSingle();
        if (bitField[3])
            OwnerUnitId = reader.ReadUInt32();
        if (!bitField[4])
            return;
        OwnerTeam = reader.ReadByteEnum<TeamType>();
    }

    public static void WriteRecord(BinaryWriter writer, ProjectileInfo value)
    {
        value.Write(writer);
    }

    public static ProjectileInfo ReadRecord(BinaryReader reader)
    {
        var projectileInfo = new ProjectileInfo();
        projectileInfo.Read(reader);
        return projectileInfo;
    }
}