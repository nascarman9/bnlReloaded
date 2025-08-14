using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class NotificationZeus : Notification
{
    public override NotificationType Type => NotificationType.Zeus;

    public string? Text { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(Text != null).Write(writer);
        if (Text != null)
            writer.Write(Text);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        Text = bitField[0] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, NotificationZeus value)
    {
        value.Write(writer);
    }

    public static NotificationZeus ReadRecord(BinaryReader reader)
    {
        var notificationZeus = new NotificationZeus();
        notificationZeus.Read(reader);
        return notificationZeus;
    }
}