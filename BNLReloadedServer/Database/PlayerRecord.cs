using BNLReloadedServer.BaseTypes;
using SQLite;

namespace BNLReloadedServer.Database;

[Table("Users")]
public class PlayerRecord
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]		
    public uint PlayerId { get; set; }
    
    [Column("steam_id")]
    public ulong SteamId { get; set; }
    
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("role")] 
    public PlayerRole PlayerRole { get; set; } = PlayerRole.User;
    
    [Column("region")]
    public string? Region { get; set; }
    
    [Column("rating_mean")]
    public double RatingMean { get; set; } = 25;
    
    [Column("rating_dev")]
    public double RatingDeviation { get; set; } = 25.0 / 3.0;

    [Column("rating_volatility")] 
    public double RatingVolatility { get; set; } = 0.06;
    
    [Column("league_info")]
    public byte[] LeagueInfo { get; set; } = [];
    
    [Column("progression")]
    public byte[] Progression { get; set; } = [];
    
    [Column("looking_for_friends")]
    public bool LookingForFriends { get; set; }
    
    [Column("badge_info")]
    public byte[] BadgeInfo { get; set; } = [];

    [Column("last_played_hero")] 
    public string? LastPlayedHero { get; set; }
    
    [Column("loadout_data")]
    public byte[] LoadoutData { get; set; } = [];
    
    [Column("hero_stats")]
    public byte[] HeroStats { get; set; } = [];
    
    [Column("match_history")]
    public byte[] MatchHistory { get; set; } = [];
    
    [Column("tutorial_tokens")]
    public int TutorialTokens { get; set; }
    
    [Column("time_trial_info")]
    public byte[] TimeTrialInfo { get; set; } = [];
    
    [Column("matchmaker_ban_end")]
    public DateTimeOffset? MatchmakerBanEnd { get; set; }
    
    [Column("graveyard_perma")]
    public bool? GraveyardPermanent { get; set; }
    
    [Column("graveyard_leave")]
    public DateTimeOffset? GraveyardLeaveTime { get; set; }
}