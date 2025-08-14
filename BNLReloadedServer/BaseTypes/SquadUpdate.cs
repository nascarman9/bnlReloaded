using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SquadUpdate
{
    public List<SquadPlayerUpdate>? Players { get; set; }

    public Key? GameMode { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(Players != null, GameMode.HasValue).Write(writer);
        if (Players != null)
            writer.WriteList(Players, SquadPlayerUpdate.WriteRecord);
        if (!GameMode.HasValue)
            return;
        Key.WriteRecord(writer, GameMode.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        Players = bitField[0] ? reader.ReadList<SquadPlayerUpdate, List<SquadPlayerUpdate>>(SquadPlayerUpdate.ReadRecord) : null;
        GameMode = bitField[1] ? Key.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, SquadUpdate value) => value.Write(writer);

    public static SquadUpdate ReadRecord(BinaryReader reader)
    {
        var squadUpdate = new SquadUpdate();
        squadUpdate.Read(reader);
        return squadUpdate;
    }
}