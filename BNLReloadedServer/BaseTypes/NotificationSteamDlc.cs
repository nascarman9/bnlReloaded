using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class NotificationSteamDlc : Notification
{
    public override NotificationType Type => NotificationType.SteamDlc;

    public uint DlcId { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.Write(DlcId);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        DlcId = reader.ReadUInt32();
    }

    public static void WriteRecord(BinaryWriter writer, NotificationSteamDlc value)
    {
        value.Write(writer);
    }

    public static NotificationSteamDlc ReadRecord(BinaryReader reader)
    {
        var notificationSteamDlc = new NotificationSteamDlc();
        notificationSteamDlc.Read(reader);
        return notificationSteamDlc;
    }
}