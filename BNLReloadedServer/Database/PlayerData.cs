using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ProtocolHelpers;
using Moserware.Skills;

namespace BNLReloadedServer.Database;

public class PlayerData
{
    public uint PlayerId { get; set; }
    public ulong SteamId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public PlayerRole Role { get; set; } = PlayerRole.User;
    public string? Region { get; set; }
    public Rating Rating { get; set; } = new(25, 25.0 / 3.0);
    public League League { get; set; } = new();
    public PlayerProgression Progression { get; set; } = new();
    public List<uint> Friends { get; set; } = [];
    public List<uint> RequestsFromFriends { get; set; } = [];
    public List<uint> RequestsFromMe { get; set; } = [];
    public bool LookingForFriends { get; set; }
    public Dictionary<int, Notification> Notifications { get; set; } = new();
    public Dictionary<BadgeType, List<Key>> Badges { get; set; } = new();
    public Key? LastPlayedHero { get; set; }
    public Dictionary<Key, LobbyLoadout> HeroLoadouts { get; set; } = new();
    public List<HeroStats> HeroStats { get; set; } = [];
    public List<MatchHistoryRecord> MatchHistory { get; set; } = [];
    public int TutorialTokens { get; set; }
    public TimeTrialData TimeTrial { get; set; } = new();
    public ulong? MatchmakerBanEnd { get; set; }
    public bool? GraveyardPermanent { get; set; }
    public ulong? GraveyardLeaveTime { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, true, Region != null, true, true, true, true, true, true, true, true, true, LastPlayedHero.HasValue, true, true, true,
            true, true, MatchmakerBanEnd.HasValue, GraveyardPermanent.HasValue, GraveyardLeaveTime.HasValue).Write(writer);
        writer.Write(PlayerId);
        writer.Write(SteamId);
        writer.Write(Nickname);
        writer.WriteByteEnum(Role);
        if (Region != null)
        {
            writer.Write(Region);
        }
        writer.Write(Rating);
        League.WriteRecord(writer, League);
        PlayerProgression.WriteRecord(writer, Progression);
        writer.WriteList(Friends, writer.Write);
        writer.WriteList(RequestsFromFriends, writer.Write);
        writer.WriteList(RequestsFromMe, writer.Write);
        writer.Write(LookingForFriends);
        writer.WriteMap(Notifications, writer.Write, Notification.WriteVariant);
        writer.WriteMap(Badges, writer.WriteByteEnum, list => writer.WriteList(list, Key.WriteRecord));
        if (LastPlayedHero.HasValue)
        {
            Key.WriteRecord(writer, LastPlayedHero.Value);
        }
        writer.WriteMap(HeroLoadouts, Key.WriteRecord, LobbyLoadout.WriteRecord);
        writer.WriteList(HeroStats, BaseTypes.HeroStats.WriteRecord);
        writer.WriteList(MatchHistory, MatchHistoryRecord.WriteRecord);
        writer.Write(TutorialTokens);
        TimeTrialData.WriteRecord(writer, TimeTrial);
        if (MatchmakerBanEnd.HasValue)
        {
            writer.Write(MatchmakerBanEnd.Value);
        }

        if (GraveyardPermanent.HasValue)
        {
            writer.Write(GraveyardPermanent.Value);
        }

        if (GraveyardLeaveTime.HasValue)
        {
            writer.Write(GraveyardLeaveTime.Value);
        }
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(23);
        bitField.Read(reader);
        if (bitField[0])
        {
            PlayerId = reader.ReadUInt32();
        }

        if (bitField[1])
        {
            SteamId = reader.ReadUInt64();
        }

        if (bitField[2])
        {
            Nickname = reader.ReadString();
        }

        if (bitField[3])
        {
            Role = reader.ReadByteEnum<PlayerRole>();
        }

        Region = bitField[4] ? reader.ReadString() : null;

        if (bitField[5])
        {
            Rating = reader.ReadRating();
        }

        if (bitField[6])
        {
            League = League.ReadRecord(reader);
        }

        if (bitField[7])
        {
            Progression = PlayerProgression.ReadRecord(reader);
        }

