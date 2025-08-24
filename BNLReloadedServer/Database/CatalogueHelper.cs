using BNLReloadedServer.BaseTypes;

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
        return GlobalLogic.Matchmaker?.GameModesForQueues?.Select(gameModesForQueue => Databases.Catalogue.GetCard<CardGameMode>(gameModesForQueue)).FirstOrDefault(card => card?.Ranking == type);
    }

    public static CardMatch? GetMatch(BaseTypes.MatchType type)
    {
        return GetCards<CardMatch>(CardCategory.Match).Find(x => x.Data?.Type == type);
    }

    public static CardGameMode ModeFriendly => Databases.Catalogue.GetCard<CardGameMode>("game_mode_friendly")!;

    public static CardGameMode ModeRanked => Databases.Catalogue.GetCard<CardGameMode>("game_mode_ranked")!;

    public static CardGameMode ModeCustom => Databases.Catalogue.GetCard<CardGameMode>("game_mode_custom")!;

    public static CardGameMode ModeMad => Databases.Catalogue.GetCard<CardGameMode>("game_mode_mad")!;

    public static CardGameMode ModeTutorial => Databases.Catalogue.GetCard<CardGameMode>("game_mode_tutorial")!;

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
    public static T GetCategory<T>(this ShopData shop) where T : ShopCategory
    {
        return shop.Categories!.Find((Predicate<ShopCategory>) (c => c is T)) as T;
    }

    public static List<T> GetCategories<T>(this ShopData shop) where T : ShopCategory
    {
        return shop.Categories!.FindAll((Predicate<ShopCategory>) (c => c is T)).ConvertAll((Converter<ShopCategory, T>) (c => c as T));
    }

    public static ShopItemPromotion? GetPromotion(this CardShopItem item)
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

    public static float? GetRealDiscount(this CardShopItem item)
    {
        var promotion = item.GetPromotion();
        return promotion != null && InTimeInterval(promotion.From, promotion.To) ? promotion.RealDiscount : null;
    }

    public static float? GetVirtualDiscount(this CardShopItem item)
    {
        var promotion = item.GetPromotion();
        return promotion != null && InTimeInterval(promotion.From, promotion.To) ? promotion.VirtualDiscount : null;
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

    public static float PlayerXpForLevel(int level)
    {
        if (level <= 1) 
            return 0.0f;
        var playerXp = GlobalLogic.XpLogic!.PlayerXp!;
        return (float) (playerXp.FlatCoeff + playerXp.MultCoeff * Math.Pow((float) (level - 1), playerXp.PowerCoeff));
    }
    
    public static float HeroXpForLevel(int level)
    {
        if (level <= 1)
            return 0.0f;
        var heroXp = GlobalLogic.XpLogic!.HeroXp;
        return (float) (heroXp!.FlatCoeff + heroXp.MultCoeff * Math.Pow((float) (level - 1), heroXp.PowerCoeff));
    }

    public static float PriceFactorReal(this CardShopItem card)
    {
        var realDiscount = card.GetRealDiscount();
        return realDiscount.HasValue ? 1f - realDiscount.Value : 1f;
    }

    public static float PriceFactorVirtual(this CardShopItem card)
    {
        var virtualDiscount = card.GetVirtualDiscount();
        return virtualDiscount.HasValue ? 1f - virtualDiscount.Value : 1f;
    }

    public static float TotalPriceVirtual(this CardShopItem card)
    {
        return (float) Math.Ceiling(card.PriceVirtual!.Value * card.PriceFactorVirtual());
    }

    public static float TotalPriceReal(this CardShopItem card)
    {
        return (float) Math.Ceiling(card.PriceReal!.Value * card.PriceFactorReal());
    }
}