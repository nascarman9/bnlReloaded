using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class GlobalStats
{
    public uint ResourcesEarned { get; set; }

    public uint BlocksBuilt { get; set; }

    public uint ObjectiveDamage { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true).Write(writer);
        writer.Write(ResourcesEarned);
        writer.Write(BlocksBuilt);
        writer.Write(ObjectiveDamage);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            ResourcesEarned = reader.ReadUInt32();
        if (bitField[1])
            BlocksBuilt = reader.ReadUInt32();
        if (!bitField[2])
            return;
        ObjectiveDamage = reader.ReadUInt32();
    }

    public static void WriteRecord(BinaryWriter writer, GlobalStats value) => value.Write(writer);

    public static GlobalStats ReadRecord(BinaryReader reader)
    {
        var globalStats = new GlobalStats();
        globalStats.Read(reader);
        return globalStats;
    }
}