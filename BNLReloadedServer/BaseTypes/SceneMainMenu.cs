using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SceneMainMenu : Scene
{
    public override SceneType Type => SceneType.MainMenu;

    public override void Write(BinaryWriter writer) => new BitField().Write(writer);

    public override void Read(BinaryReader reader) => new BitField(0).Read(reader);

    public static void WriteRecord(BinaryWriter writer, SceneMainMenu value) => value.Write(writer);

    public static SceneMainMenu ReadRecord(BinaryReader reader)
    {
        var sceneMainMenu = new SceneMainMenu();
        sceneMainMenu.Read(reader);
        return sceneMainMenu;
    }
}