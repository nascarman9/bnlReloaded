using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardGlobalLogic : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.GlobalLogic;

    public List<Key>? AvailableAchievements { get; set; }

    public List<Key>? AvailableBadges { get; set; }

    public Dictionary<BadgeType, int>? MaxBadgesByType { get; set; }

    public Dictionary<string, RegionGuiInfo>? Regions { get; set; }

    public List<Key>? AvailableHeroes { get; set; }

    public SquadLogic? Squad { get; set; }

    public MatchmakerLogic? Matchmaker { get; set; }

    public CustomGameLogic? CustomGame { get; set; }

    public TimeTrialLogic? TimeTrial { get; set; }

    public TutorialLogic? Tutorial { get; set; }

    public MatchMedalsLogic? Medals { get; set; }

    public LearningLogic? Learning { get; set; }

    public GlobalExpLogic? XpLogic { get; set; }

    public MeritLogic? MeritLogic { get; set; }

    public GraveyardLogic? Graveyard { get; set; }

    public LeaverRatingLogic? LeaverRating { get; set; }

    public TipsLogic? TipsLogic { get; set; }

    public SpectatorLogic? SpectatorLogic { get; set; }

    public RssLogic? RssLogic { get; set; }

    public ChristmasLogic? ChristmasLogic { get; set; }

    public QueueDodgeLogic? QueueDodgeLogic { get; set; }

    public AnticheatLogic? Anticheat { get; set; }

    public MatchKickLogic? MatchKick { get; set; }

    public GuiLogic? GuiLogic { get; set; }

    public ControlsLogic? ControlsLogic { get; set; }

    public ChallengeLogic? Challenge { get; set; }

    public CurrencyLogic? Currency { get; set; }

    public PerksLogic? Perks { get; set; }

    public LineOfSightLogic? LineOfSight { get; set; }

    public PlayerCommandsLogic? CommandsLogic { get; set; }

    public List<Key>? DeviceGroups { get; set; }

    public List<Key>? PurchasableDevices { get; set; }

    public LootCrateLogic? LootCrateLogic { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, AvailableAchievements != null, AvailableBadges != null, MaxBadgesByType != null,
        Regions != null, AvailableHeroes != null, Squad != null, Matchmaker != null, CustomGame != null,
        TimeTrial != null, Tutorial != null, Medals != null, Learning != null, XpLogic != null, MeritLogic != null,
        Graveyard != null, LeaverRating != null, TipsLogic != null, SpectatorLogic != null, RssLogic != null,
        ChristmasLogic != null, QueueDodgeLogic != null, Anticheat != null, MatchKick != null, GuiLogic != null,
        ControlsLogic != null, Challenge != null, Currency != null, Perks != null, LineOfSight != null,
        CommandsLogic != null, DeviceGroups != null, PurchasableDevices != null, LootCrateLogic != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (AvailableAchievements != null)
        writer.WriteList(AvailableAchievements, Key.WriteRecord);
      if (AvailableBadges != null)
        writer.WriteList(AvailableBadges, Key.WriteRecord);
      if (MaxBadgesByType != null)
        writer.WriteMap(MaxBadgesByType, writer.WriteByteEnum, writer.Write);
      if (Regions != null)
        writer.WriteMap(Regions, writer.Write, RegionGuiInfo.WriteRecord);
      if (AvailableHeroes != null)
        writer.WriteList(AvailableHeroes, Key.WriteRecord);
      if (Squad != null)
        SquadLogic.WriteRecord(writer, Squad);
      if (Matchmaker != null)
        MatchmakerLogic.WriteRecord(writer, Matchmaker);
      if (CustomGame != null)
        CustomGameLogic.WriteRecord(writer, CustomGame);
      if (TimeTrial != null)
        TimeTrialLogic.WriteRecord(writer, TimeTrial);
      if (Tutorial != null)
        TutorialLogic.WriteRecord(writer, Tutorial);
      if (Medals != null)
        MatchMedalsLogic.WriteRecord(writer, Medals);
      if (Learning != null)
        LearningLogic.WriteRecord(writer, Learning);
      if (XpLogic != null)
        GlobalExpLogic.WriteRecord(writer, XpLogic);
      if (MeritLogic != null)
        MeritLogic.WriteRecord(writer, MeritLogic);
      if (Graveyard != null)
        GraveyardLogic.WriteRecord(writer, Graveyard);
      if (LeaverRating != null)
        LeaverRatingLogic.WriteRecord(writer, LeaverRating);
      if (TipsLogic != null)
        TipsLogic.WriteRecord(writer, TipsLogic);
      if (SpectatorLogic != null)
        SpectatorLogic.WriteRecord(writer, SpectatorLogic);
      if (RssLogic != null)
        RssLogic.WriteRecord(writer, RssLogic);
      if (ChristmasLogic != null)
        ChristmasLogic.WriteRecord(writer, ChristmasLogic);
      if (QueueDodgeLogic != null)
        QueueDodgeLogic.WriteRecord(writer, QueueDodgeLogic);
      if (Anticheat != null)
        AnticheatLogic.WriteRecord(writer, Anticheat);
      if (MatchKick != null)
        MatchKickLogic.WriteRecord(writer, MatchKick);
      if (GuiLogic != null)
        GuiLogic.WriteRecord(writer, GuiLogic);
      if (ControlsLogic != null)
        ControlsLogic.WriteRecord(writer, ControlsLogic);
      if (Challenge != null)
        ChallengeLogic.WriteRecord(writer, Challenge);
      if (Currency != null)
        CurrencyLogic.WriteRecord(writer, Currency);
      if (Perks != null)
        PerksLogic.WriteRecord(writer, Perks);
      if (LineOfSight != null)
        LineOfSightLogic.WriteRecord(writer, LineOfSight);
      if (CommandsLogic != null)
        PlayerCommandsLogic.WriteRecord(writer, CommandsLogic);
      if (DeviceGroups != null)
        writer.WriteList(DeviceGroups, Key.WriteRecord);
      if (PurchasableDevices != null)
        writer.WriteList(PurchasableDevices, Key.WriteRecord);
      if (LootCrateLogic != null)
        LootCrateLogic.WriteRecord(writer, LootCrateLogic);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(35);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      AvailableAchievements = bitField[2] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      AvailableBadges = bitField[3] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      MaxBadgesByType = bitField[4] ? reader.ReadMap<BadgeType, int, Dictionary<BadgeType, int>>(reader.ReadByteEnum<BadgeType>, reader.ReadInt32) : null;
      Regions = bitField[5] ? reader.ReadMap<string, RegionGuiInfo, Dictionary<string, RegionGuiInfo>>(reader.ReadString, RegionGuiInfo.ReadRecord) : null;
      AvailableHeroes = bitField[6] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Squad = bitField[7] ? SquadLogic.ReadRecord(reader) : null;
      Matchmaker = bitField[8] ? MatchmakerLogic.ReadRecord(reader) : null;
      CustomGame = bitField[9] ? CustomGameLogic.ReadRecord(reader) : null;
      TimeTrial = bitField[10] ? TimeTrialLogic.ReadRecord(reader) : null;
      Tutorial = bitField[11] ? TutorialLogic.ReadRecord(reader) : null;
      Medals = bitField[12] ? MatchMedalsLogic.ReadRecord(reader) : null;
      Learning = bitField[13] ? LearningLogic.ReadRecord(reader) : null;
      XpLogic = bitField[14] ? GlobalExpLogic.ReadRecord(reader) : null;
      MeritLogic = bitField[15] ? MeritLogic.ReadRecord(reader) : null;
      Graveyard = bitField[16] ? GraveyardLogic.ReadRecord(reader) : null;
      LeaverRating = bitField[17] ? LeaverRatingLogic.ReadRecord(reader) : null;
      TipsLogic = bitField[18] ? TipsLogic.ReadRecord(reader) : null;
      SpectatorLogic = bitField[19] ? SpectatorLogic.ReadRecord(reader) : null;
      RssLogic = bitField[20] ? RssLogic.ReadRecord(reader) : null;
      ChristmasLogic = bitField[21] ? ChristmasLogic.ReadRecord(reader) : null;
      QueueDodgeLogic = bitField[22] ? QueueDodgeLogic.ReadRecord(reader) : null;
      Anticheat = bitField[23] ? AnticheatLogic.ReadRecord(reader) : null;
      MatchKick = bitField[24] ? MatchKickLogic.ReadRecord(reader) : null;
      GuiLogic = bitField[25] ? GuiLogic.ReadRecord(reader) : null;
      ControlsLogic = bitField[26] ? ControlsLogic.ReadRecord(reader) : null;
      Challenge = bitField[27] ? ChallengeLogic.ReadRecord(reader) : null;
      Currency = bitField[28] ? CurrencyLogic.ReadRecord(reader) : null;
      Perks = bitField[29] ? PerksLogic.ReadRecord(reader) : null;
      LineOfSight = bitField[30] ? LineOfSightLogic.ReadRecord(reader) : null;
      CommandsLogic = bitField[31] ? PlayerCommandsLogic.ReadRecord(reader) : null;
      DeviceGroups = bitField[32] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      PurchasableDevices = bitField[33] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      LootCrateLogic = bitField[34] ? LootCrateLogic.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardGlobalLogic value)
    {
      value.Write(writer);
    }

    public static CardGlobalLogic ReadRecord(BinaryReader reader)
    {
      var cardGlobalLogic = new CardGlobalLogic();
      cardGlobalLogic.Read(reader);
      return cardGlobalLogic;
    }
}