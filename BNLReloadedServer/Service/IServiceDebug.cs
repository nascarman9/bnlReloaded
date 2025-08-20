using System.Numerics;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Service;

public interface IServiceDebug : IService
{
    public void SendExecute(ushort rpcId, string? result, string? error = null);
    public void SendExecuteArgs(ushort rpcId, string? result, string? error = null);
    public void SendGetScreenshot(ushort rpcId);
    public void SendGetNodeTree(ushort rpcId, DebugServerNode? node, string? error = null);
    public void SendCoreCommand(ushort rpcId, string? result, string? error = null);
    public void SendFileListing(ushort rpcId, List<string>? data, string? error = null);
    public void SendLoadFile(ushort rpcId, byte[]? data, string? error = null);
    public void SendSaveFile(ushort rpcId, string? error = null);
    public void SendGetTriggers(ushort rpcId, Dictionary<int, List<Vector3s>>? triggers, string? error = null);
    public void SendZoneAddSplash(Vector3s hitPos, float radius, float damage, Dictionary<Vector3s, float> blocks);
    public void SendZoneUnitMoved(int unitId, Vector3 pos, List<Vector3s> blocks);
    public void SendZoneUnitRemoved(int unitId);
    public void SendZoneTriggerMoved(int triggerId, List<Vector3s> blocks);
    public void SendZoneTriggerRemoved(int triggerId);
}