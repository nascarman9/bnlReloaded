using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class NotificationGraveyard : Notification
{
    public override NotificationType Type => NotificationType.Graveyard;

    public int EnterCount { get; set; }

    public ulong? EndTime { get; set; }

    public GraveyardEnterReason Reason { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, EndTime.HasValue, true).Write(writer);
        writer.Write(EnterCount);
        if (EndTime.HasValue)
            writer.Write(EndTime.Value);
        writer.WriteByteEnum(Reason);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            EnterCount = reader.ReadInt32();
        EndTime = bitField[1] ? reader.ReadUInt64() : null;
        if (!bitField[2])
            return;
        Reason = reader.ReadByteEnum<GraveyardEnterReason>();
    }

    public static void WriteRecord(BinaryWriter writer, NotificationGraveyard value)
    {
        value.Write(writer);
    }

    public static NotificationGraveyard ReadRecord(BinaryReader reader)
    {
        var notificationGraveyard = new NotificationGraveyard();
        notificationGraveyard.Read(reader);
        return notificationGraveyard;
    }
}