using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LobbyMapData
{
    public MapInfo? Info { get; set; }

    public List<uint>? PlayerVotes { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(Info != null, PlayerVotes != null).Write(writer);
        if (Info != null)
            MapInfo.WriteVariant(writer, Info);
        if (PlayerVotes != null)
            writer.WriteList(PlayerVotes, writer.Write);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        Info = bitField[0] ? MapInfo.ReadVariant(reader) : null;
        PlayerVotes = bitField[1] ? reader.ReadList<uint, List<uint>>(reader.ReadUInt32) : null;
    }

    public static void WriteRecord(BinaryWriter writer, LobbyMapData value) => value.Write(writer);

    public static LobbyMapData ReadRecord(BinaryReader reader)
    {
        var lobbyMapData = new LobbyMapData();
        lobbyMapData.Read(reader);
        return lobbyMapData;
    }
}