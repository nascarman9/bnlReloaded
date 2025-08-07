using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public struct Key(string id) : IEquatable<Key>
{
    public static readonly Key None = new() { Hash = 0 };

    public uint Hash { get; private set; } = Crc32.GetHash(id);

    public void Read(BinaryReader reader) => Hash = reader.ReadUInt32();

    public static Key ReadRecord(BinaryReader reader)
    {
        return new Key { Hash = reader.ReadUInt32() };
    }

    public void Write(BinaryWriter writer) => writer.Write(Hash);

    public static void WriteRecord(BinaryWriter writer, Key key) => key.Write(writer);

    public override int GetHashCode() => Hash.GetHashCode();

    public override bool Equals(object? obj) => obj is Key key && (int) Hash == (int) key.Hash;

    public T? GetCard<T>() where T : class => Databases.Catalogue.GetCard<T>(this);

    public override string ToString() => Hash.ToString();

    public static bool operator ==(Key a, Key b) => (int) a.Hash == (int) b.Hash;

    public static bool operator !=(Key a, Key b) => (int) a.Hash != (int) b.Hash;

    public bool Equals(Key other)
    {
        return Hash == other.Hash;
    }
}