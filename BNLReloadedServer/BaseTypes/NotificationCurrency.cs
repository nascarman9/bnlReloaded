using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class NotificationCurrency : Notification
{
    public override NotificationType Type => NotificationType.Currency;

    public Dictionary<CurrencyType, float>? Delta { get; set; }

    public string? SourceTag { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(Delta != null, SourceTag != null).Write(writer);
        if (Delta != null)
            writer.WriteMap(Delta, writer.WriteByteEnum, writer.Write);
        if (SourceTag != null)
            writer.Write(SourceTag);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        Delta = bitField[0] ? reader.ReadMap<CurrencyType, float, Dictionary<CurrencyType, float>>(reader.ReadByteEnum<CurrencyType>, reader.ReadSingle) : null;
        SourceTag = bitField[1] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, NotificationCurrency value)
    {
        value.Write(writer);
    }

    public static NotificationCurrency ReadRecord(BinaryReader reader)
    {
        var notificationCurrency = new NotificationCurrency();
        notificationCurrency.Read(reader);
        return notificationCurrency;
    }
}