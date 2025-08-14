using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class NotificationLeagueJoined : Notification
{
    public override NotificationType Type => NotificationType.LeagueJoined;

    public override void Write(BinaryWriter writer) => new BitField().Write(writer);

    public override void Read(BinaryReader reader) => new BitField(0).Read(reader);

    public static void WriteRecord(BinaryWriter writer, NotificationLeagueJoined value)
    {
        value.Write(writer);
    }

    public static NotificationLeagueJoined ReadRecord(BinaryReader reader)
    {
        var notificationLeagueJoined = new NotificationLeagueJoined();
        notificationLeagueJoined.Read(reader);
        return notificationLeagueJoined;
    }
}