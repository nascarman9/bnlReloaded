using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ServerTypes;

namespace BNLReloadedServer.Database;

public interface IMapDatabase
{
    public List<CardMap> LoadMapCards();
    public CardMap? LoadMapCard(Key key);
    public MapData? LoadMapData(Key key);
    public byte[]? LoadBlockData(Key key);
    public byte[]? LoadColorData(Key key);
    public ExtraMaps? GrabExtraMaps();
}