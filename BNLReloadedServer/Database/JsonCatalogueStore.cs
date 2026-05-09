using System.Text.Json;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ServerTypes;

namespace BNLReloadedServer.Database;

public class JsonCatalogueStore(
    string fromPath,
    string toPath,
    string deserializedPath,
    JsonSerializerOptions serializerOptions) : CatalogueStore
{
    public override void Store(IEnumerable<Card> cards)
    {
        using var fs = new StreamWriter(File.Create(toPath));
        fs.Write(JsonSerializer.Serialize(cards, serializerOptions).Replace("\\u00A0", "\u00A0"));
    }

    public override void Load(IEnumerable<CardMap> maps, ExtraMaps? extraMaps)
    {
        using var fs = new StreamReader(File.OpenRead(fromPath));
        if (JsonSerializer.Deserialize<List<Card>>(fs.ReadToEnd(), serializerOptions) is not {} deserializedCards) return;
        deserializedCards.RemoveAll(c => c is CardMap or CardMapData);
            
        // Add maps
        AddMaps(deserializedCards, maps, extraMaps);

        var memStream = new MemoryStream();
        var writer = new BinaryWriter(memStream);
        using var fs2 = File.Create(deserializedPath);
        writer.Write((byte)0);
        writer.WriteList(deserializedCards, Card.WriteVariant);
        var zipped = (writer.BaseStream as MemoryStream)?.GetBuffer().Zip(0);
        zipped?.CopyTo(fs2);
        zipped?.Close();
    }
}