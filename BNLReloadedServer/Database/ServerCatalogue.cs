using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.Database;

public class ServerCatalogue : Catalogue
{
    private readonly Dictionary<Key, Card> _db = new(KeyEqualityComparer.Instance);

    public ServerCatalogue()
    {
        var cards = CatalogueCache.UpdateCatalogue(CatalogueCache.Load());
        foreach (var card in cards)
        {
            card.Key = Key(card.Id!);
            _db.Add(card.Key, card);
        }
    }
    public override Card GetCard(Key key)
    {
        if (_db.TryGetValue(key, out var card))
            return card;
        throw new Exception("Invalid card id " + key);
    }

    public override IEnumerable<Card> All => _db.Values;

    public void Replicate(List<Card> cards)
    {
        _db.Clear();
        foreach (var card in cards)
        {
            card.Key = Key(card.Id!);
            _db.Add(card.Key, card);
        }
        Replicated = true;
    }

    public void UpdateCard(Card card)
    {
        card.Key = Key(card.Id!);
        _db[card.Key] = card;
    }

    public void RemoveCard(string id)
    {
        _db.Remove(Key(id));
    }
}