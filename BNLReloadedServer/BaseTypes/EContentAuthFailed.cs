using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class EContentAuthFailed : Exception
{
    public void Write(BinaryWriter writer) => new BitField().Write(writer);

    public void Read(BinaryReader reader) => new BitField(0).Read(reader);

    public static void WriteRecord(BinaryWriter writer, EContentAuthFailed value)
    {
        value.Write(writer);
    }

    public static EContentAuthFailed ReadRecord(BinaryReader reader)
    {
        var econtentAuthFailed = new EContentAuthFailed();
        econtentAuthFailed.Read(reader);
        return econtentAuthFailed;
    }
}