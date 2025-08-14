using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardNotification : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Notification;

    public ColorFloat? MessageColor { get; set; }

    public LocalizedString? BigNotifyText { get; set; }

    public LocalizedString? SmallNotifyText { get; set; }

    public LocalizedString? ChatNotifyText { get; set; }

    public string? WorldNotifyIcon { get; set; }

    public string? PlayerNotifySound { get; set; }

    public string? MinimapNotifyPrefab { get; set; }

    public bool ShowOnKillScroll { get; set; }

    public float? Range { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, MessageColor.HasValue, BigNotifyText != null, SmallNotifyText != null,
        ChatNotifyText != null, WorldNotifyIcon != null, PlayerNotifySound != null, MinimapNotifyPrefab != null, true,
        Range.HasValue).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (MessageColor.HasValue)
        writer.Write(MessageColor.Value);
      if (BigNotifyText != null)
        LocalizedString.WriteRecord(writer, BigNotifyText);
      if (SmallNotifyText != null)
        LocalizedString.WriteRecord(writer, SmallNotifyText);
      if (ChatNotifyText != null)
        LocalizedString.WriteRecord(writer, ChatNotifyText);
      if (WorldNotifyIcon != null)
        writer.Write(WorldNotifyIcon);
      if (PlayerNotifySound != null)
        writer.Write(PlayerNotifySound);
      if (MinimapNotifyPrefab != null)
        writer.Write(MinimapNotifyPrefab);
      writer.Write(ShowOnKillScroll);
      if (!Range.HasValue)
        return;
      writer.Write(Range.Value);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(11);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      MessageColor = bitField[2] ? reader.ReadColorFloat() : null;
      BigNotifyText = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      SmallNotifyText = bitField[4] ? LocalizedString.ReadRecord(reader) : null;
      ChatNotifyText = bitField[5] ? LocalizedString.ReadRecord(reader) : null;
      WorldNotifyIcon = bitField[6] ? reader.ReadString() : null;
      PlayerNotifySound = bitField[7] ? reader.ReadString() : null;
      MinimapNotifyPrefab = bitField[8] ? reader.ReadString() : null;
      if (bitField[9])
        ShowOnKillScroll = reader.ReadBoolean();
      Range = bitField[10] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardNotification value)
    {
      value.Write(writer);
    }

    public static CardNotification ReadRecord(BinaryReader reader)
    {
      var cardNotification = new CardNotification();
      cardNotification.Read(reader);
      return cardNotification;
    }
}