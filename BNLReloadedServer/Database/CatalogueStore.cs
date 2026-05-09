using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ServerTypes;

namespace BNLReloadedServer.Database;

public abstract class CatalogueStore
{
    protected void AddMaps(List<Card> cards, IEnumerable<CardMap> cardMaps, ExtraMaps? extraMaps)
    {
        foreach (var map in cardMaps)
        {
            var exists = false;
            foreach (var (_, idx) in cards.Select((x, idx) => (x, idx))
                         .Where(x => x.x is CardMap && x.x.Id == map.Id).ToList())
            {
                exists = true;
                cards[idx] = map;
            }

            if (!exists)
            {
                cards.Add(map);
            }
        }

        if (extraMaps is not null)
        {
            foreach (var mapList in cards.OfType<CardMapList>())
            {
                if (extraMaps.Custom is { Count: > 0 })
                {
                    if (mapList.Custom is not null)
                    {
                        mapList.Custom.AddRange(extraMaps.Custom);
                    }
                    else
                    {
                        mapList.Custom = extraMaps.Custom;
                    }
                }

                if (extraMaps.Friendly is { Count: > 0 })
                {
                    if (mapList.Friendly is not null)
                    {
                        mapList.Friendly.AddRange(extraMaps.Friendly);
                    }
                    else
                    {
                        mapList.Friendly = extraMaps.Friendly;
                    }
                }

                if (extraMaps.FriendlyNoob is { Count: > 0 })
                {
                    if (mapList.FriendlyNoob is not null)
                    {
                        mapList.FriendlyNoob.AddRange(extraMaps.FriendlyNoob);
                    }
                    else
                    {
                        mapList.FriendlyNoob = extraMaps.FriendlyNoob;
                    }
                }

                if (extraMaps.Ranked is { Count: > 0 })
                {
                    if (mapList.Ranked is not null)
                    {
                        mapList.Ranked.AddRange(extraMaps.Ranked);
                    }
                    else
                    {
                        mapList.Ranked = extraMaps.Ranked;
                    }
                }
            }
        }
            
        foreach (var t in cards)
        {
            t.Key = Catalogue.Key(t.Id ?? string.Empty);
        }
    }
    
    public abstract void Store(IEnumerable<Card> cards);
    public abstract void Load(IEnumerable<CardMap> maps, ExtraMaps? extraMaps);
}