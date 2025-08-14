using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MasterServerInfo
{
    public bool ServerMaintenance { get; set; }

    public bool SteamEncryptedLogin { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        writer.Write(ServerMaintenance);
        writer.Write(SteamEncryptedLogin);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            ServerMaintenance = reader.ReadBoolean();
        if (!bitField[1])
            return;
        SteamEncryptedLogin = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, MasterServerInfo value)
    {
        value.Write(writer);
    }

    public static MasterServerInfo ReadRecord(BinaryReader reader)
    {
        var masterServerInfo = new MasterServerInfo();
        masterServerInfo.Read(reader);
        return masterServerInfo;
    }
}