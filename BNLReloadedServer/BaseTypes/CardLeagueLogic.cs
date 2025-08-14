using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardLeagueLogic : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.LeagueLogic;

    public float DecayStartDays { get; set; } = 30f;

    public int DecayPointsPerDay { get; set; } = 5;

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, true, true).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      writer.Write(DecayStartDays);
      writer.Write(DecayPointsPerDay);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      if (bitField[2])
        DecayStartDays = reader.ReadSingle();
      if (!bitField[3])
        return;
      DecayPointsPerDay = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, CardLeagueLogic value)
    {
      value.Write(writer);
    }

    public static CardLeagueLogic ReadRecord(BinaryReader reader)
    {
      var cardLeagueLogic = new CardLeagueLogic();
      cardLeagueLogic.Read(reader);
      return cardLeagueLogic;
    }
}