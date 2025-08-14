using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ProtocolInterfaces;

namespace BNLReloadedServer.BaseTypes;

public class CardRubble : Card, IIcon
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Rubble;

    public string? Icon { get; set; }

    public LocalizedString? Name { get; set; }

    public LocalizedString? Description { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Icon != null, Name != null, Description != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Icon != null)
        writer.Write(Icon);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Description == null)
        return;
      LocalizedString.WriteRecord(writer, Description);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Icon = bitField[2] ? reader.ReadString() : null;
      Name = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      Description = bitField[4] ? LocalizedString.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardRubble value) => value.Write(writer);

    public static CardRubble ReadRecord(BinaryReader reader)
    {
      var cardRubble = new CardRubble();
      cardRubble.Read(reader);
      return cardRubble;
    }
}