using System.Text.Json;
using System.Text.RegularExpressions;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ServerTypes;
using CouchDB.Driver;

namespace BNLReloadedServer.Database;

public partial class CouchCatalogueStore(
    CouchClient fromDb, 
    string toPath,
    string deserializedPath,
    JsonSerializerOptions serializerOptions): CatalogueStore
{
    private static readonly HashSet<string> Exclude = ["map", "map_data"];
    
    public override void Store(IEnumerable<Card> cards)
    {
        using var fs = new StreamWriter(File.Create(toPath));
        fs.Write(JsonSerializer.Serialize(cards, serializerOptions).Replace("\\u00A0", "\u00A0"));
    }

    public override void Load(IEnumerable<CardMap> maps, ExtraMaps? extraMaps)
    {
        List<Card> cards = [];
        foreach (var cardName in Enum.GetNames<CardCategory>().Select(e => SnakeRegex().Replace(e, "$1_$2").ToLower())
                     .Where(e => !Exclude.Contains(e)))
        {
            cards.AddRange(fromDb.GetDatabase<Card>(cardName).ToList());
        }
        
        // Add maps
        AddMaps(cards, maps, extraMaps);

        var memStream = new MemoryStream();
        var writer = new BinaryWriter(memStream);
        using var fs2 = File.Create(deserializedPath);
        writer.Write((byte)0);
        writer.WriteList(cards, Card.WriteVariant);
        var zipped = (writer.BaseStream as MemoryStream)?.GetBuffer().Zip(0);
        zipped?.CopyTo(fs2);
        zipped?.Close();
    }

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex SnakeRegex();
}