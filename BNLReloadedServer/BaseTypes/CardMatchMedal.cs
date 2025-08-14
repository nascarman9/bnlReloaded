using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ProtocolInterfaces;

namespace BNLReloadedServer.BaseTypes;

public class CardMatchMedal : Card, IIcon
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.MatchMedal;

    public string? Icon { get; set; }

    public LocalizedString? Name { get; set; }

    public LocalizedString? Description { get; set; }

    public Dictionary<ScoreType, float>? ServerCounters { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Icon != null, Name != null, Description != null, ServerCounters != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Icon != null)
        writer.Write(Icon);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      if (ServerCounters != null)
        writer.WriteMap(ServerCounters, writer.WriteByteEnum, writer.Write);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Icon = bitField[2] ? reader.ReadString() : null;
      Name = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      Description = bitField[4] ? LocalizedString.ReadRecord(reader) : null;
      ServerCounters = bitField[5] ? reader.ReadMap<ScoreType, float, Dictionary<ScoreType, float>>(reader.ReadByteEnum<ScoreType>, reader.ReadSingle) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardMatchMedal value)
    {
      value.Write(writer);
    }

    public static CardMatchMedal ReadRecord(BinaryReader reader)
    {
      var cardMatchMedal = new CardMatchMedal();
      cardMatchMedal.Read(reader);
      return cardMatchMedal;
    }
}