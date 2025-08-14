using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardAchievement : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Achievement;

    public string? SteamTag { get; set; }

    public Dictionary<string, int>? RequiresStatsInt { get; set; }

    public Dictionary<string, float>? RequiresStatsFloat { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, SteamTag != null, RequiresStatsInt != null, RequiresStatsFloat != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (SteamTag != null)
        writer.Write(SteamTag);
      if (RequiresStatsInt != null)
        writer.WriteMap(RequiresStatsInt, writer.Write, writer.Write);
      if (RequiresStatsFloat != null)
        writer.WriteMap(RequiresStatsFloat, writer.Write, writer.Write);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      SteamTag = bitField[2] ? reader.ReadString() : null;
      RequiresStatsInt = bitField[3] ? reader.ReadMap<string, int, Dictionary<string, int>>(reader.ReadString, reader.ReadInt32) : null;
      RequiresStatsFloat = bitField[4] ? reader.ReadMap<string, float, Dictionary<string, float>>(reader.ReadString, reader.ReadSingle) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardAchievement value)
    {
      value.Write(writer);
    }

    public static CardAchievement ReadRecord(BinaryReader reader)
    {
      var cardAchievement = new CardAchievement();
      cardAchievement.Read(reader);
      return cardAchievement;
    }
}