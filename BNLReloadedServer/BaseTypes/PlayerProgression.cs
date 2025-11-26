using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class PlayerProgression
{
    public XpInfo? PlayerProgress { get; set; }

    public Dictionary<Key, XpInfo>? HeroesProgress { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(PlayerProgress != null, HeroesProgress != null).Write(writer);
        if (PlayerProgress != null)
            XpInfo.WriteRecord(writer, PlayerProgress);
        if (HeroesProgress != null)
            writer.WriteMap(HeroesProgress, Key.WriteRecord, XpInfo.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        PlayerProgress = bitField[0] ? XpInfo.ReadRecord(reader) : null;
        HeroesProgress = bitField[1] ? reader.ReadMap<Key, XpInfo, Dictionary<Key, XpInfo>>(Key.ReadRecord, XpInfo.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, PlayerProgression value)
    {
        value.Write(writer);
    }

    public static PlayerProgression ReadRecord(BinaryReader reader)
    {
        var playerProgression = new PlayerProgression();
        playerProgression.Read(reader);
        return playerProgression;
    }

    public static byte[] WriteByteRecord(PlayerProgression playerProgression)
    {
        var memStream = new MemoryStream();
        using var writer = new BinaryWriter(memStream);
        WriteRecord(writer, playerProgression);
        return memStream.ToArray();
    }

    public static PlayerProgression ReadByteRecord(byte[] bytes)
    {
        var memStream = new MemoryStream(bytes);
        using var reader = new BinaryReader(memStream);
        return ReadRecord(reader);
    }
}