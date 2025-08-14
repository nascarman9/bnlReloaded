using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SquadFinderPlayerData
{
    public uint PlayerId { get; set; }

    public ulong SteamId { get; set; }

    public string? Nickname { get; set; }

    public SquadFinderSettings? Settings { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, Nickname != null, Settings != null).Write(writer);
        writer.Write(PlayerId);
        writer.Write(SteamId);
        if (Nickname != null)
            writer.Write(Nickname);
        if (Settings != null)
            SquadFinderSettings.WriteRecord(writer, Settings);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(4);
        bitField.Read(reader);
        if (bitField[0])
            PlayerId = reader.ReadUInt32();
        if (bitField[1])
            SteamId = reader.ReadUInt64();
        Nickname = bitField[2] ? reader.ReadString() : null;
        Settings = bitField[3] ? SquadFinderSettings.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, SquadFinderPlayerData value)
    {
        value.Write(writer);
    }

    public static SquadFinderPlayerData ReadRecord(BinaryReader reader)
    {
        var finderPlayerData = new SquadFinderPlayerData();
        finderPlayerData.Read(reader);
        return finderPlayerData;
    }
}