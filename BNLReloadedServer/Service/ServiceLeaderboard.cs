using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;

namespace BNLReloadedServer.Service;

public class ServiceLeaderboard(ISender sender) : IServiceLeaderboard
{
    private enum ServiceLeaderboardId : byte
    {
        MessageGetTimeTrialLeaderboard = 0,
        MessageGetLeagueLeaderboard = 1
    }
    
    private static BinaryWriter CreateWriter()
    {
        var memStream = new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServiceLeaderboard);
        return writer;
    }

    public void SendGetTimeTrialLeaderboard(ushort rpcId, Dictionary<Key, List<TtLeaderboardRecord>>? data,
        ELeaderboardUpdateCooldown? eLeaderboardUpdateCooldown = null, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLeaderboardId.MessageGetTimeTrialLeaderboard);
        writer.Write(rpcId);
        if (data != null)
        {
            writer.Write((byte) 0);
            writer.WriteMap(data, Key.WriteRecord, item => writer.WriteList(item, TtLeaderboardRecord.WriteRecord));
        }
        else if (eLeaderboardUpdateCooldown != null)
        {
            writer.Write((byte)1);
            ELeaderboardUpdateCooldown.WriteRecord(writer, eLeaderboardUpdateCooldown);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.Send(writer);
    }

    private void ReceiveGetTimeTrialLeaderboard(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
    }

    public void SendGetLeagueLeaderboard(ushort rpcId, List<LeagueLeaderboardRecord>? data, ELeaderboardUpdateCooldown? eLeaderboardUpdateCooldown = null,
        string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLeaderboardId.MessageGetLeagueLeaderboard);
        writer.Write(rpcId);
        if (data != null)
        {
            writer.Write((byte) 0);
            writer.WriteList(data, LeagueLeaderboardRecord.WriteRecord);
        }
        else if (eLeaderboardUpdateCooldown != null)
        {
            writer.Write((byte)1);
            ELeaderboardUpdateCooldown.WriteRecord(writer, eLeaderboardUpdateCooldown);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.Send(writer);
    }

    private void ReceiveGetLeagueLeaderboard(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
    }
    
    public void Receive(BinaryReader reader)
    {
        var serviceLeaderboardId = reader.ReadByte();
        ServiceLeaderboardId? leaderboardEnum = null;
        if (Enum.IsDefined(typeof(ServiceLeaderboardId), serviceLeaderboardId))
        {
            leaderboardEnum = (ServiceLeaderboardId)serviceLeaderboardId;
        }
        Console.WriteLine($"ServiceLeaderboardId: {leaderboardEnum.ToString()}");
        switch (leaderboardEnum)
        {
            case ServiceLeaderboardId.MessageGetTimeTrialLeaderboard:
                ReceiveGetTimeTrialLeaderboard(reader);
                break;
            case ServiceLeaderboardId.MessageGetLeagueLeaderboard:
                ReceiveGetLeagueLeaderboard(reader);
                break;
            default:
                Console.WriteLine($"Unknown service leaderboard id {serviceLeaderboardId}");
                break;
        }
    }
}