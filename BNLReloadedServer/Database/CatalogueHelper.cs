using BNLReloadedServer.BaseTypes;
using MatchType = BNLReloadedServer.BaseTypes.MatchType;

namespace BNLReloadedServer.Database;

public static class CatalogueHelper
{
    public const string ModeNameRanked = "RANKED";
    public const string ModeNameFriendly = "CASUAL";
    public const string ModeNameAblockalypse = "ABLOCKALYPSE";
    public const string ModeNameGraveyard = "GRAVEYARD";

    public static CardMapLogic MapLogic => Databases.Catalogue.GetCard<CardMapLogic>("map_logic")!;

    public static CardMapList MapList => Databases.Catalogue.GetCard<CardMapList>("map_list")!;

    public static CardGlobalLogic GlobalLogic => Databases.Catalogue.GetCard<CardGlobalLogic>("global_logic")!;

    public static CardChatLogic ChatLogic => Databases.Catalogue.GetCard<CardChatLogic>("chat_logic")!;

    public static CardSettingsLogic SettingsLogic => Databases.Catalogue.GetCard<CardSettingsLogic>("settings_logic")!;

    public static CardMovementLogic MovementLogic => Databases.Catalogue.GetCard<CardMovementLogic>("movement_logic")!;

    public static CardShopLogic ShopLogic => Databases.Catalogue.GetCard<CardShopLogic>("shop_logic")!;

    public static CardRewardsLogic RewardsLogic => Databases.Catalogue.GetCard<CardRewardsLogic>("rewards_logic")!;
    
    public static TimeTrialLogic TimeTrialLogic => GlobalLogic.TimeTrial!;

    public static List<T> GetCards<T>(CardCategory category) where T : Card
    {
        return Databases.Catalogue.All.Where(x => x.Category == category).Select(x => (T) x).ToList();
    }

    public static CardGameMode? GetMode(GameRankingType type)
    {
        return GlobalLogic.Matchmaker?.GameModesForQueues
            ?.Select(gameModesForQueue => Databases.Catalogue.GetCard<CardGameMode>(gameModesForQueue))
            .FirstOrDefault(card => card?.Ranking == type);
    }

    public static CardMatch? GetMatch(MatchType type, Key gameMode)
    {
        if (type != MatchType.ShieldRush2)
            return GetCards<CardMatch>(CardCategory.Match).Find(x => x.Data?.Type == type);
        
        var madMode = ModeMad;
        var rankedMode = ModeRanked;
        if (gameMode == rankedMode.Key)
        {
            return rankedMode.MatchMode.GetCard<CardMatch>();
        }

        if (gameMode == madMode.Key)
        {
            return madMode.MatchMode.GetCard<CardMatch>();
        }

        return ModeFriendly.MatchMode.GetCard<CardMatch>();
    }

    public static CardGameMode ModeFriendly => Databases.Catalogue.GetCard<CardGameMode>("game_mode_friendly")!;

    public static CardGameMode ModeRanked => Databases.Catalogue.GetCard<CardGameMode>("game_mode_ranked")!;

    public static CardGameMode ModeCustom => Databases.Catalogue.GetCard<CardGameMode>("game_mode_custom")!;

    public static CardGameMode ModeMad => Databases.Catalogue.GetCard<CardGameMode>("game_mode_mad")!;

    public static CardGameMode ModeTutorial => Databases.Catalogue.GetCard<CardGameMode>("game_mode_tutorial")!;
    public static Key BrawnClassKey { get; } = new("hero_class_brawn");
    public static Key SkillsClassKey { get; } = new("hero_class_skills");
    public static Key BrainsClassKey { get; } = new("hero_class_brains");
    public static Key DefaultSource { get; } = new("damage_source_default");
    public static Key FallSource { get; } = new("damage_source_fall");
    public static Key LavaSource { get; } = new("damage_source_lava");
    public static Key AcidSource { get; } = new("damage_source_acid");
    public static Key AntimatterSource { get; } = new("damage_source_antimatter_shield");
    public static Key SmokeBomb { get; } = new("unit_device_generic_smoketrap");
    public static Key FallImpact { get; } = new("impact_falling");
    public static Key SupplyDrop { get; } = new("unit_supply_resource");
    public static Key SuperSupplyDrop { get; } = new("unit_supply_super_resource");
    public static Key ClassicBlockbuster { get; } = new("unit_supply_blockbuster_classic");
    public static Key SpecialBadge { get; } = new("badge_icon_community_representative");
    public static Key AntimatterShieldImpact { get; } = new("impact_antimatter_shield");

