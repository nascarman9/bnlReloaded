using System.Drawing;
using System.Globalization;
using System.Numerics;
using BNLReloadedServer.BaseTypes;
using Moserware.Skills;

namespace BNLReloadedServer.ProtocolHelpers;

public static class BinaryWriterHelper
{
    extension(BinaryWriter writer)
    {
        public void Write(Vector2 vector)
        {
            writer.Write(vector.X);
            writer.Write(vector.Y);
        }

        public void Write(Vector2s vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
        }

        public void Write(Vector3 vector)
        {
            writer.Write(vector.X);
            writer.Write(vector.Y);
            writer.Write(vector.Z);
        }

        public void Write(Vector3s vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
        }

        public void Write(Quaternion quaternion)
        {
            writer.Write(quaternion.X);
            writer.Write(quaternion.Y);
            writer.Write(quaternion.Z);
            writer.Write(quaternion.W);
        }

        public void Write(Color color)
        {
            writer.Write(color.R);
            writer.Write(color.G);
            writer.Write(color.B);
            writer.Write(color.A);
        }

        public void Write(ColorFloat color)
        {
            writer.Write(color.r);
            writer.Write(color.g);
            writer.Write(color.b);
            writer.Write(color.a);
        }

        public void Write(Glicko glicko)
        {
            writer.Write(glicko.Rating);
            writer.Write(glicko.Deviation);
            writer.Write(glicko.Volatility);
        }

        public void Write(Rating rating)
        {
            writer.Write(rating.Mean);
            writer.Write(rating.StandardDeviation);
        }

        public void WriteShortCoord(float coord)
        {
            writer.Write((short) (coord * 100.0));
        }

        public void WriteVectorShort(Vector2 vector)
        {
            writer.WriteShortCoord(vector.X);
            writer.WriteShortCoord(vector.Y);
        }

        public void WriteVectorShort(Vector3 vector)
        {
            writer.WriteShortCoord(vector.X);
            writer.WriteShortCoord(vector.Y);
            writer.WriteShortCoord(vector.Z);
        }

        public void WriteAngle(float angle)
        {
            writer.Write((ushort) (angle * 100.0));
        }

        public void WriteList<T>(ICollection<T> list, Action<T> writeAction)
        {
            writer.Write7BitEncodedInt(list.Count);
            foreach (var item in list)
            {
                writeAction(item);
            }
        }

        public void WriteList<T>(ICollection<T> list, Action<BinaryWriter, T> writeAction)
        {
            writer.Write7BitEncodedInt(list.Count);
            foreach (var item in list)
            {
                writeAction(writer, item);
            }
        }

        public void WriteArray<T>(T[] array, Action<T> writeAction)
        {
            writer.Write7BitEncodedInt(array.Length);
            foreach (var item in array)
            {
                writeAction(item);
            }
        }

        public void WriteArray<T>(T[] array, Action<BinaryWriter, T> writeAction)
        {
            writer.Write7BitEncodedInt(array.Length);
            foreach (var item in array)
            {
                writeAction(writer, item);
            }
        }

        public void WriteBinary(byte[] bytes)
        {
            writer.Write7BitEncodedInt(bytes.Length);     
            writer.Write(bytes);
        }

        public void WriteMap<TKey, TValue>(IDictionary<TKey, TValue> map,
            Action<TKey> keyAction, Action<TValue> valueAction)
        {
            writer.Write7BitEncodedInt(map.Count);
            foreach (var keyValuePair in map)
            {
                keyAction(keyValuePair.Key);
                valueAction(keyValuePair.Value);
            }
        }

        public void WriteMap<TKey, TValue>(IDictionary<TKey, TValue> map,
            Action<BinaryWriter, TKey> keyAction, Action<TValue> valueAction)
        {
            writer.Write7BitEncodedInt(map.Count);
            foreach (var keyValuePair in map)
            {
                keyAction(writer, keyValuePair.Key);
                valueAction(keyValuePair.Value);
            }
        }

        public void WriteMap<TKey, TValue>(IDictionary<TKey, TValue> map,
            Action<TKey> keyAction, Action<BinaryWriter, TValue> valueAction)
        {
            writer.Write7BitEncodedInt(map.Count);
            foreach (var keyValuePair in map)
            {
                keyAction(keyValuePair.Key);
                valueAction(writer, keyValuePair.Value);
            }
        }

        public void WriteMap<TKey, TValue>(IDictionary<TKey, TValue> map,
            Action<BinaryWriter, TKey> keyAction, Action<BinaryWriter, TValue> valueAction)
        {
            writer.Write7BitEncodedInt(map.Count);
            foreach (var keyValuePair in map)
            {
                keyAction(writer, keyValuePair.Key);
                valueAction(writer, keyValuePair.Value);
            }
        }

        public void WriteOption<T>(T? value, Action<T> writeAction) where T : class
        {
            writer.Write(value != null);
            if (value == null) return;
            writeAction(value);
        }

        public void WriteOption<T>(T? value, Action<BinaryWriter, T> writeAction) where T : class
        {
            writer.Write(value != null);
            if (value == null) return;
            writeAction(writer, value);
        }

        public void WriteOptionValue<T>(T? value, Action<T> writeAction) where T : struct
        {
            writer.Write(value.HasValue);
            if (!value.HasValue) return;
            writeAction(value.Value);
        }

        public void WriteOptionValue<T>(T? value, Action<BinaryWriter, T> writeAction) where T : struct
        {
            writer.Write(value.HasValue);
            if (!value.HasValue) return;
            writeAction(writer, value.Value);
        }

        public void WriteByteEnum<T>(T value) where T : Enum =>
            writer.Write(Convert.ToByte(value, CultureInfo.InvariantCulture));
        public void WriteDateTime(DateTime dateTime) =>
            writer.Write((uint)(dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds);
    }
}