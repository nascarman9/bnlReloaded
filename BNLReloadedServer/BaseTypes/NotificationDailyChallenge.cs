using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class NotificationDailyChallenge : Notification
{
    public override NotificationType Type => NotificationType.DailyChallenge;

    public Key ChallengeKey { get; set; }

    public float Value { get; set; }

    public bool BetterThanFriend { get; set; }

    public string? FriendName { get; set; }

    public float RewardPercent { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, FriendName != null, true).Write(writer);
        Key.WriteRecord(writer, ChallengeKey);
        writer.Write(Value);
        writer.Write(BetterThanFriend);
        if (FriendName != null)
            writer.Write(FriendName);
        writer.Write(RewardPercent);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(5);
        bitField.Read(reader);
        if (bitField[0])
            ChallengeKey = Key.ReadRecord(reader);
        if (bitField[1])
            Value = reader.ReadSingle();
        if (bitField[2])
            BetterThanFriend = reader.ReadBoolean();
        FriendName = bitField[3] ? reader.ReadString() : null;
        if (!bitField[4])
            return;
        RewardPercent = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, NotificationDailyChallenge value)
    {
        value.Write(writer);
    }

    public static NotificationDailyChallenge ReadRecord(BinaryReader reader)
    {
        var notificationDailyChallenge = new NotificationDailyChallenge();
        notificationDailyChallenge.Read(reader);
        return notificationDailyChallenge;
    }
}