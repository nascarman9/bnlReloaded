using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public abstract class Scene
{
    public abstract SceneType Type { get; }

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, Scene value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static Scene ReadVariant(BinaryReader reader)
    {
        var scene = Create(reader.ReadByteEnum<SceneType>());
        scene.Read(reader);
        return scene;
    }

    public static Scene Create(SceneType type)
    {
        return type switch
        {
            SceneType.MainMenu => new SceneMainMenu(),
            //SceneType.Lobby => new SceneLobby(),
            //SceneType.Zone => new SceneZone(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}