using System.Text.Json;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ServerTypes;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BNLReloadedServer.Database;

public class MapDatabase : IMapDatabase
{
    private static string MapPath { get; } = Path.Combine(Databases.BaseFolderPath, "Maps");
    
    private static string MapListFile { get; } = Path.Combine(Databases.ConfigsFolderPath, "extraMaps.json");
    
    private readonly Dictionary<Key, string> _maps = new();

    public List<CardMap> LoadMapCards()
    {
        var mapCards = new List<CardMap>();
        if (!Directory.Exists(MapPath)) return mapCards;
        
        var mapFolders = Directory.GetDirectories(MapPath, "*", SearchOption.TopDirectoryOnly);
        foreach (var mapFolder in mapFolders)
        {
            var mapKey = new Key(Path.GetFileNameWithoutExtension(mapFolder));
            _maps.Add(mapKey, mapFolder);
            var map = LoadMapCard(mapKey);
            if (map == null) continue;
            map.Data?.BlocksData = null;
            map.Data?.ColorsData = null;
            mapCards.Add(map);
        }
        
        return mapCards;
    }

    public List<CardMap> GetMapCards() => 
        _maps.Count == 0 ? LoadMapCards() : _maps.Select(map => map.Key.GetCard<CardMap>()).OfType<CardMap>().ToList();

    public CardMap? LoadMapCard(Key key)
    {
        if (_maps.Count == 0)
        {
            LoadMapCards();
        }
        
        if (!_maps.TryGetValue(key, out var mapFolder) || !Directory.Exists(mapFolder)) return null;
        
        CardMap? map = null;
        MapData? mapData = null;
        var files = Directory.GetFiles(mapFolder, "*", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            switch (Path.GetFileNameWithoutExtension(file))
            {
                case "card":
                {
                    using var fs = new StreamReader(File.OpenRead(file));
                    map = JsonSerializer.Deserialize<CardMap>(fs.ReadToEnd(), JsonHelper.DefaultSerializerSettings);
                    break;
                }
                
                case "data":
                {
                    using var fs = new StreamReader(File.OpenRead(file));
                    mapData = JsonSerializer.Deserialize<MapData>(fs.ReadToEnd(), JsonHelper.DefaultSerializerSettings);
                    break;
                }

                case "workshopData":
                {
                    using var fs = new StreamReader(File.OpenRead(file));
                    var handler = new JsonWebTokenHandler();
                    var jsonWebToken = handler.ReadJsonWebToken(fs.ReadToEnd());
            
                    var rawPayload = jsonWebToken.EncodedPayload;
                    var mapJson = Base64UrlEncoder.Decode(rawPayload);
            
                    mapData = JsonSerializer.Deserialize<MapData>(mapJson, JsonHelper.DefaultSerializerSettings);
                    break;
                }
                
                case "editorData":
                {
                    var unzippedFile = File.ReadAllBytes(file).UnZip();
                    var customMap = JsonSerializer.Deserialize<MapCustomData>(unzippedFile, JsonHelper.DefaultSerializerSettings);
                    mapData = customMap?.Map;
                    break;
                }
            }
        }
        if (map == null || mapData == null) return null;
        map.Data = mapData;
        return map;
    }

    public MapData? LoadMapData(Key key) => LoadMapCard(key)?.Data;

    public byte[]? LoadBlockData(Key key) => LoadMapData(key)?.BlocksData;

    public byte[]? LoadColorData(Key key) => LoadMapData(key)?.ColorsData;
    
    public ExtraMaps? GrabExtraMaps()
    {
        if (!File.Exists(MapListFile)) return null;
        using var fs = new StreamReader(File.OpenRead(MapListFile));
        return JsonSerializer.Deserialize<ExtraMaps>(fs.ReadToEnd(), JsonHelper.DefaultSerializerSettings);
    }

    public void SaveMap(string key, CardMap mapCard, MapData mapData)
    {
        var info = Directory.CreateDirectory(Path.Combine(MapPath, key));
        using (var cardWriter = File.CreateText(Path.Combine(info.FullName, "card.json")))
        {
            mapCard.Data = null;
            cardWriter.Write(JsonSerializer.Serialize(mapCard, JsonHelper.DefaultSerializerSettings));
        }

        using (var mapDataWriter = File.CreateText(Path.Combine(info.FullName, "data.json")))
        {
            mapDataWriter.Write(JsonSerializer.Serialize(mapData, JsonHelper.DefaultSerializerSettings));
        }
    }
}