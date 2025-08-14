using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardStrings : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Strings;

    public Dictionary<string, LocalizedString>? Strings { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Strings != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Strings != null)
        writer.WriteMap(Strings, writer.Write, LocalizedString.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Strings = bitField[2] ? reader.ReadMap<string, LocalizedString, Dictionary<string, LocalizedString>>(reader.ReadString, LocalizedString.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardStrings value) => value.Write(writer);

    public static CardStrings ReadRecord(BinaryReader reader)
    {
      var cardStrings = new CardStrings();
      cardStrings.Read(reader);
      return cardStrings;
    }
}