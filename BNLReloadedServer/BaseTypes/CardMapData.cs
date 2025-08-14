using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardMapData : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.MapData;

    public string Name { get; set; } = string.Empty;

    public int Version { get; set; }

    public int Schema { get; set; } = 2;

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, true, true, true).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      writer.Write(Name);
      writer.Write(Version);
      writer.Write(Schema);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      if (bitField[2])
        Name = reader.ReadString();
      if (bitField[3])
        Version = reader.ReadInt32();
      if (!bitField[4])
        return;
      Schema = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, CardMapData value) => value.Write(writer);

    public static CardMapData ReadRecord(BinaryReader reader)
    {
      var cardMapData = new CardMapData();
      cardMapData.Read(reader);
      return cardMapData;
    }
}