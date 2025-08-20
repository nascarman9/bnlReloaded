using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Service;

public interface IServiceMapEditor : IService
{
    public void SendLoadMap(ushort rpcId, MapData? map, byte[]? blocks = null, byte[]? colors = null, string? error = null);
    public void SendSaveMap(ushort rpcId, string? error = null);
    public void SendLoadMetadata(ushort rpcId, HerculesMetadata? metadata, string? error = null);
    public void SendEncodeMap(ushort rpcId, string? signedMap, EMapValidation? mapValidation = null, string? error = null);
    public void SendDecodeMap(ushort rpcId, MapData? map, byte[]? blocks = null, byte[]? colors = null, string? error = null);
    public void SendCheckMap(ushort rpcId, EMapValidation? mapValidation = null, string? error = null);
    public void SendPlayMapData(ushort rpcId, EMapValidation? mapValidation = null, string? error = null);
}