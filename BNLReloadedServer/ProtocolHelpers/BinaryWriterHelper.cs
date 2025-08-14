using System.Drawing;
using System.Globalization;
using System.Numerics;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public static class BinaryWriterHelper
{
    public static void Write(this BinaryWriter writer, Vector2 vector)
    {
        writer.Write(vector.X);
        writer.Write(vector.Y);
    }

    public static void Write(this BinaryWriter writer, Vector2s vector)
    {
        writer.Write(vector.x);
        writer.Write(vector.y);
    }

    public static void Write(this BinaryWriter writer, Vector3 vector)
    {
        writer.Write(vector.X);
        writer.Write(vector.Y);
        writer.Write(vector.Z);
    }

    public static void Write(this BinaryWriter writer, Vector3s vector)
    {
        writer.Write(vector.x);
        writer.Write(vector.y);
        writer.Write(vector.z);
    }

    public static void Write(this BinaryWriter writer, Quaternion quaternion)
    {
        writer.Write(quaternion.X);
        writer.Write(quaternion.Y);
        writer.Write(quaternion.Z);
        writer.Write(quaternion.W);
    }

    public static void Write(this BinaryWriter writer, Color color)
    {
        writer.Write(color.R);
        writer.Write(color.G);
        writer.Write(color.B);
        writer.Write(color.A);
    }

    public static void Write(this BinaryWriter writer, ColorFloat color)
    {
        writer.Write(color.r);
        writer.Write(color.g);
        writer.Write(color.b);
        writer.Write(color.a);
    }

    public static void Write(this BinaryWriter writer, Glicko glicko)
    {
        writer.Write(glicko.Rating);
        writer.Write(glicko.Deviation);
        writer.Write(glicko.Volatility);
    }
    
    public static void WriteShortCoord(this BinaryWriter writer, float coord)
    {
        writer.Write((short) (coord * 100.0));
    }

    public static void WriteVectorShort(this BinaryWriter writer, Vector2 vector)
    {
        writer.WriteShortCoord(vector.X);
        writer.WriteShortCoord(vector.Y);
    }

    public static void WriteVectorShort(this BinaryWriter writer, Vector3 vector)
    {
        writer.WriteShortCoord(vector.X);
        writer.WriteShortCoord(vector.Y);
        writer.WriteShortCoord(vector.Z);
    }

    public static void WriteAngle(this BinaryWriter writer, float angle)
    {
        writer.Write((ushort) (angle * 100.0));
    }
    
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

    public static void WriteBinary(this BinaryWriter writer, byte[] bytes)
    {
        writer.Write7BitEncodedInt(bytes.Length);     
        writer.Write(bytes);
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

    public static void WriteByteEnum<T>(this BinaryWriter writer, T value) where T : Enum => writer.Write(Convert.ToByte(value, CultureInfo.InvariantCulture));

    public static void WriteDateTime(this BinaryWriter writer, DateTime dateTime) => writer.Write((uint) (dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds);
}