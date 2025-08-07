namespace BNLReloadedServer.ProtocolHelpers;

public static class BinaryReaderHelper
{
    public static TList ReadList<T, TList>(this BinaryReader reader, Func<T> readFunc) where TList : ICollection<T>, new()
    {
        var count = reader.Read7BitEncodedInt();
        var items = new TList();
        for (var i = 0; i < count; i++)
        {
            items.Add(readFunc());
        }

        return items;
    }
    
    public static TList ReadList<T, TList>(this BinaryReader reader, Func<BinaryReader, T> readFunc) where TList : ICollection<T>, new()
    {
        var count = reader.Read7BitEncodedInt();
        var items = new TList();
        for (var i = 0; i < count; i++)
        {
            items.Add(readFunc(reader));
        }

        return items;
    }

    public static T[] ReadArray<T>(this BinaryReader reader, Func<T> readFunc)
    {
        var count = reader.Read7BitEncodedInt();
        var items = new T[count];
        for (var i = 0; i < count; i++)
        {
            items[i] = readFunc();
        }

        return items;
    }
    
    public static T[] ReadArray<T>(this BinaryReader reader, Func<BinaryReader, T> readFunc)
    {
        var count = reader.Read7BitEncodedInt();
        var items = new T[count];
        for (var i = 0; i < count; i++)
        {
            items[i] = readFunc(reader);
        }

        return items;
    }

    public static TDict ReadMap<TKey, TValue, TDict>(this BinaryReader reader, Func<TKey> keyFunc, Func<TValue> valueFunc)
        where TDict : IDictionary<TKey, TValue>, new()
    {
        var count = reader.Read7BitEncodedInt();
        var items = new TDict();
        for (var i = 0; i < count; i++)
        {
            items.Add(keyFunc(), valueFunc());
        }

        return items;
    }
    
    public static TDict ReadMap<TKey, TValue, TDict>(this BinaryReader reader, Func<BinaryReader, TKey> keyFunc, Func<TValue> valueFunc)
        where TDict : IDictionary<TKey, TValue>, new()
    {
        var count = reader.Read7BitEncodedInt();
        var items = new TDict();
        for (var i = 0; i < count; i++)
        {
            items.Add(keyFunc(reader), valueFunc());
        }

        return items;
    }
    
    public static TDict ReadMap<TKey, TValue, TDict>(this BinaryReader reader, Func<TKey> keyFunc, Func<BinaryReader, TValue> valueFunc)
        where TDict : IDictionary<TKey, TValue>, new()
    {
        var count = reader.Read7BitEncodedInt();
        var items = new TDict();
        for (var i = 0; i < count; i++)
        {
            items.Add(keyFunc(), valueFunc(reader));
        }

        return items;
    }
    
    public static TDict ReadMap<TKey, TValue, TDict>(this BinaryReader reader, Func<BinaryReader, TKey> keyFunc, Func<BinaryReader, TValue> valueFunc)
        where TDict : IDictionary<TKey, TValue>, new()
    {
        var count = reader.Read7BitEncodedInt();
        var items = new TDict();
        for (var i = 0; i < count; i++)
        {
            items.Add(keyFunc(reader), valueFunc(reader));
        }

        return items;
    }

    public static T? ReadOption<T>(this BinaryReader reader, Func<T> readFunc) where T : class
    {
        var hasValue = reader.ReadBoolean();
        return !hasValue ? null : readFunc();
    }
    
    public static T? ReadOption<T>(this BinaryReader reader, Func<BinaryReader, T> readFunc) where T : class
    {
        var hasValue = reader.ReadBoolean();
        return !hasValue ? null : readFunc(reader);
    }

    public static T? ReadOptionValue<T>(this BinaryReader reader, Func<T> readFunc) where T : struct
    {
        var hasValue = reader.ReadBoolean();
        return !hasValue ? null : readFunc();
    }
    
    public static T? ReadOptionValue<T>(this BinaryReader reader, Func<BinaryReader, T> readFunc) where T : struct
    {
        var hasValue = reader.ReadBoolean();
        return !hasValue ? null : readFunc(reader);
    }

    public static T ReadByteEnum<T>(this BinaryReader reader) where T : Enum
    {
        return (T) Enum.ToObject(typeof (T), reader.ReadByte());
    }
}