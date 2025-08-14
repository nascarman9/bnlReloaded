using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class NotificationDemerit : Notification
{
    public override NotificationType Type => NotificationType.Demerit;

    public DemeritReason Reason { get; set; }

    public float Value { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        writer.WriteByteEnum(Reason);
        writer.Write(Value);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            Reason = reader.ReadByteEnum<DemeritReason>();
        if (!bitField[1])
            return;
        Value = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, NotificationDemerit value)
    {
        value.Write(writer);
    }

    public static NotificationDemerit ReadRecord(BinaryReader reader)
    {
        var notificationDemerit = new NotificationDemerit();
        notificationDemerit.Read(reader);
        return notificationDemerit;
    }
}