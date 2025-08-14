using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ELeaderboardUpdateCooldown : Exception
{
    public void Write(BinaryWriter writer) => new BitField().Write(writer);

    public void Read(BinaryReader reader) => new BitField(0).Read(reader);

    public static void WriteRecord(BinaryWriter writer, ELeaderboardUpdateCooldown value)
    {
        value.Write(writer);
    }

    public static ELeaderboardUpdateCooldown ReadRecord(BinaryReader reader)
    {
        var eleaderboardUpdateCooldown = new ELeaderboardUpdateCooldown();
        eleaderboardUpdateCooldown.Read(reader);
        return eleaderboardUpdateCooldown;
    }
}