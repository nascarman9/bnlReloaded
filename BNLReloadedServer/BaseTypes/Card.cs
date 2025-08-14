using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<Card>))]
public abstract class Card : IJsonFactory<Card>
{
    [JsonIgnore]
    public Key Key { get; set; }
    
    public abstract CardCategory Category { get; }

    [JsonPropertyOrder(-2)]
    [JsonPropertyName("_id")]
    public string? Id { get; set; }

    [JsonPropertyOrder(-1)]
    public ScopeType Scope { get; set; } = ScopeType.Public;

    public static Card CreateFromJson(JsonElement json) => Create(json.GetProperty("category").Deserialize<CardCategory>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, Card value)
    {
      writer.WriteByteEnum(value.Category);
      value.Write(writer);
    }

    public static Card ReadVariant(BinaryReader reader)
    {
      var card = Create(reader.ReadByteEnum<CardCategory>());
      card.Read(reader);
      return card;
    }

    public static Card Create(CardCategory category)
    {
      return category switch
      {
        CardCategory.Block => new CardBlock(),
        CardCategory.Map => new CardMap(),
        CardCategory.MapData => new CardMapData(),
        CardCategory.Match => new CardMatch(),
        CardCategory.GameMode => new CardGameMode(),
        CardCategory.GlobalLogic => new CardGlobalLogic(),
        CardCategory.ChatLogic => new CardChatLogic(),
        CardCategory.SettingsLogic => new CardSettingsLogic(),
        CardCategory.MovementLogic => new CardMovementLogic(),
        CardCategory.LeagueLogic => new CardLeagueLogic(),
        CardCategory.VibrationLogic => new CardVibrationLogic(),
        CardCategory.ShopLogic => new CardShopLogic(),
        CardCategory.RewardsLogic => new CardRewardsLogic(),
        CardCategory.MapLogic => new CardMapLogic(),
        CardCategory.MapList => new CardMapList(),
        CardCategory.Gear => new CardGear(),
        CardCategory.Device => new CardDevice(),
        CardCategory.DeviceGroup => new CardDeviceGroup(),
        CardCategory.Unit => new CardUnit(),
        CardCategory.Projectile => new CardProjectile(),
        CardCategory.Effect => new CardEffect(),
        CardCategory.DamageSource => new CardDamageSource(),
        CardCategory.Material => new CardMaterial(),
        CardCategory.Impact => new CardImpact(),
        CardCategory.HeroClass => new CardHeroClass(),
        CardCategory.Perk => new CardPerk(),
        CardCategory.Ability => new CardAbility(),
        CardCategory.MatchMedal => new CardMatchMedal(),
        CardCategory.Badge => new CardBadge(),
        CardCategory.Skin => new CardSkin(),
        CardCategory.Achievement => new CardAchievement(),
        CardCategory.Challenge => new CardChallenge(),
        CardCategory.Notification => new CardNotification(),
        CardCategory.Booster => new CardBooster(),
        CardCategory.ShopItem => new CardShopItem(),
        CardCategory.SteamShopItem => new CardSteamShopItem(),
        CardCategory.SteamDlcItem => new CardSteamDlcItem(),
        CardCategory.Reward => new CardReward(),
        CardCategory.Rubble => new CardRubble(),
        CardCategory.LootCrate => new CardLootCrate(),
        CardCategory.Strings => new CardStrings(),
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Invalid variant tag")
      };
    }
}