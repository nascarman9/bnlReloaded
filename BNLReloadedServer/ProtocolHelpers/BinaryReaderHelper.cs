using System.Drawing;
using System.Numerics;
using BNLReloadedServer.BaseTypes;
using Moserware.Skills;

namespace BNLReloadedServer.ProtocolHelpers;

public static class BinaryReaderHelper
{
    extension(BinaryReader reader)
    {
        public Vector2 ReadVector2() => 
            new(reader.ReadSingle(), reader.ReadSingle());

        public Vector2s ReadVector2s() => 
            new(reader.ReadInt16(), reader.ReadInt16());

        public Vector3 ReadVector3() => 
            new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        public Vector3s ReadVector3s() => 
            new(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16());

        public Quaternion ReadQuaternion() => 
            new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        public Color ReadColor()
        {
            var r = reader.ReadByte();
            var g = reader.ReadByte();
            var b = reader.ReadByte();
            var a = reader.ReadByte();
            return Color.FromArgb(a, r, g, b);
        }

        public ColorFloat ReadColorFloat() => 
            new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        public Glicko ReadGlicko() => 
            new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        
        public Rating ReadRating() =>
            new(reader.ReadDouble(), reader.ReadDouble());

        public float ReadShortCoord() => reader.ReadInt16() / 100f;

        public Vector2 ReadVector2Short() => 
            new(reader.ReadShortCoord(), reader.ReadShortCoord());

        public Vector3 ReadVector3Short() =>
            new(reader.ReadShortCoord(), reader.ReadShortCoord(), reader.ReadShortCoord());

        public float ReadAngle() => reader.ReadUInt16() / 100f;

        public TList ReadList<T, TList>(Func<T> readFunc) where TList : ICollection<T>, new()
        {
            var count = reader.Read7BitEncodedInt();
            var items = new TList();
            for (var i = 0; i < count; i++)
            {
                items.Add(readFunc());
            }

            return items;
        }

        public TList ReadList<T, TList>(Func<BinaryReader, T> readFunc) where TList : ICollection<T>, new()
        {
            var count = reader.Read7BitEncodedInt();
            var items = new TList();
            for (var i = 0; i < count; i++)
            {
                items.Add(readFunc(reader));
            }

            return items;
        }

        public T[] ReadArray<T>(Func<T> readFunc)
        {
            var count = reader.Read7BitEncodedInt();
            var items = new T[count];
            for (var i = 0; i < count; i++)
            {
                items[i] = readFunc();
            }

            return items;
        }

        public T[] ReadArray<T>(Func<BinaryReader, T> readFunc)
        {
            var count = reader.Read7BitEncodedInt();
            var items = new T[count];
            for (var i = 0; i < count; i++)
            {
                items[i] = readFunc(reader);
            }

            return items;
        }

        public byte[] ReadBinary()
        {
            var count = reader.Read7BitEncodedInt();
            return reader.ReadBytes(count);
        }

        public TDict ReadMap<TKey, TValue, TDict>(Func<TKey> keyFunc, Func<TValue> valueFunc)
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

        public TDict ReadMap<TKey, TValue, TDict>(Func<BinaryReader, TKey> keyFunc, Func<TValue> valueFunc)
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

        public TDict ReadMap<TKey, TValue, TDict>(Func<TKey> keyFunc, Func<BinaryReader, TValue> valueFunc)
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

        public TDict ReadMap<TKey, TValue, TDict>(Func<BinaryReader, TKey> keyFunc, Func<BinaryReader, TValue> valueFunc)
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

        public T? ReadOption<T>(Func<T> readFunc) where T : class
        {
            var hasValue = reader.ReadBoolean();
            return !hasValue ? null : readFunc();
        }

        public T? ReadOption<T>(Func<BinaryReader, T> readFunc) where T : class
        {
            var hasValue = reader.ReadBoolean();
            return !hasValue ? null : readFunc(reader);
        }

        public T? ReadOptionValue<T>(Func<T> readFunc) where T : struct
        {
            var hasValue = reader.ReadBoolean();
            return !hasValue ? null : readFunc();
        }

        public T? ReadOptionValue<T>(Func<BinaryReader, T> readFunc) where T : struct
        {
            var hasValue = reader.ReadBoolean();
            return !hasValue ? null : readFunc(reader);
        }

        public T ReadByteEnum<T>() where T : Enum => (T) Enum.ToObject(typeof (T), reader.ReadByte());
        public DateTime ReadDateTime() => new DateTime(1970, 1, 1).AddSeconds(reader.ReadUInt32());
    }
}