        if (bitField[8])
        {
            Friends = reader.ReadList<uint, List<uint>>(reader.ReadUInt32);
        }

        if (bitField[9])
        {
            RequestsFromFriends = reader.ReadList<uint, List<uint>>(reader.ReadUInt32);
        }

        if (bitField[10])
        {
            RequestsFromMe = reader.ReadList<uint, List<uint>>(reader.ReadUInt32);
        }

        if (bitField[11])
        {
            LookingForFriends = reader.ReadBoolean();
        }

        if (bitField[12])
        {
            Notifications =
                reader.ReadMap<int, Notification, Dictionary<int, Notification>>(reader.ReadInt32,
                    Notification.ReadVariant);
        }

        if (bitField[13])
        {
            Badges = reader.ReadMap<BadgeType, List<Key>, Dictionary<BadgeType, List<Key>>>(
                reader.ReadByteEnum<BadgeType>, () => reader.ReadList<Key, List<Key>>(Key.ReadRecord));
        }

        LastPlayedHero = bitField[14] ? Key.ReadRecord(reader) : null;

        if (bitField[15])
        {
            HeroLoadouts = reader.ReadMap<Key, LobbyLoadout, Dictionary<Key, LobbyLoadout>>(Key.ReadRecord, LobbyLoadout.ReadRecord);
        }

        if (bitField[16])
        {
            HeroStats = reader.ReadList<HeroStats, List<HeroStats>>(BaseTypes.HeroStats.ReadRecord);
        }

        if (bitField[17])
        {
            MatchHistory = reader.ReadList<MatchHistoryRecord, List<MatchHistoryRecord>>(MatchHistoryRecord.ReadRecord);
        }

        if (bitField[18])
        {
            TutorialTokens = reader.ReadInt32();
        }

        if (bitField[19])
        {
            TimeTrial = TimeTrialData.ReadRecord(reader);
        }
        
