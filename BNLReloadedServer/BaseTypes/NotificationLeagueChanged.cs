using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class NotificationLeagueChanged : Notification
{
    public override NotificationType Type => NotificationType.LeagueChanged;

    public int TierFrom { get; set; }

    public int DivistionFrom { get; set; }

    public int TierTo { get; set; }

    public int DivistionTo { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, true).Write(writer);
        writer.Write(TierFrom);
        writer.Write(DivistionFrom);
        writer.Write(TierTo);
        writer.Write(DivistionTo);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(4);
        bitField.Read(reader);
        if (bitField[0])
            TierFrom = reader.ReadInt32();
        if (bitField[1])
            DivistionFrom = reader.ReadInt32();
        if (bitField[2])
            TierTo = reader.ReadInt32();
        if (!bitField[3])
            return;
        DivistionTo = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, NotificationLeagueChanged value)
    {
        value.Write(writer);
    }

    public static NotificationLeagueChanged ReadRecord(BinaryReader reader)
    {
        var notificationLeagueChanged = new NotificationLeagueChanged();
        notificationLeagueChanged.Read(reader);
        return notificationLeagueChanged;
    }
}