using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardMap : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Map;

    public LocalizedString? Name { get; set; }

    public LocalizedString? Description { get; set; }

    public string? Image { get; set; }

    public string? LargeImage { get; set; }

    public MapData? Data { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Name != null, Description != null, Image != null, LargeImage != null, Data != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      if (Image != null)
        writer.Write(Image);
      if (LargeImage != null)
        writer.Write(LargeImage);
      if (Data == null)
        return;
      MapData.WriteRecord(writer, Data);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Name = bitField[2] ? LocalizedString.ReadRecord(reader) : null;
      Description = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      Image = bitField[4] ? reader.ReadString() : null;
      LargeImage = bitField[5] ? reader.ReadString() : null;
      Data = bitField[6] ? MapData.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardMap value) => value.Write(writer);

    public static CardMap ReadRecord(BinaryReader reader)
    {
      var cardMap = new CardMap();
      cardMap.Read(reader);
      return cardMap;
    }
}