    public static readonly List<Key> ObjectiveShieldKeys =
    [
        new("effect_shield_for_line_base"),
        new("effect_shield_for_line_2"),
        new("effect_shield_for_line_3")
    ];

    public static readonly List<Key> GasGrenadeKeys =
    [
        new("gear_doc_eliza_chem_grenade"),
        new("gear_doc_eliza_chem_grenade_perk_homebrewed_chemicals"),
        new("gear_doc_eliza_chem_grenade_perk_beautiful_bubbles"),
        new("gear_doc_eliza_chem_grenade_splashing_damage_1"),
        new("gear_doc_eliza_chem_grenade_splashing_damage_2"),
        new("gear_doc_eliza_chem_grenade_splashing_damage_3"),
        new("gear_doc_eliza_chem_grenade_perk_splashing_damage"),
        new("gear_doc_eliza_chem_grenade_beautiful_bubbles_1"),
        new("gear_doc_eliza_chem_grenade_beautiful_bubbles_2"),
        new("gear_doc_eliza_chem_grenade_beautiful_bubbles_3")
    ];

    public static readonly List<Key> NerveGasKeys =
    [
        new("gear_trondson_kobold_lamps_nerve_gas")
    ];

    public static IEnumerable<Key> GetHeroes() => GlobalLogic.AvailableHeroes ?? [];

    public static Dictionary<int, Key>? GetDefaultDevices(Key heroKey)
    {
        if (Databases.Catalogue.GetCard<CardUnit>(heroKey)?.Data is not UnitDataPlayer heroData) return null;
        if (heroData.DefaultDevices == null || heroData.SpecialDevices == null || heroData.SpecialDevices.Count == 0 || heroData.DefaultDevices.Count != 5) return null;
        return new Dictionary<int, Key>
        {
            { 1, heroData.SpecialDevices[0] },
            { 2, heroData.DefaultDevices[0] },
            { 3, heroData.DefaultDevices[1] },
            { 4, heroData.DefaultDevices[2] },
            { 5, heroData.DefaultDevices[3] },
            { 6, heroData.DefaultDevices[4] }
        };
    }
    extension(ShopData shop)
    {
        public T GetCategory<T>() where T : ShopCategory
        {
            return shop.Categories.Find((Predicate<ShopCategory>) (c => c is T)) as T;
        }

        public List<T> GetCategories<T>() where T : ShopCategory
        {
            return shop.Categories.FindAll((Predicate<ShopCategory>) (c => c is T)).ConvertAll((Converter<ShopCategory, T>) (c => c as T));
        }
    }

    extension(CardShopItem item)
    {
        public ShopItemPromotion? GetPromotion()
        {
            var promotion = item.Promotion;
            if (promotion != null) return promotion;
            foreach (var category in ShopLogic.Shop!.Categories)
            {
                if (promotion != null) continue;
                var shopCategoryBundles = category as ShopCategoryBundles;
                var shopItemPromotion = category is not ShopCategoryCommon shopCategoryCommon ? shopCategoryBundles?.Promotion : shopCategoryCommon.Promotion;
                if (shopItemPromotion == null) continue;
                if (category.Items!.Any(key => key == item.Key))
                {
                    promotion = shopItemPromotion;
                }
            }
            return promotion;
        }

        public float? GetRealDiscount()
        {
            var promotion = item.GetPromotion();
            return promotion != null && InTimeInterval(promotion.From, promotion.To) ? promotion.RealDiscount : null;
        }

        public float? GetVirtualDiscount()
        {
            var promotion = item.GetPromotion();
            return promotion != null && InTimeInterval(promotion.From, promotion.To) ? promotion.VirtualDiscount : null;
        }
    }

    private static bool InTimeInterval(DateTime? from, DateTime? to)
    {
        if (from.HasValue && (DateTime.Now - from.Value).TotalSeconds <= 0.0)
            return false;
        return !to.HasValue || (to.Value - DateTime.Now).TotalSeconds > 0.0;
    }

