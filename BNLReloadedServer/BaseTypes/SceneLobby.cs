using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SceneLobby : Scene
{
    public override SceneType Type => SceneType.Lobby;

    public TeamType MyTeam { get; set; }

    public Key GameMode { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        writer.WriteByteEnum(MyTeam);
        Key.WriteRecord(writer, GameMode);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            MyTeam = reader.ReadByteEnum<TeamType>();
        if (!bitField[1])
            return;
        GameMode = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, SceneLobby value) => value.Write(writer);

    public static SceneLobby ReadRecord(BinaryReader reader)
    {
        var sceneLobby = new SceneLobby();
        sceneLobby.Read(reader);
        return sceneLobby;
    }
}