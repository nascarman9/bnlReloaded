using System.Globalization;

namespace BNLReloadedServer.ProtocolHelpers;

public static class BinaryWriterHelper
{
    public static void WriteList<T>(this BinaryWriter writer, ICollection<T> list, Action<T> writeAction)
    {
        writer.Write7BitEncodedInt(list.Count);
        foreach (var item in list)
        {
            writeAction(item);
        }
    }
    
    public static void WriteList<T>(this BinaryWriter writer, ICollection<T> list, Action<BinaryWriter, T> writeAction)
    {
        writer.Write7BitEncodedInt(list.Count);
        foreach (var item in list)
        {
            writeAction(writer, item);
        }
    }

    public static void WriteArray<T>(this BinaryWriter writer, T[] array, Action<T> writeAction)
    {
        writer.Write7BitEncodedInt(array.Length);
        foreach (var item in array)
        {
            writeAction(item);
        }
    }
    
    public static void WriteArray<T>(this BinaryWriter writer, T[] array, Action<BinaryWriter, T> writeAction)
    {
        writer.Write7BitEncodedInt(array.Length);
        foreach (var item in array)
        {
            writeAction(writer, item);
        }
    }

    public static void WriteMap<TKey, TValue>(this BinaryWriter writer, IDictionary<TKey, TValue> map,
        Action<TKey> keyAction, Action<TValue> valueAction)
    {
        writer.Write7BitEncodedInt(map.Count);
        foreach (var keyValuePair in map)
        {
            keyAction(keyValuePair.Key);
            valueAction(keyValuePair.Value);
        }
    }
    
    public static void WriteMap<TKey, TValue>(this BinaryWriter writer, IDictionary<TKey, TValue> map,
        Action<BinaryWriter, TKey> keyAction, Action<TValue> valueAction)
    {
        writer.Write7BitEncodedInt(map.Count);
        foreach (var keyValuePair in map)
        {
            keyAction(writer, keyValuePair.Key);
            valueAction(keyValuePair.Value);
        }
    }
    
    public static void WriteMap<TKey, TValue>(this BinaryWriter writer, IDictionary<TKey, TValue> map,
        Action<TKey> keyAction, Action<BinaryWriter, TValue> valueAction)
    {
        writer.Write7BitEncodedInt(map.Count);
        foreach (var keyValuePair in map)
        {
            keyAction(keyValuePair.Key);
            valueAction(writer, keyValuePair.Value);
        }
    }
    
    public static void WriteMap<TKey, TValue>(this BinaryWriter writer, IDictionary<TKey, TValue> map,
        Action<BinaryWriter, TKey> keyAction, Action<BinaryWriter, TValue> valueAction)
    {
        writer.Write7BitEncodedInt(map.Count);
        foreach (var keyValuePair in map)
        {
            keyAction(writer, keyValuePair.Key);
            valueAction(writer, keyValuePair.Value);
        }
    }

    public static void WriteOption<T>(this BinaryWriter writer, T? value, Action<T> writeAction) where T : class
    {
        writer.Write(value != null);
        if (value == null) return;
        writeAction(value);
    }
    
    public static void WriteOption<T>(this BinaryWriter writer, T? value, Action<BinaryWriter, T> writeAction) where T : class
    {
        writer.Write(value != null);
        if (value == null) return;
        writeAction(writer, value);
    }

    public static void WriteOptionValue<T>(this BinaryWriter writer, T? value, Action<T> writeAction) where T : struct
    {
        writer.Write(value.HasValue);
        if (!value.HasValue) return;
        writeAction(value.Value);
    }
    
    public static void WriteOptionValue<T>(this BinaryWriter writer, T? value, Action<BinaryWriter, T> writeAction) where T : struct
    {
        writer.Write(value.HasValue);
        if (!value.HasValue) return;
        writeAction(writer, value.Value);
    }

    public static void WriteByteEnum<T>(this BinaryWriter writer, T value) where T : Enum
    {
        writer.Write(Convert.ToByte(value, CultureInfo.InvariantCulture));
    }
}