    public static bool InTime(this InventoryItem item)
    {
        return !item.EndTime.HasValue || (long) item.EndTime.Value - DateTimeOffset.Now.ToUnixTimeMilliseconds() > 0.0;
    }

    public static List<Key> GetSkinsInShop(Key hero)
    {
        var data = hero.GetCard<CardUnit>()!.Data as UnitDataPlayer;
        var skinsInShop = new List<Key>();
        foreach (var key2 in from category in ShopLogic.Shop!.Categories from key1 in category.Items from key2 in key1.GetCard<CardShopItem>()!.Items where key2.GetCard<CardSkin>() != null && data!.Skins.Contains(key2) && !skinsInShop.Contains(key2) select key2)
        {
            skinsInShop.Add(key2);
        }
        return skinsInShop;
    }
    
    public static PlayerProgression GetDefaultProgression() => 
        new()
        {
            PlayerProgress = new XpInfo
            {
                Level = 6,
                LevelXp = 0f,
                XpForNextLevel = PlayerXpForLevel(7)
            },
            HeroesProgress = GetHeroes().ToDictionary(k => k, _ => new XpInfo
            {
                Level = 15,
                LevelXp = 0f,
                XpForNextLevel = HeroXpForLevel(16)
            })
        };

    public static League GetDefaultLeague() =>
        new()
        {
            Tier = 0,
            Division = 0,
            Points = 0,
            JoinedTime = DateTime.UtcNow,
            LastPlayedTime = DateTime.UtcNow,
            Status = null
        };

    public static float PlayerXpForLevel(int level)
    {
        if (level <= 1) 
            return 0.0f;
        var playerXp = GlobalLogic.XpLogic?.PlayerXp;
        return (float) (playerXp.FlatCoeff + playerXp.MultCoeff * Math.Pow((float) (level - 1), playerXp.PowerCoeff));
    }
    
    public static float HeroXpForLevel(int level)
    {
        if (level <= 1)
            return 0.0f;
        var heroXp = GlobalLogic.XpLogic?.HeroXp;
        return (float) (heroXp.FlatCoeff + heroXp.MultCoeff * Math.Pow((float) (level - 1), heroXp.PowerCoeff));
    }

    public static XpInfo LeveLUp(XpInfo xpInfo, float xpAmount)
    {
        var xp = xpAmount;
        var newXp = new XpInfo
        {
            Level = xpInfo.Level,
            LevelXp = xpInfo.LevelXp,
            XpForNextLevel = xpInfo.XpForNextLevel
        };
        
        while (newXp.LevelXp + xp >= newXp.XpForNextLevel)
        {
            xp -= newXp.XpForNextLevel - newXp.LevelXp;
            newXp.Level += 1;
            newXp.LevelXp = 0;
            newXp.XpForNextLevel = PlayerXpForLevel(newXp.Level);
        }

        newXp.LevelXp += xp;
        return newXp;
    }
    
    public static XpInfo LeveLUpHero(XpInfo xpInfo, float xpAmount)
    {
        var xp = xpAmount;
        var newXp = new XpInfo
        {
            Level = xpInfo.Level,
            LevelXp = xpInfo.LevelXp,
            XpForNextLevel = xpInfo.XpForNextLevel
        };
        
        while (newXp.LevelXp + xp >= newXp.XpForNextLevel)
        {
            xp -= newXp.XpForNextLevel - newXp.LevelXp;
            newXp.Level += 1;
            newXp.LevelXp = 0;
            newXp.XpForNextLevel = HeroXpForLevel(newXp.Level);
        }
        
        newXp.LevelXp += xp;
        return newXp;
    }

    extension(CardShopItem card)
    {
        public float PriceFactorReal()
        {
            var realDiscount = card.GetRealDiscount();
            return realDiscount.HasValue ? 1f - realDiscount.Value : 1f;
        }

        public float PriceFactorVirtual()
        {
            var virtualDiscount = card.GetVirtualDiscount();
            return virtualDiscount.HasValue ? 1f - virtualDiscount.Value : 1f;
        }

        public float TotalPriceVirtual()
        {
            return (float) Math.Ceiling(card.PriceVirtual!.Value * card.PriceFactorVirtual());
        }

        public float TotalPriceReal()
        {
            return (float) Math.Ceiling(card.PriceReal!.Value * card.PriceFactorReal());
        }
    }
}