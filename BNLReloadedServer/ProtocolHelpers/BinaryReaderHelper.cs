using System.Drawing;
using System.Numerics;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public static class BinaryReaderHelper
{
    public static Vector2 ReadVector2(this BinaryReader reader) => 
        new(reader.ReadSingle(), reader.ReadSingle());
    
    public static Vector2s ReadVector2s(this BinaryReader reader) => 
        new(reader.ReadInt16(), reader.ReadInt16());

    public static Vector3 ReadVector3(this BinaryReader reader) => 
        new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    
    public static Vector3s ReadVector3s(this BinaryReader reader) => 
        new(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16());

    public static Quaternion ReadQuaternion(this BinaryReader reader) => 
        new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

    public static Color ReadColor(this BinaryReader reader)
    {
        var r = reader.ReadByte();
        var g = reader.ReadByte();
        var b = reader.ReadByte();
        var a = reader.ReadByte();
        return Color.FromArgb(a, r, g, b);
    }

    public static ColorFloat ReadColorFloat(this BinaryReader reader) => 
        new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    
    public static Glicko ReadGlicko(this BinaryReader reader) => 
        new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

    public static float ReadShortCoord(this BinaryReader reader) => reader.ReadInt16() / 100f;

    public static Vector2 ReadVector2Short(this BinaryReader reader) => 
        new(reader.ReadShortCoord(), reader.ReadShortCoord());

    public static Vector3 ReadVector3Short(this BinaryReader reader) =>
        new(reader.ReadShortCoord(), reader.ReadShortCoord(), reader.ReadShortCoord());
    
    public static float ReadAngle(this BinaryReader reader) => reader.ReadUInt16() / 100f;

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

    public static byte[] ReadBinary(this BinaryReader reader)
    {
        var count = reader.Read7BitEncodedInt();
        return reader.ReadBytes(count);
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

    public static T ReadByteEnum<T>(this BinaryReader reader) where T : Enum => (T) Enum.ToObject(typeof (T), reader.ReadByte());

    public static DateTime ReadDateTime(this BinaryReader reader) => new DateTime(1970, 1, 1).AddSeconds(reader.ReadUInt32());
}