        MatchmakerBanEnd = bitField[20] ? reader.ReadUInt64() : null;
        GraveyardPermanent = bitField[21] ? reader.ReadBoolean() : null;
        GraveyardLeaveTime = bitField[22] ? reader.ReadUInt64() : null;
    }
    
    public static void WriteRecord(BinaryWriter writer, PlayerData value) => value.Write(writer);

    public static PlayerData ReadRecord(BinaryReader reader)
    {
        var playerData = new PlayerData();
        playerData.Read(reader);
        return playerData;
    }
    
    public static byte[] WriteBadgeByteRecord(Dictionary<BadgeType, List<Key>> badges)
    {
        var memStream = new MemoryStream();
        using var writer = new BinaryWriter(memStream);
        writer.WriteMap(badges, writer.WriteByteEnum, list => writer.WriteList(list, Key.WriteRecord));
        return memStream.ToArray();
    }

    public static Dictionary<BadgeType, List<Key>> ReadBadgeByteRecord(byte[] bytes)
    {
        var memStream = new MemoryStream(bytes);
        using var reader = new BinaryReader(memStream);
        return reader.ReadMap<BadgeType, List<Key>, Dictionary<BadgeType, List<Key>>>(
            reader.ReadByteEnum<BadgeType>, () => reader.ReadList<Key, List<Key>>(Key.ReadRecord));
    }
    
    public static byte[] WriteLoadoutByteRecord(Dictionary<Key, LobbyLoadout> loadouts)
    {
        var memStream = new MemoryStream();
        using var writer = new BinaryWriter(memStream);
        writer.WriteMap(loadouts, Key.WriteRecord, LobbyLoadout.WriteRecord);
        return memStream.ToArray();
    }

    public static Dictionary<Key, LobbyLoadout> ReadLoadoutByteRecord(byte[] bytes)
    {
        var memStream = new MemoryStream(bytes);
        using var reader = new BinaryReader(memStream);
        return reader.ReadMap<Key, LobbyLoadout, Dictionary<Key, LobbyLoadout>>(Key.ReadRecord, LobbyLoadout.ReadRecord);
    }
    
    public static byte[] WriteStatByteRecord(List<HeroStats> stats)
    {
        var memStream = new MemoryStream();
        using var writer = new BinaryWriter(memStream);
        writer.WriteList(stats, BaseTypes.HeroStats.WriteRecord);
        return memStream.ToArray();
    }

    public static List<HeroStats> ReadStatByteRecord(byte[] bytes)
    {
        var memStream = new MemoryStream(bytes);
        using var reader = new BinaryReader(memStream);
        return reader.ReadList<HeroStats, List<HeroStats>>(BaseTypes.HeroStats.ReadRecord);
    }
    
    public static byte[] WriteMatchByteRecord(List<MatchHistoryRecord> matchHistory)
    {
        var memStream = new MemoryStream();
        using var writer = new BinaryWriter(memStream);
        writer.WriteList(matchHistory, MatchHistoryRecord.WriteRecord);
        return memStream.ToArray();
    }

    public static List<MatchHistoryRecord> ReadMatchByteRecord(byte[] bytes)
    {
        var memStream = new MemoryStream(bytes);
        using var reader = new BinaryReader(memStream);
        return reader.ReadList<MatchHistoryRecord, List<MatchHistoryRecord>>(MatchHistoryRecord.ReadRecord);
    }

    public PlayerRecord ToPlayerRecord() =>
        new()
        {
            PlayerId = PlayerId,
            SteamId = SteamId,
            Username = Nickname,
            PlayerRole = Role,
            Region = Region,
            RatingMean = Rating.Mean,
            RatingDeviation = Rating.StandardDeviation,
            LeagueInfo = League.WriteByteRecord(League),
            Progression = PlayerProgression.WriteByteRecord(Progression),
            LookingForFriends = LookingForFriends,
            BadgeInfo = WriteBadgeByteRecord(Badges),
            LastPlayedHero = LastPlayedHero.HasValue ? Databases.Catalogue.GetCard<Card>(LastPlayedHero.Value)?.Id : null,
            LoadoutData = WriteLoadoutByteRecord(HeroLoadouts),
            HeroStats = WriteStatByteRecord(HeroStats),
            MatchHistory = WriteMatchByteRecord(MatchHistory),
            TutorialTokens = TutorialTokens,
            TimeTrialInfo = TimeTrialData.WriteByteRecord(TimeTrial),
            MatchmakerBanEnd = MatchmakerBanEnd.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds((long)MatchmakerBanEnd.Value) : null,
            GraveyardPermanent = GraveyardPermanent,
            GraveyardLeaveTime = GraveyardLeaveTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds((long)GraveyardLeaveTime.Value) : null,
        };

    public static PlayerData FromPlayerRecord(PlayerRecord playerRecord) =>
        new()
        {
            PlayerId = playerRecord.PlayerId,
            SteamId = playerRecord.SteamId,
            Nickname = playerRecord.Username,
            Role = playerRecord.PlayerRole,
            Region = playerRecord.Region,
            Rating = new Rating(playerRecord.RatingMean, playerRecord.RatingDeviation),
            League = League.ReadByteRecord(playerRecord.LeagueInfo),
            Progression = PlayerProgression.ReadByteRecord(playerRecord.Progression),
            LookingForFriends = playerRecord.LookingForFriends,
            Badges = ReadBadgeByteRecord(playerRecord.BadgeInfo),
            LastPlayedHero = playerRecord.LastPlayedHero != null ? Catalogue.Key(playerRecord.LastPlayedHero) : null,
            HeroLoadouts = ReadLoadoutByteRecord(playerRecord.LoadoutData),
            HeroStats = ReadStatByteRecord(playerRecord.HeroStats),
            MatchHistory = ReadMatchByteRecord(playerRecord.MatchHistory),
            TutorialTokens = playerRecord.TutorialTokens,
            TimeTrial = TimeTrialData.ReadByteRecord(playerRecord.TimeTrialInfo),
            MatchmakerBanEnd = (ulong?)playerRecord.MatchmakerBanEnd?.ToUnixTimeMilliseconds(),
            GraveyardPermanent = playerRecord.GraveyardPermanent,
            GraveyardLeaveTime = (ulong?)playerRecord.GraveyardLeaveTime?.ToUnixTimeMilliseconds()
        };
}