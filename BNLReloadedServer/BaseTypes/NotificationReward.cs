using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class NotificationReward : Notification
{
    public override NotificationType Type => NotificationType.Reward;

    public Key RewardKey { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        Key.WriteRecord(writer, RewardKey);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        RewardKey = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, NotificationReward value)
    {
        value.Write(writer);
    }

    public static NotificationReward ReadRecord(BinaryReader reader)
    {
        var notificationReward = new NotificationReward();
        notificationReward.Read(reader);
        return notificationReward;
    }
}