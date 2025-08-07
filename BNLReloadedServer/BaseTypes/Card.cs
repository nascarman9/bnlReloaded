using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public abstract class Card
{
  [JsonIgnore]
    public Key Key { get; set; }

    public abstract CardCategory Category { get; }

    public string? Id { get; set; }

    public ScopeType Scope { get; set; } = ScopeType.Public;

    public abstract void FromJsonData(JsonNode json);

    public abstract JsonNode ToJsonData();

    public static Card CreateFromJson(JsonNode json)
    {
      var fromJson = Create(json["category"].Deserialize<CardCategory>());
      fromJson.FromJsonData(json);
      return fromJson;
    }

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
      switch (category)
      {
        /*case CardCategory.Block:
          return (Card) new CardBlock();
        case CardCategory.Map:
          return (Card) new CardMap();
        case CardCategory.MapData:
          return (Card) new CardMapData();
        case CardCategory.Match:
          return (Card) new CardMatch();
        case CardCategory.GameMode:
          return (Card) new CardGameMode();
        case CardCategory.GlobalLogic:
          return (Card) new CardGlobalLogic();
        case CardCategory.ChatLogic:
          return (Card) new CardChatLogic();
        case CardCategory.SettingsLogic:
          return (Card) new CardSettingsLogic();
        case CardCategory.MovementLogic:
          return (Card) new CardMovementLogic();
        case CardCategory.LeagueLogic:
          return (Card) new CardLeagueLogic();
        case CardCategory.VibrationLogic:
          return (Card) new CardVibrationLogic();
        case CardCategory.ShopLogic:
          return (Card) new CardShopLogic();
        case CardCategory.RewardsLogic:
          return (Card) new CardRewardsLogic();
        case CardCategory.MapLogic:
          return (Card) new CardMapLogic();
        case CardCategory.MapList:
          return (Card) new CardMapList();
        case CardCategory.Gear:
          return (Card) new CardGear();
        case CardCategory.Device:
          return (Card) new CardDevice();
        case CardCategory.DeviceGroup:
          return (Card) new CardDeviceGroup();
        case CardCategory.Unit:
          return (Card) new CardUnit();
        case CardCategory.Projectile:
          return (Card) new CardProjectile();
        case CardCategory.Effect:
          return (Card) new CardEffect();
        case CardCategory.DamageSource:
          return (Card) new CardDamageSource();
        case CardCategory.Material:
          return (Card) new CardMaterial();
        case CardCategory.Impact:
          return (Card) new CardImpact();
        case CardCategory.HeroClass:
          return (Card) new CardHeroClass();
        case CardCategory.Perk:
          return (Card) new CardPerk();
        case CardCategory.Ability:
          return (Card) new CardAbility();
        case CardCategory.MatchMedal:
          return (Card) new CardMatchMedal();
        case CardCategory.Badge:
          return (Card) new CardBadge();
        case CardCategory.Skin:
          return (Card) new CardSkin();
        case CardCategory.Achievement:
          return (Card) new CardAchievement();
        case CardCategory.Challenge:
          return (Card) new CardChallenge();
        case CardCategory.Notification:
          return (Card) new CardNotification();
        case CardCategory.Booster:
          return (Card) new CardBooster();
        case CardCategory.ShopItem:
          return (Card) new CardShopItem();
        case CardCategory.SteamShopItem:
          return (Card) new CardSteamShopItem();
        case CardCategory.SteamDlcItem:
          return (Card) new CardSteamDlcItem();
        case CardCategory.Reward:
          return (Card) new CardReward();
        case CardCategory.Rubble:
          return (Card) new CardRubble();
        case CardCategory.LootCrate:
          return (Card) new CardLootCrate();
        case CardCategory.Strings:
          return (Card) new CardStrings();*/
        default:
          throw new ArgumentOutOfRangeException(nameof (category), category, "Invalid variant tag");
      }
    }
}