using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardBooster : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Booster;

    public LocalizedString? Name { get; set; }

    public LocalizedString? Description { get; set; }

    public string? Icon { get; set; }

    public float? MatchGoldBoost { get; set; }

    public float? MatchXpBoost { get; set; }

    public float? TimeTrialXpBoost { get; set; }

    public float? TimeTrialGoldBoost { get; set; }

    public float? ChallengeGoldBoost { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Name != null, Description != null, Icon != null, MatchGoldBoost.HasValue,
          MatchXpBoost.HasValue, TimeTrialXpBoost.HasValue, TimeTrialGoldBoost.HasValue, ChallengeGoldBoost.HasValue)
        .Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      if (Icon != null)
        writer.Write(Icon);
      if (MatchGoldBoost.HasValue)
        writer.Write(MatchGoldBoost.Value);
      if (MatchXpBoost.HasValue)
        writer.Write(MatchXpBoost.Value);
      if (TimeTrialXpBoost.HasValue)
        writer.Write(TimeTrialXpBoost.Value);
      if (TimeTrialGoldBoost.HasValue)
        writer.Write(TimeTrialGoldBoost.Value);
      if (!ChallengeGoldBoost.HasValue)
        return;
      writer.Write(ChallengeGoldBoost.Value);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(10);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Name = bitField[2] ? LocalizedString.ReadRecord(reader) : null;
      Description = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      Icon = bitField[4] ? reader.ReadString() : null;
      MatchGoldBoost = bitField[5] ? reader.ReadSingle() : null;
      MatchXpBoost = bitField[6] ? reader.ReadSingle() : null;
      TimeTrialXpBoost = bitField[7] ? reader.ReadSingle() : null;
      TimeTrialGoldBoost = bitField[8] ? reader.ReadSingle() : null;
      ChallengeGoldBoost = bitField[9] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardBooster value) => value.Write(writer);

    public static CardBooster ReadRecord(BinaryReader reader)
    {
      var cardBooster = new CardBooster();
      cardBooster.Read(reader);
      return cardBooster;
    }
}