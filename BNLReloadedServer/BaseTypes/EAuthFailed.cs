using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class EAuthFailed : Exception
{
    public void Write(BinaryWriter writer) => new BitField().Write(writer);

    public void Read(BinaryReader reader) => new BitField(0).Read(reader);

    public static void WriteRecord(BinaryWriter writer, EAuthFailed value) => value.Write(writer);

    public static EAuthFailed ReadRecord(BinaryReader reader)
    {
        var eauthFailed = new EAuthFailed();
        eauthFailed.Read(reader);
        return eauthFailed;
    }
}