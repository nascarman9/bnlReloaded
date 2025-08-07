using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Servers;

namespace BNLReloadedServer.Service;

public class ServicePlayer(ISender sender, IServiceScene serviceScene, IServiceTime serviceTime) : IServicePlayer
{
    private enum ServicePlayerId : byte
    {
        MessageUpdateSteamInfo = 0,
        MessagePlayerUpdate = 1,
        MessageClientRevision = 2,
        MessageServerRevision = 3,
        MessageSteamCurrency = 44
    }
    
    private static BinaryWriter CreateWriter()
    {
        var memStream =  new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServicePlayer);
        return writer;
    }

    private void ReceiveUpdateSteamInfo(BinaryReader reader)
    {
        var playerSteamInfo = PlayerSteamInfo.ReadRecord(reader);
        serviceTime.SendSetOrigin(DateTime.Now.ToBinary());
        serviceScene.SendServerUpdate(new ServerUpdate
        {
            BuyPlatinumEnabled = false,
            FriendlyEnabled = true,
            MadModeEnabled = true,
            MapEditorEnabled = true,
            PlayButtonEnabled = true,
            RankedEnabled = true,
            TutorialEnabled = true,
            ShopEnabled = true,
            TimeAssaultEnabled = true
        });
        serviceScene.SendChangeScene(Scene.Create(SceneType.MainMenu));
    }

    private void ReceiveClientRevision(BinaryReader reader)
    {
        var clientRevision = reader.ReadString();
        SendServerRevision("952");
    }
    
    public void SendServerRevision(string revision)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageServerRevision);
        writer.Write(revision);
        sender.SendToSession(writer);
    }

    public void SendSteamCurrency(ushort rpcId, string currency)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageSteamCurrency);
        writer.Write(rpcId);
        writer.Write((byte) 0);
        writer.Write(currency);
        sender.SendToSession(writer);
    }

    private void ReceiveSteamCurrency(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        SendSteamCurrency(rpcId, "USD");
    }

    public void Receive(BinaryReader reader)
    {
        var servicePlayerId = reader.ReadByte();
        Console.WriteLine($"ServicePlayerId: {servicePlayerId}");
        switch (servicePlayerId)
        {
            case (byte)ServicePlayerId.MessageUpdateSteamInfo:
                ReceiveUpdateSteamInfo(reader);
                break;
            case (byte)ServicePlayerId.MessagePlayerUpdate:
                break;
            case (byte)ServicePlayerId.MessageClientRevision:
                ReceiveClientRevision(reader);
                break;
            case (byte)ServicePlayerId.MessageSteamCurrency:
                ReceiveSteamCurrency(reader);
                break;
            default:
                Console.WriteLine($"Player service received unsupported serviceId: {servicePlayerId}");
                break;
        }
    }
}