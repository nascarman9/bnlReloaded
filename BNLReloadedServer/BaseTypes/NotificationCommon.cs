using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class NotificationCommon : Notification
{
    public override NotificationType Type => NotificationType.Common;

    public bool Alarm { get; set; }

    public bool OnlyInMainMenu { get; set; }

    public bool KeepAlive { get; set; }

    public string? MessageTag { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, MessageTag != null).Write(writer);
        writer.Write(Alarm);
        writer.Write(OnlyInMainMenu);
        writer.Write(KeepAlive);
        if (MessageTag != null)
            writer.Write(MessageTag);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(4);
        bitField.Read(reader);
        if (bitField[0])
            Alarm = reader.ReadBoolean();
        if (bitField[1])
            OnlyInMainMenu = reader.ReadBoolean();
        if (bitField[2])
            KeepAlive = reader.ReadBoolean();
        if (bitField[3])
            MessageTag = reader.ReadString();
        else
            MessageTag = null;
    }

    public static void WriteRecord(BinaryWriter writer, NotificationCommon value)
    {
        value.Write(writer);
    }

    public static NotificationCommon ReadRecord(BinaryReader reader)
    {
        var notificationCommon = new NotificationCommon();
        notificationCommon.Read(reader);
        return notificationCommon;
    }
}