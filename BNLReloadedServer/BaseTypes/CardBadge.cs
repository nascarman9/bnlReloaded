using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ProtocolInterfaces;

namespace BNLReloadedServer.BaseTypes;

public class CardBadge : Card, IIcon, IUnlockable
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Badge;

    public string? Icon { get; set; }

    public LocalizedString? Name { get; set; }

    public LocalizedString? Description { get; set; }

    public bool VisibleInEmblems { get; set; }

    public BadgeType BadgeType { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Icon != null, Name != null, Description != null, true, true).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Icon != null)
        writer.Write(Icon);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      writer.Write(VisibleInEmblems);
      writer.WriteByteEnum(BadgeType);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Icon = bitField[2] ? reader.ReadString() : null;
      Name = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      Description = bitField[4] ? LocalizedString.ReadRecord(reader) : null;
      if (bitField[5])
        VisibleInEmblems = reader.ReadBoolean();
      if (!bitField[6])
        return;
      BadgeType = reader.ReadByteEnum<BadgeType>();
    }

    public static void WriteRecord(BinaryWriter writer, CardBadge value) => value.Write(writer);

    public static CardBadge ReadRecord(BinaryReader reader)
    {
      var cardBadge = new CardBadge();
      cardBadge.Read(reader);
      return cardBadge;
    }
}