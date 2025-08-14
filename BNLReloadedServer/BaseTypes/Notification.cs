using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public abstract class Notification
{
    public abstract NotificationType Type { get; }

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, Notification value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static Notification ReadVariant(BinaryReader reader)
    {
        var notification = Create(reader.ReadByteEnum<NotificationType>());
        notification.Read(reader);
        return notification;
    }

    public static Notification Create(NotificationType type)
    {
        return type switch
        {
            NotificationType.Common => new NotificationCommon(),
            NotificationType.Demerit => new NotificationDemerit(),
            NotificationType.Graveyard => new NotificationGraveyard(),
            NotificationType.LeagueChanged => new NotificationLeagueChanged(),
            NotificationType.Currency => new NotificationCurrency(),
            NotificationType.Reward => new NotificationReward(),
            NotificationType.SteamDlc => new NotificationSteamDlc(),
            NotificationType.Zeus => new NotificationZeus(),
            NotificationType.LeagueJoined => new NotificationLeagueJoined(),
            NotificationType.DailyChallenge => new NotificationDailyChallenge(),
            NotificationType.GameModeClosed => throw new ArgumentOutOfRangeException(nameof(type), type, "GameModeClosed Notification not supported"),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}