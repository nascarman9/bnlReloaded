using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.Database;

public static class CatalogueCache
{
    public static string CachePath { get; } = Path.Combine(Databases.CacheFolderPath, Databases.ConfigDatabase.CdbName());

    public static uint Hash()
    {
        try
        {
            return Crc32.GetFileHash(CachePath, new byte[500000]);
        }
        catch (Exception ex)
        {
            return 0;
        }
    }

    public static byte[] Load() => File.ReadAllBytes(CachePath);

    public static void Save(byte[] data) => File.WriteAllBytes(CachePath, data);

    public static List<Card> UpdateCatalogue(byte[] data)
    {
        using var reader = new BinaryReader(data.UnZip());
        reader.ReadByte();
        return reader.ReadList<Card, List<Card>>(Card.ReadVariant);
    }
}