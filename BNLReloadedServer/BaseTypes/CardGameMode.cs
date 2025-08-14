using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardGameMode : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.GameMode;

    public string? Name { get; set; }

    public int PlayersPerTeam { get; set; }

    public int MinPlayersInSquad { get; set; }

    public int MaxPlayersInSquad { get; set; }

    public int? MinPlayerLevel { get; set; }

    public GameModeHeroesReqs? HeroesReqs { get; set; }

    public Key MatchMode { get; set; }

    public int SelectionMapsCount { get; set; }

    public NoobMapsLogic? NoobMaps { get; set; }

    public LobbyMode? LobbyMode { get; set; }

    public LobbyHeroLimit? HeroLimit { get; set; }

    public GameRankingType Ranking { get; set; }

    public Dictionary<string, GameModeTimeConfig>? AvailabilityTimes { get; set; }

    public string? ClosingTag { get; set; }

    public MatchmakerConfig? Matchmaker { get; set; }

    public BackfillingLogic? Backfilling { get; set; }

    public AfkLogic? AntiAfk { get; set; }

    public bool AffectsLeaverRating { get; set; }

    public bool AffectedByLeaverRating { get; set; }

    public ExitMatchBehaviourType ExitMatchBehaviour { get; set; }

    public bool AllowDemoPlayers { get; set; }

    public bool AffectsChallenges { get; set; }

    public bool AffectsAchievements { get; set; }

    public MatchRewardLogic? RewardLogic { get; set; }

    public MatchExpLogic? XpLogic { get; set; }

    public MatchCurrencyLogic? CurrencyLogic { get; set; }

    public List<LootCrateMatchReward> LossCrateWeightedReward { get; set; } = [];

    public List<LootCrateMatchReward> WinCrateWeightedReward { get; set; } = [];

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Name != null, true, true, true, MinPlayerLevel.HasValue, HeroesReqs != null, true,
        true, NoobMaps != null, LobbyMode != null, HeroLimit != null, true, AvailabilityTimes != null,
        ClosingTag != null, Matchmaker != null, Backfilling != null, AntiAfk != null, true, true, true, true, true,
        true, RewardLogic != null, XpLogic != null, CurrencyLogic != null, true,
        true).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Name != null)
        writer.Write(Name);
      writer.Write(PlayersPerTeam);
      writer.Write(MinPlayersInSquad);
      writer.Write(MaxPlayersInSquad);
      if (MinPlayerLevel.HasValue)
        writer.Write(MinPlayerLevel.Value);
      if (HeroesReqs != null)
        GameModeHeroesReqs.WriteRecord(writer, HeroesReqs);
      Key.WriteRecord(writer, MatchMode);
      writer.Write(SelectionMapsCount);
      if (NoobMaps != null)
        NoobMapsLogic.WriteRecord(writer, NoobMaps);
      if (LobbyMode != null)
        LobbyMode.WriteVariant(writer, LobbyMode);
      if (HeroLimit != null)
        LobbyHeroLimit.WriteRecord(writer, HeroLimit);
      writer.WriteByteEnum(Ranking);
      if (AvailabilityTimes != null)
        writer.WriteMap(AvailabilityTimes, writer.Write, GameModeTimeConfig.WriteRecord);
      if (ClosingTag != null)
        writer.Write(ClosingTag);
      if (Matchmaker != null)
        MatchmakerConfig.WriteRecord(writer, Matchmaker);
      if (Backfilling != null)
        BackfillingLogic.WriteRecord(writer, Backfilling);
      if (AntiAfk != null)
        AfkLogic.WriteRecord(writer, AntiAfk);
      writer.Write(AffectsLeaverRating);
      writer.Write(AffectedByLeaverRating);
      writer.WriteByteEnum(ExitMatchBehaviour);
      writer.Write(AllowDemoPlayers);
      writer.Write(AffectsChallenges);
      writer.Write(AffectsAchievements);
      if (RewardLogic != null)
        MatchRewardLogic.WriteRecord(writer, RewardLogic);
      if (XpLogic != null)
        MatchExpLogic.WriteRecord(writer, XpLogic);
      if (CurrencyLogic != null)
        MatchCurrencyLogic.WriteRecord(writer, CurrencyLogic);
      writer.WriteList(LossCrateWeightedReward, LootCrateMatchReward.WriteRecord);
      writer.WriteList(WinCrateWeightedReward, LootCrateMatchReward.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(30);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Name = bitField[2] ? reader.ReadString() : null;
      if (bitField[3])
        PlayersPerTeam = reader.ReadInt32();
      if (bitField[4])
        MinPlayersInSquad = reader.ReadInt32();
      if (bitField[5])
        MaxPlayersInSquad = reader.ReadInt32();
      MinPlayerLevel = bitField[6] ? reader.ReadInt32() : null;
      HeroesReqs = bitField[7] ? GameModeHeroesReqs.ReadRecord(reader) : null;
      if (bitField[8])
        MatchMode = Key.ReadRecord(reader);
      if (bitField[9])
        SelectionMapsCount = reader.ReadInt32();
      NoobMaps = bitField[10] ? NoobMapsLogic.ReadRecord(reader) : null;
      LobbyMode = bitField[11] ? LobbyMode.ReadVariant(reader) : null;
      HeroLimit = bitField[12] ? LobbyHeroLimit.ReadRecord(reader) : null;
      if (bitField[13])
        Ranking = reader.ReadByteEnum<GameRankingType>();
      AvailabilityTimes = bitField[14] ? reader.ReadMap<string, GameModeTimeConfig, Dictionary<string, GameModeTimeConfig>>(reader.ReadString, GameModeTimeConfig.ReadRecord): null;
      ClosingTag = bitField[15] ? reader.ReadString() : null;
      Matchmaker = bitField[16] ? MatchmakerConfig.ReadRecord(reader) : null;
      Backfilling = bitField[17] ? BackfillingLogic.ReadRecord(reader) : null;
      AntiAfk = bitField[18] ? AfkLogic.ReadRecord(reader) : null;
      if (bitField[19])
        AffectsLeaverRating = reader.ReadBoolean();
      if (bitField[20])
        AffectedByLeaverRating = reader.ReadBoolean();
      if (bitField[21])
        ExitMatchBehaviour = reader.ReadByteEnum<ExitMatchBehaviourType>();
      if (bitField[22])
        AllowDemoPlayers = reader.ReadBoolean();
      if (bitField[23])
        AffectsChallenges = reader.ReadBoolean();
      if (bitField[24])
        AffectsAchievements = reader.ReadBoolean();
      RewardLogic = bitField[25] ? MatchRewardLogic.ReadRecord(reader) : null;
      XpLogic = bitField[26] ? MatchExpLogic.ReadRecord(reader) : null;
      CurrencyLogic = bitField[27] ? MatchCurrencyLogic.ReadRecord(reader) : null;
      if (bitField[28])
        LossCrateWeightedReward = reader.ReadList<LootCrateMatchReward, List<LootCrateMatchReward>>(LootCrateMatchReward.ReadRecord);
      if (bitField[29])
        WinCrateWeightedReward = reader.ReadList<LootCrateMatchReward, List<LootCrateMatchReward>>(LootCrateMatchReward.ReadRecord);
    }

    public static void WriteRecord(BinaryWriter writer, CardGameMode value) => value.Write(writer);

    public static CardGameMode ReadRecord(BinaryReader reader)
    {
      var cardGameMode = new CardGameMode();
      cardGameMode.Read(reader);
      return cardGameMode;
    }
}