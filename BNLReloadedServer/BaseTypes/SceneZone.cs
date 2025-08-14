using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SceneZone : Scene
{
    public override SceneType Type => SceneType.Zone;

    public Key GameMode { get; set; }

    public Key MatchKey { get; set; }

    public TeamType MyTeam { get; set; }

    public bool IsSpectator { get; set; }

    public bool IsMapEditor { get; set; }

    public bool Restart { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, true, true, true).Write(writer);
        Key.WriteRecord(writer, GameMode);
        Key.WriteRecord(writer, MatchKey);
        writer.WriteByteEnum(MyTeam);
        writer.Write(IsSpectator);
        writer.Write(IsMapEditor);
        writer.Write(Restart);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(6);
        bitField.Read(reader);
        if (bitField[0])
            GameMode = Key.ReadRecord(reader);
        if (bitField[1])
            MatchKey = Key.ReadRecord(reader);
        if (bitField[2])
            MyTeam = reader.ReadByteEnum<TeamType>();
        if (bitField[3])
            IsSpectator = reader.ReadBoolean();
        if (bitField[4])
            IsMapEditor = reader.ReadBoolean();
        if (!bitField[5])
            return;
        Restart = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, SceneZone value) => value.Write(writer);

    public static SceneZone ReadRecord(BinaryReader reader)
    {
        var sceneZone = new SceneZone();
        sceneZone.Read(reader);
        return sceneZone;
    }
}