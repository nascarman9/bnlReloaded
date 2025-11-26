using System.Collections.Frozen;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.Database;

public class ServerCatalogue : Catalogue
{
    private FrozenDictionary<Key, Card> _db;

    public ServerCatalogue()
    {
        var cards = CatalogueCache.UpdateCatalogue(CatalogueCache.Load());
        var tempDict = new Dictionary<Key, Card>(KeyEqualityComparer.Instance);
        foreach (var card in cards)
        {
            if (card.Id == null) continue;
            card.Key = Key(card.Id);
            tempDict.Add(card.Key, card);
        }
        
        _db = tempDict.ToFrozenDictionary();
    }
    
    public override Card? GetCard(Key key)
    {
        return _db.GetValueOrDefault(key);
    }

    public override IEnumerable<Card> All => _db.Values;

    public void Replicate(List<Card> cards)
    {
        var tempDict = new Dictionary<Key, Card>(KeyEqualityComparer.Instance);
        foreach (var card in cards)
        {
            if (card.Id == null) continue;
            card.Key = Key(card.Id);
            tempDict.Add(card.Key, card);
        }
        _db = tempDict.ToFrozenDictionary();
        Replicated = true;
    }

    public void UpdateCard(Card card)
    {
        if (card.Id == null) return;
        var tempDict = new Dictionary<Key, Card>(_db, KeyEqualityComparer.Instance);
        card.Key = Key(card.Id);
        tempDict[card.Key] = card;
        _db = tempDict.ToFrozenDictionary();
    }

    public void RemoveCard(string id)
    {
        var tempDict = new Dictionary<Key, Card>(_db, KeyEqualityComparer.Instance);
        tempDict.Remove(Key(id));
        _db = tempDict.ToFrozenDictionary();
    }
}