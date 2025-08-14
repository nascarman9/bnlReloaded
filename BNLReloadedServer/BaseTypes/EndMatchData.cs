using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class EndMatchData
{
    public float MatchSeconds { get; set; }

    public List<EndMatchPlayerData>? PlayersData { get; set; }

    public bool IsWinner { get; set; }

    public bool IsBackfiller { get; set; }

    public bool IsAfk { get; set; }

    public Key HeroKey { get; set; }

    public Key SkinKey { get; set; }

    public XpInfo? OldHeroXp { get; set; }

    public XpInfo? OldPlayerXp { get; set; }

    public XpInfo? NewHeroXp { get; set; }

    public float RewardXp { get; set; }

    public Dictionary<CurrencyType, float>? OldCurrency { get; set; }

    public Dictionary<CurrencyType, float>? RewardCurrency { get; set; }

    public Dictionary<MatchRewardBonusType, float>? RewardBonuses { get; set; }

    public float? XpBoost { get; set; }

    public float? GoldBoost { get; set; }

    public RankedMatchStatus RankedStatus { get; set; }

    public RankedMatchResultData? RankedData { get; set; }

    public List<ChallengeDiffData>? ChallengesData { get; set; }

    public TimeTrialResultData? TimeTrialData { get; set; }

    public Key? LootCrateKey { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, PlayersData != null, true, true, true, true, true, OldHeroXp != null, OldPlayerXp != null,
        NewHeroXp != null, true, OldCurrency != null, RewardCurrency != null, RewardBonuses != null, XpBoost.HasValue,
        GoldBoost.HasValue, true, RankedData != null, ChallengesData != null, TimeTrialData != null,
        LootCrateKey.HasValue).Write(writer);
      writer.Write(MatchSeconds);
      if (PlayersData != null)
        writer.WriteList(PlayersData, EndMatchPlayerData.WriteRecord);
      writer.Write(IsWinner);
      writer.Write(IsBackfiller);
      writer.Write(IsAfk);
      Key.WriteRecord(writer, HeroKey);
      Key.WriteRecord(writer, SkinKey);
      if (OldHeroXp != null)
        XpInfo.WriteRecord(writer, OldHeroXp);
      if (OldPlayerXp != null)
        XpInfo.WriteRecord(writer, OldPlayerXp);
      if (NewHeroXp != null)
        XpInfo.WriteRecord(writer, NewHeroXp);
      writer.Write(RewardXp);
      if (OldCurrency != null)
        writer.WriteMap(OldCurrency, writer.WriteByteEnum, writer.Write);
      if (RewardCurrency != null)
        writer.WriteMap(RewardCurrency, writer.WriteByteEnum, writer.Write);
      if (RewardBonuses != null)
        writer.WriteMap(RewardBonuses, writer.WriteByteEnum, writer.Write);
      if (XpBoost.HasValue)
        writer.Write(XpBoost.Value);
      if (GoldBoost.HasValue)
        writer.Write(GoldBoost.Value);
      writer.WriteByteEnum(RankedStatus);
      if (RankedData != null)
        RankedMatchResultData.WriteRecord(writer, RankedData);
      if (ChallengesData != null)
        writer.WriteList(ChallengesData, ChallengeDiffData.WriteRecord);
      if (TimeTrialData != null)
        TimeTrialResultData.WriteRecord(writer, TimeTrialData);
      if (!LootCrateKey.HasValue)
        return;
      Key.WriteRecord(writer, LootCrateKey.Value);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(21);
      bitField.Read(reader);
      if (bitField[0])
        MatchSeconds = reader.ReadSingle();
      PlayersData = bitField[1] ? reader.ReadList<EndMatchPlayerData, List<EndMatchPlayerData>>(EndMatchPlayerData.ReadRecord) : null;
      if (bitField[2])
        IsWinner = reader.ReadBoolean();
      if (bitField[3])
        IsBackfiller = reader.ReadBoolean();
      if (bitField[4])
        IsAfk = reader.ReadBoolean();
      if (bitField[5])
        HeroKey = Key.ReadRecord(reader);
      if (bitField[6])
        SkinKey = Key.ReadRecord(reader);
      OldHeroXp = bitField[7] ? XpInfo.ReadRecord(reader) : null;
      OldPlayerXp = bitField[8] ? XpInfo.ReadRecord(reader) : null;
      NewHeroXp = bitField[9] ? XpInfo.ReadRecord(reader) : null;
      if (bitField[10])
        RewardXp = reader.ReadSingle();
      OldCurrency = bitField[11] ? reader.ReadMap<CurrencyType, float, Dictionary<CurrencyType, float>>(reader.ReadByteEnum<CurrencyType>, reader.ReadSingle) : null;
      RewardCurrency = bitField[12] ? reader.ReadMap<CurrencyType, float, Dictionary<CurrencyType, float>>(reader.ReadByteEnum<CurrencyType>, reader.ReadSingle) : null;
      RewardBonuses = bitField[13] ? reader.ReadMap<MatchRewardBonusType, float, Dictionary<MatchRewardBonusType, float>>(reader.ReadByteEnum<MatchRewardBonusType>, reader.ReadSingle) : null;
      XpBoost = bitField[14] ? reader.ReadSingle() : null;
      GoldBoost = bitField[15] ? reader.ReadSingle() : null;
      if (bitField[16])
        RankedStatus = reader.ReadByteEnum<RankedMatchStatus>();
      RankedData = bitField[17] ? RankedMatchResultData.ReadRecord(reader) : null;
      ChallengesData = bitField[18] ? reader.ReadList<ChallengeDiffData, List<ChallengeDiffData>>(ChallengeDiffData.ReadRecord) : null;
      TimeTrialData = bitField[19] ? TimeTrialResultData.ReadRecord(reader) : null;
      LootCrateKey = bitField[20] ? Key.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, EndMatchData value) => value.Write(writer);

    public static EndMatchData ReadRecord(BinaryReader reader)
    {
      var endMatchData = new EndMatchData();
      endMatchData.Read(reader);
      return endMatchData;
    }
}