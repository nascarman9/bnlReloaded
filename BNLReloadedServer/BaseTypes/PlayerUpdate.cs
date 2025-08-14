using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class PlayerUpdate
{
    public string? Nickname { get; set; }

    public League? League { get; set; }

    public PlayerProgression? Progression { get; set; }

    public List<FriendInfo>? Friends { get; set; }

    public List<FriendRequest>? RequestsFromFriends { get; set; }

    public List<FriendRequest>? RequestsFromMe { get; set; }

    public float? Merits { get; set; }

    public float? LeaverRating { get; set; }

    public LeaverState? LeaverState { get; set; }

    public Dictionary<int, Notification>? Notifications { get; set; }

    public float? Influence { get; set; }

    public bool? GraveyardPermanent { get; set; }

    public ulong? GraveyardLeaveTime { get; set; }

    public Dictionary<BadgeType, List<Key>>? SelectedBadges { get; set; }

    public List<uint>? VoiceMute { get; set; }

    public ulong? MatchmakerBanEnd { get; set; }

    public bool? LookingForFriends { get; set; }

    public int? TutorialTokens { get; set; }

    public bool? TutorialCompleted { get; set; }

    public List<Challenge?>? Challenges { get; set; }

    public int? ChallengeRefusesLeft { get; set; }

    public ulong? ChallengeDayEndTime { get; set; }

    public int? ChallengesCompleted { get; set; }

    public Dictionary<CurrencyType, float>? Currency { get; set; }

    public List<InventoryItem>? Inventory { get; set; }

    public List<Key>? OneTimeRewards { get; set; }

    public bool? DailyMatchPlayed { get; set; }

    public bool? DailyWinAvailable { get; set; }

    public int? FullMatchesPlayed { get; set; }

    public TimeTrialData? TimeTrial { get; set; }

    public Dictionary<Key, GameModeState>? GameModeStates { get; set; }

    public bool? IsInSquadFinder { get; set; }

    public SquadFinderSettings? SquadFinderSettings { get; set; }

    public List<SquadFinderPlayerData>? SquadFinderPlayers { get; set; }

    public Dictionary<Key, int>? DeviceLevels { get; set; }

    public Dictionary<Key, int>? Rubbles { get; set; }

    public int? NextLootCrateTime { get; set; }

    public Dictionary<Key, int>? LootCrates { get; set; }

    public Key? LastPlayedHero { get; set; }

    public List<Key>? NewItems { get; set; }

    public List<Key>? HeroesOnRotation { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Nickname != null, League != null, Progression != null, Friends != null, RequestsFromFriends != null,
        RequestsFromMe != null, Merits.HasValue, LeaverRating.HasValue, LeaverState.HasValue, Notifications != null,
        Influence.HasValue, GraveyardPermanent.HasValue, GraveyardLeaveTime.HasValue, SelectedBadges != null,
        VoiceMute != null, MatchmakerBanEnd.HasValue, LookingForFriends.HasValue, TutorialTokens.HasValue,
        TutorialCompleted.HasValue, Challenges != null, ChallengeRefusesLeft.HasValue, ChallengeDayEndTime.HasValue,
        ChallengesCompleted.HasValue, Currency != null, Inventory != null, OneTimeRewards != null,
        DailyMatchPlayed.HasValue, DailyWinAvailable.HasValue, FullMatchesPlayed.HasValue, TimeTrial != null,
        GameModeStates != null, IsInSquadFinder.HasValue, SquadFinderSettings != null, SquadFinderPlayers != null,
        DeviceLevels != null, Rubbles != null, NextLootCrateTime.HasValue, LootCrates != null, LastPlayedHero.HasValue,
        NewItems != null, HeroesOnRotation != null).Write(writer);
      if (Nickname != null)
        writer.Write(Nickname);
      if (League != null)
        League.WriteRecord(writer, League);
      if (Progression != null)
        PlayerProgression.WriteRecord(writer, Progression);
      if (Friends != null)
        writer.WriteList(Friends, FriendInfo.WriteRecord);
      if (RequestsFromFriends != null)
        writer.WriteList(RequestsFromFriends, FriendRequest.WriteRecord);
      if (RequestsFromMe != null)
        writer.WriteList(RequestsFromMe, FriendRequest.WriteRecord);
      if (Merits.HasValue)
        writer.Write(Merits.Value);
      if (LeaverRating.HasValue)
        writer.Write(LeaverRating.Value);
      if (LeaverState.HasValue)
        writer.WriteByteEnum(LeaverState.Value);
      if (Notifications != null)
        writer.WriteMap(Notifications, writer.Write, Notification.WriteVariant);
      if (Influence.HasValue)
        writer.Write(Influence.Value);
      if (GraveyardPermanent.HasValue)
        writer.Write(GraveyardPermanent.Value);
      if (GraveyardLeaveTime.HasValue)
        writer.Write(GraveyardLeaveTime.Value);
      if (SelectedBadges != null)
        writer.WriteMap(SelectedBadges, writer.WriteByteEnum, item => writer.WriteList(item, Key.WriteRecord));
      if (VoiceMute != null)
        writer.WriteList(VoiceMute, writer.Write);
      if (MatchmakerBanEnd.HasValue)
        writer.Write(MatchmakerBanEnd.Value);
      if (LookingForFriends.HasValue)
        writer.Write(LookingForFriends.Value);
      if (TutorialTokens.HasValue)
        writer.Write(TutorialTokens.Value);
      if (TutorialCompleted.HasValue)
        writer.Write(TutorialCompleted.Value);
      if (Challenges != null)
        writer.WriteList(Challenges, item => writer.WriteOption(item, Challenge.WriteRecord));
      if (ChallengeRefusesLeft.HasValue)
        writer.Write(ChallengeRefusesLeft.Value);
      if (ChallengeDayEndTime.HasValue)
        writer.Write(ChallengeDayEndTime.Value);
      if (ChallengesCompleted.HasValue)
        writer.Write(ChallengesCompleted.Value);
      if (Currency != null)
        writer.WriteMap(Currency, writer.WriteByteEnum, writer.Write);
      if (Inventory != null)
        writer.WriteList(Inventory, InventoryItem.WriteRecord);
      if (OneTimeRewards != null)
        writer.WriteList(OneTimeRewards, Key.WriteRecord);
      if (DailyMatchPlayed.HasValue)
        writer.Write(DailyMatchPlayed.Value);
      if (DailyWinAvailable.HasValue)
        writer.Write(DailyWinAvailable.Value);
      if (FullMatchesPlayed.HasValue)
        writer.Write(FullMatchesPlayed.Value);
      if (TimeTrial != null)
        TimeTrialData.WriteRecord(writer, TimeTrial);
      if (GameModeStates != null)
        writer.WriteMap(GameModeStates, Key.WriteRecord, GameModeState.WriteRecord);
      if (IsInSquadFinder.HasValue)
        writer.Write(IsInSquadFinder.Value);
      if (SquadFinderSettings != null)
        SquadFinderSettings.WriteRecord(writer, SquadFinderSettings);
      if (SquadFinderPlayers != null)
        writer.WriteList(SquadFinderPlayers, SquadFinderPlayerData.WriteRecord);
      if (DeviceLevels != null)
        writer.WriteMap(DeviceLevels, Key.WriteRecord, writer.Write);
      if (Rubbles != null)
        writer.WriteMap(Rubbles, Key.WriteRecord, writer.Write);
      if (NextLootCrateTime.HasValue)
        writer.Write(NextLootCrateTime.Value);
      if (LootCrates != null)
        writer.WriteMap(LootCrates, Key.WriteRecord, writer.Write);
      if (LastPlayedHero.HasValue)
        Key.WriteRecord(writer, LastPlayedHero.Value);
      if (NewItems != null)
        writer.WriteList(NewItems, Key.WriteRecord);
      if (HeroesOnRotation == null)
        return;
      writer.WriteList(HeroesOnRotation, Key.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(41);
      bitField.Read(reader);
      Nickname = bitField[0] ? reader.ReadString() : null;
      League = bitField[1] ? League.ReadRecord(reader) : null;
      Progression = bitField[2] ? PlayerProgression.ReadRecord(reader) : null;
      Friends = bitField[3] ? reader.ReadList<FriendInfo, List<FriendInfo>>(FriendInfo.ReadRecord) : null;
      RequestsFromFriends = bitField[4] ? reader.ReadList<FriendRequest, List<FriendRequest>>(FriendRequest.ReadRecord) : null;
      RequestsFromMe = bitField[5] ? reader.ReadList<FriendRequest, List<FriendRequest>>(FriendRequest.ReadRecord) : null;
      Merits = bitField[6] ? reader.ReadSingle() : null;
      LeaverRating = bitField[7] ? reader.ReadSingle() : null;
      LeaverState = bitField[8] ? reader.ReadByteEnum<LeaverState>() : null;
      Notifications = bitField[9] ? reader.ReadMap<int, Notification, Dictionary<int, Notification>>(reader.ReadInt32, Notification.ReadVariant) : null;
      Influence = bitField[10] ? reader.ReadSingle() : null;
      GraveyardPermanent = bitField[11] ? reader.ReadBoolean() : null;
      GraveyardLeaveTime = bitField[12] ? reader.ReadUInt64() : null;
      SelectedBadges = bitField[13] ? reader.ReadMap<BadgeType, List<Key>, Dictionary<BadgeType, List<Key>>>(reader.ReadByteEnum<BadgeType>, () => reader.ReadList<Key, List<Key>>(Key.ReadRecord)) : null;
      VoiceMute = bitField[14] ? reader.ReadList<uint, List<uint>>(reader.ReadUInt32) : null;
      MatchmakerBanEnd = bitField[15] ? reader.ReadUInt64() : null;
      LookingForFriends = bitField[16] ? reader.ReadBoolean() : null;
      TutorialTokens = bitField[17] ? reader.ReadInt32() : null;
      TutorialCompleted = bitField[18] ? reader.ReadBoolean() : null;
      Challenges = bitField[19] ? reader.ReadList<Challenge?, List<Challenge?>>(() => reader.ReadOption(Challenge.ReadRecord)) : null;
      ChallengeRefusesLeft = bitField[20] ? reader.ReadInt32() : null;
      ChallengeDayEndTime = bitField[21] ? reader.ReadUInt64() : null;
      ChallengesCompleted = bitField[22] ? reader.ReadInt32() : null;
      Currency = bitField[23] ? reader.ReadMap<CurrencyType, float, Dictionary<CurrencyType, float>>(reader.ReadByteEnum<CurrencyType>, reader.ReadSingle) : null;
      Inventory = bitField[24] ? reader.ReadList<InventoryItem, List<InventoryItem>>(InventoryItem.ReadRecord) : null;
      OneTimeRewards = bitField[25] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      DailyMatchPlayed = bitField[26] ? reader.ReadBoolean() : null;
      DailyWinAvailable = bitField[27] ? reader.ReadBoolean() : null;
      FullMatchesPlayed = bitField[28] ? reader.ReadInt32() : null;
      TimeTrial = bitField[29] ? TimeTrialData.ReadRecord(reader) : null;
      GameModeStates = bitField[30] ? reader.ReadMap<Key, GameModeState, Dictionary<Key, GameModeState>>(Key.ReadRecord, GameModeState.ReadRecord) : null;
      IsInSquadFinder = bitField[31] ? reader.ReadBoolean() : null;
      SquadFinderSettings = bitField[32] ? SquadFinderSettings.ReadRecord(reader) : null;
      SquadFinderPlayers = bitField[33] ? reader.ReadList<SquadFinderPlayerData, List<SquadFinderPlayerData>>(SquadFinderPlayerData.ReadRecord) : null;
      DeviceLevels = bitField[34] ? reader.ReadMap<Key, int, Dictionary<Key, int>>(Key.ReadRecord, reader.ReadInt32) : null;
      Rubbles = bitField[35] ? reader.ReadMap<Key, int, Dictionary<Key, int>>(Key.ReadRecord, reader.ReadInt32) : null;
      NextLootCrateTime = bitField[36] ? reader.ReadInt32() : null;
      LootCrates = bitField[37] ? reader.ReadMap<Key, int, Dictionary<Key, int>>(Key.ReadRecord, reader.ReadInt32) : null;
      LastPlayedHero = bitField[38] ? Key.ReadRecord(reader) : null;
      NewItems = bitField[39] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      HeroesOnRotation = bitField[40] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, PlayerUpdate value) => value.Write(writer);

    public static PlayerUpdate ReadRecord(BinaryReader reader)
    {
      var playerUpdate = new PlayerUpdate();
      playerUpdate.Read(reader);
      return playerUpdate;
    }
}