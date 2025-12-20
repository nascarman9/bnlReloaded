using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ServerTypes;

namespace BNLReloadedServer.Database;

public interface IMapDatabase
{
    public List<CardMap> LoadMapCards();
    public List<CardMap> GetMapCards();
    public CardMap? LoadMapCard(Key key);
    public MapData? LoadMapData(Key key);
    public byte[]? LoadBlockData(Key key);
    public byte[]? LoadColorData(Key key);
    public ExtraMaps? GrabExtraMaps();
    public void SaveMap(string key, CardMap mapCard, MapData mapData);
}