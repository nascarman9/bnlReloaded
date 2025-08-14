using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ToolBulletHitscan : ToolBullet
{
    public override ToolBulletType Type => ToolBulletType.Hitscan;

    public override void Write(BinaryWriter writer) => new BitField().Write(writer);

    public override void Read(BinaryReader reader) => new BitField(0).Read(reader);

    public static void WriteRecord(BinaryWriter writer, ToolBulletHitscan value)
    {
        value.Write(writer);
    }

    public static ToolBulletHitscan ReadRecord(BinaryReader reader)
    {
        var toolBulletHitscan = new ToolBulletHitscan();
        toolBulletHitscan.Read(reader);
        return toolBulletHitscan;
    }
}