using BNLReloadedServer.Database;

namespace BNLReloadedServer.BaseTypes;

public static class BlockCardsCache
{
    private static CardBlock[] cache;

    public static void InitCache()
    {
        var dictionary = new Dictionary<ushort, CardBlock>();
        foreach (var card in Databases.Catalogue.All.Where((Func<Card, bool>) (a => a is CardBlock)))
        {
            var cardBlock = (CardBlock)card;
            dictionary[cardBlock.BlockId] = cardBlock;
        }

        cache = new CardBlock[65536];
        for (var key = 0; key < cache.Length; ++key)
        {
            if (dictionary.TryGetValue((ushort) key, out var cardBlock))
                cache[key] = cardBlock;
        }
    }

    public static CardBlock GetCard(ushort blockId) => cache[blockId];
}