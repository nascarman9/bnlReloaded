using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class HeroStats
{
    public Key Hero { get; set; }

    public int Wins { get; set; }

    public int TotalMatches { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true).Write(writer);
        Key.WriteRecord(writer, Hero);
        writer.Write(Wins);
        writer.Write(TotalMatches);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            Hero = Key.ReadRecord(reader);
        if (bitField[1])
            Wins = reader.ReadInt32();
        if (!bitField[2])
            return;
        TotalMatches = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, HeroStats value) => value.Write(writer);

    public static HeroStats ReadRecord(BinaryReader reader)
    {
        var heroStats = new HeroStats();
        heroStats.Read(reader);
        return heroStats;
    }
}