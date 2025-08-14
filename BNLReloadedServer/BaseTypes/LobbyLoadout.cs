using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LobbyLoadout
{
    public Key HeroKey { get; set; }

    public Dictionary<int, Key>? Devices { get; set; }

    public List<Key>? Perks { get; set; }

    public Key SkinKey { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, Devices != null, Perks != null, true).Write(writer);
        Key.WriteRecord(writer, HeroKey);
        if (Devices != null)
            writer.WriteMap(Devices, writer.Write, Key.WriteRecord);
        if (Perks != null)
            writer.WriteList(Perks, Key.WriteRecord);
        Key.WriteRecord(writer, SkinKey);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(4);
        bitField.Read(reader);
        if (bitField[0])
            HeroKey = Key.ReadRecord(reader);
        Devices = bitField[1] ? reader.ReadMap<int, Key, Dictionary<int, Key>>(reader.ReadInt32, Key.ReadRecord) : null;
        Perks = bitField[2] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
        if (!bitField[3])
            return;
        SkinKey = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, LobbyLoadout value) => value.Write(writer);

    public static LobbyLoadout ReadRecord(BinaryReader reader)
    {
        var lobbyLoadout = new LobbyLoadout();
        lobbyLoadout.Read(reader);
        return lobbyLoadout;
    }
}