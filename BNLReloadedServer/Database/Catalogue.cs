using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Database;

public abstract class Catalogue
{
    public bool Replicated { get; protected set; }

    public abstract Card? GetCard(Key key);

    public abstract IEnumerable<Card> All { get; }

    public static Key Key(string name) => new(name);

    public T? GetCard<T>(string name) where T : class
    {
        return (object?)GetCard(Key(name)) as T;
    }

    public T? GetCard<T>(Key key) where T : class => (object?) GetCard(key) as T;
}