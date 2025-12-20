using System.Numerics;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;

namespace BNLReloadedServer.Service;

public class ServiceZone(ISender sender) : IServiceZone
{
    private enum ServiceZoneId : byte
    {
        MessageInitZone = 0,
        MessageZoneReady = 1,
        MessageZoneLeave = 2,
        MessageEndMatch = 3,
        MessageEndMatchResult = 4,
        MessageExitMatch = 5,
        MessageFinishTutorial = 6,
        MessageKickPlayer = 7,
        MessageBlockUpdates = 8,
        MessageUpdateZone = 9,
        MessageUnitCreate = 10,
        MessageUnitUpdate = 11,
        MessageUnitDrop = 12,
        MessageUnitMove = 13,
        MessageUnitMoveFail = 14,
        MessageUnitManeuver = 15,
        MessageUnitControl = 16,
        MessageReceiveUnitMove = 17,
        MessageCast = 18,
        MessageImpact = 19,
        MessageDoStartReload = 20,
        MessageDoEndReload = 21,
        MessageDoCancelReload = 22,
        MessageDoStartChannel = 23,
        MessageDoEndChannel = 24,
        MessageDoDashStartCharge = 25,
        MessageDoDashEndCharge = 26,
        MessageDoToolStartCharge = 27,
        MessageDoToolEndCharge = 28,
        MessageDoGroundSlamCast = 29,
        MessageDoStartBuild = 30,
        MessageDoCancelBuild = 31,
        MessageReceiveCast = 32,
        MessageHit = 33,
        MessageSwitchGear = 34,
        MessageStartReload = 35,
        MessageReload = 36,
        MessageEndReload = 37,
        MessageCancelReload = 38,
        MessageStartChannel = 39,
        MessageEndChannel = 40,
        MessageDashStartCharge = 41,
        MessageDashEndCharge = 42,
        MessageDashCast = 43,
        MessageDashHit = 44,
        MessageToolStartCharge = 45,
        MessageToolEndCharge = 46,
        MessageGroundSlamCast = 47,
        MessageGroundSlamHit = 48,
        MessageStartBuild = 49,
        MessageCancelBuild = 50,
        MessageCastAbility = 51,
        MessageDoCastAbility = 52,
        MessageReceiveCreateProjectile = 53,
        MessageReceiveMoveProjectile = 54,
        MessageReceiveDropProjectile = 55,
        MessageCreateProjectile = 56,
        MessageMoveProjectile = 57,
        MessageDropProjectile = 58,
        MessagePickup = 59,
        MessageUpdateDrown = 60,
        MessageFallHit = 61,
        MessageEmitZoneEvent = 62,
        MessageBroadcastZoneEvent = 63,
        MessageReceivePlayerCommand = 64,
        MessagePlayerCommand = 65,
        MessageSetSpawnPoint = 66,
        MessageKill = 67,
        MessageDamage = 68,
        MessageDeviceBuilt = 69,
        MessageBlockMined = 70,
        MessageResourceBonusIncreased = 71,
        MessagePickupTaken = 72,
        MessageUpdateBarriers = 73,
        MessagePortalTeleport = 74,
        MessageMapTriggerAction = 75,
        MessageStartRecall = 76,
        MessageDoStartRecall = 77,
        MessageDoCancelRecall = 78,
        MessageDoRecall = 79,
        MessageSurrenderStart = 80,
        MessageSurrenderBegin = 81,
        MessageSurrenderVote = 82,
        MessageSurrenderProgress = 83,
        MessageSurrenderEnd = 84,
        MessageSetTurretTarget = 85,
        MessageReceiveTurretAttack = 86,
        MessageTurretAttack = 87,
        MessageFireMortar = 88,
        MessageReceiveMortarAttack = 89,
        MessageMortarAttack = 90,
        MessageUpdateTesla = 91,
        MessageTeslaAttack = 92,
        MessageReceiveDrillAttack = 93,
        MessageDrillAttack = 94,
        MessageUnitProjectileHit = 95,
        MessageSkybeamHit = 96,
        MessageExecuteMapEditorCommand = 97
    }
    
    private readonly IRegionServerDatabase _serverDatabase = Databases.RegionServerDatabase;
    private IGameInstance? GameInstance => Databases.RegionServerDatabase.GetGameInstance(sender.AssociatedPlayerId);
    
    private static BinaryWriter CreateWriter()
    {
        var memStream =  new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServiceZone);
        return writer;
    }

    public void SendInitZone(ZoneInitData data)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageInitZone);
        ZoneInitData.WriteRecord(writer, data);
        sender.Send(writer);
    }

    private void ReceiveZoneReady(BinaryReader reader)
    {
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.PlayerZoneReady(sender.AssociatedPlayerId.Value);
        }
    }

    private void ReceiveZoneLeave(BinaryReader reader)
    {
        if (sender.AssociatedPlayerId.HasValue)
        {
            _serverDatabase.RemoveFromCustomGame(sender.AssociatedPlayerId.Value);
            GameInstance?.PlayerLeftInstance(sender.AssociatedPlayerId.Value, KickReason.MatchQuit);    
        }
    }

    public void SendEndMatch(TeamType winner)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageEndMatch);
        writer.WriteByteEnum(winner);
        sender.Send(writer);
    }

    public void SendEndMatchResult(EndMatchData data)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageEndMatchResult);
        EndMatchData.WriteRecord(writer, data);
        sender.Send(writer);
    }

    private void ReceiveExitMatch(BinaryReader reader)
    {
        if (sender.AssociatedPlayerId.HasValue)
        {
            _serverDatabase.RemoveFromCustomGame(sender.AssociatedPlayerId.Value);
            GameInstance?.PlayerLeftInstance(sender.AssociatedPlayerId.Value, KickReason.MatchQuit);    
        }
    }

    private void ReceiveFinishTutorial(BinaryReader reader)
    {
        
    }

    public void SendKickPlayer(uint playerId, KickReason reason)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageKickPlayer);
        writer.Write(playerId);
        writer.WriteByteEnum(reason);
        sender.Send(writer);
    }

    public void SendBlockUpdates(Dictionary<Vector3s, BlockUpdate> updates)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageBlockUpdates);
        writer.WriteMap(updates, writer.Write, BlockUpdate.WriteRecord);
        sender.Send(writer);
    }

    public void SendUpdateZone(ZoneUpdate data)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageUpdateZone);
        ZoneUpdate.WriteRecord(writer, data);
        sender.Send(writer);
    }

    public void SendUnitCreate(uint id, UnitInit data)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageUnitCreate);
        writer.Write(id);
        UnitInit.WriteRecord(writer, data);
        sender.Send(writer);
    }

    public void SendUnitUpdate(uint id, UnitUpdate data)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageUnitUpdate);
        writer.Write(id);
        UnitUpdate.WriteRecord(writer, data);
        sender.Send(writer);
    }

    public void SendUnitDrop(uint id)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageUnitDrop);
        writer.Write(id);
        sender.Send(writer);
    }

    public void SendUnitMove(uint id, ulong time, ZoneTransform transform)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageUnitMove);
        writer.Write(id);
        writer.Write(time);
        ZoneTransform.WriteRecord(writer, transform);
        sender.Send(writer);
    }

    public void SendUnitMoveFail(uint id, ulong time)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageUnitMoveFail);
        writer.Write(id);
        writer.Write(time);
        sender.Send(writer);
    }

    public void SendUnitManeuver(uint id, Maneuver maneuver)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageUnitManeuver);
        writer.Write(id);
        Maneuver.WriteVariant(writer, maneuver);
        sender.Send(writer);
    }

    public void SendUnitControl(uint id)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageUnitControl);
        writer.Write(id);
        sender.Send(writer);
    }

    private void ReceiveUnitMove(BinaryReader reader)
    {
        var id = reader.ReadUInt32();
        var time = reader.ReadUInt64();
        var transform = ZoneTransform.ReadRecord(reader);
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.UnitMoved(id, time, transform);
        }
    }

    public void SendCast(uint unitId, CastData data)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageCast);
        writer.Write(unitId);
        CastData.WriteRecord(writer, data);
        sender.Send(writer);
    }

    public void SendImpact(ImpactData data)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageImpact);
        ImpactData.WriteRecord(writer, data);
        sender.Send(writer);
    }

    public void SendDoStartReload(uint unitId)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDoStartReload);
        writer.Write(unitId);
        sender.Send(writer);
    }

    public void SendDoEndReload(uint unitId)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDoEndReload);
        writer.Write(unitId);
        sender.Send(writer);
    }

    public void SendDoCancelReload(uint unitId)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDoCancelReload);
        writer.Write(unitId);
        sender.Send(writer);
    }

    public void SendDoStartChannel(uint unitId, ChannelData data)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDoStartChannel);
        writer.Write(unitId);
        ChannelData.WriteRecord(writer, data);
        sender.Send(writer);
    }

    public void SendDoEndChannel(uint unitId, byte toolIndex)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDoEndChannel);
        writer.Write(unitId);
        writer.Write(toolIndex);
        sender.Send(writer);
    }

    public void SendDoDashStartCharge(uint unitId, byte toolIndex)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDoDashStartCharge);
        writer.Write(unitId);
        writer.Write(toolIndex);
        sender.Send(writer);
    }

    public void SendDoDashEndCharge(uint unitId, byte toolIndex)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDoDashEndCharge);
        writer.Write(unitId);
        writer.Write(toolIndex);
        sender.Send(writer);
    }

    public void SendDoToolStartCharge(uint unitId, byte toolIndex)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDoToolStartCharge);
        writer.Write(unitId);
        writer.Write(toolIndex);
        sender.Send(writer);
    }

    public void SendDoToolEndCharge(uint unitId, byte toolIndex)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDoToolEndCharge);
        writer.Write(unitId);
        writer.Write(toolIndex);
        sender.Send(writer);
    }

    public void SendDoGroundSlamCast(uint unitId, byte toolIndex)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDoGroundSlamCast);
        writer.Write(unitId);
        writer.Write(toolIndex);
        sender.Send(writer);
    }

    public void SendDoStartBuild(uint unitId, BuildInfo info)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDoStartBuild);
        writer.Write(unitId);
        BuildInfo.WriteRecord(writer, info);
        sender.Send(writer);
    }

    public void SendDoCancelBuild(uint unitId)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDoCancelBuild);
        writer.Write(unitId);
        sender.Send(writer);
    }

    private void ReceiveCast(BinaryReader reader)
    {
        var data = CastData.ReadRecord(reader);
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.CastRequest(sender.AssociatedPlayerId.Value, data);
        }
    }

    private void ReceiveHit(BinaryReader reader)
    {
        var time = reader.ReadUInt64();
        var hits = reader.ReadMap<ulong, HitData, Dictionary<ulong, HitData>>(reader.ReadUInt64, HitData.ReadRecord);
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.Hit(time, hits);
        }
    }

    public void SendSwitchGear(ushort rpcId, bool? accepted, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageSwitchGear);
        writer.Write(rpcId);
        if (accepted.HasValue)
        {
            writer.Write((byte) 0);
            writer.Write(accepted.Value);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveSwitchGear(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var gearKey = Key.ReadRecord(reader);
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.SwitchGear(rpcId, sender.AssociatedPlayerId.Value, gearKey, this);
        }
    }

    public void SendStartReload(ushort rpcId, bool? accepted, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageStartReload);
        writer.Write(rpcId);
        if (accepted.HasValue)
        {
            writer.Write((byte) 0);
            writer.Write(accepted.Value);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveStartReload(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.StartReload(rpcId, sender.AssociatedPlayerId.Value, this);
        }
    }

    public void SendReload(ushort rpcId, bool? accepted, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageReload);
        writer.Write(rpcId);
        if (accepted.HasValue)
        {
            writer.Write((byte) 0);
            writer.Write(accepted.Value);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveReload(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.Reload(rpcId, sender.AssociatedPlayerId.Value, this);
        }
    }

    private void ReceiveEndReload(BinaryReader reader)
    {
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.ReloadEnd(sender.AssociatedPlayerId.Value);
        }
    }

    private void ReceiveCancelReload(BinaryReader reader)
    {
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.ReloadCancel(sender.AssociatedPlayerId.Value);
        }
    }

    public void SendStartChannel(ushort rpcId, bool? accepted, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageStartChannel);
        writer.Write(rpcId);
        if (accepted.HasValue)
        {
            writer.Write((byte) 0);
            writer.Write(accepted.Value);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveStartChannel(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var data = ChannelData.ReadRecord(reader);
        
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.CreateChannel(rpcId, sender.AssociatedPlayerId.Value, data, this);
        }
    }

    private void ReceiveEndChannel(BinaryReader reader)
    {
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.EndChannel(sender.AssociatedPlayerId.Value);
        }
    }

    private void ReceiveDashStartCharge(BinaryReader reader)
    {
        var toolIndex = reader.ReadByte();
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.DashChargeStart(sender.AssociatedPlayerId.Value, toolIndex);
        }
    }

    public void SendDashEndChargeSuccess(ushort rpcId, bool? isMaxCharge)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDashEndCharge);
        writer.Write(rpcId);
        writer.Write((byte) 0);
        writer.WriteOptionValue(isMaxCharge, writer.Write);
        sender.Send(writer);
    }

    public void SendDashEndChargeFail(ushort rpcId, string error)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDashEndCharge);
        writer.Write(rpcId);
        writer.Write(byte.MaxValue);
        writer.Write(error);
        sender.Send(writer);
    }

    private void ReceiveDashEndCharge(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var toolIndex = reader.ReadByte();

        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.DashChargeEnd(rpcId, sender.AssociatedPlayerId.Value, toolIndex, this);
        }
    }

    private void ReceiveDashCast(BinaryReader reader)
    {
        var toolIndex = reader.ReadByte();
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.DashCast(sender.AssociatedPlayerId.Value, toolIndex);
        }
    }

    private void ReceiveDashHit(BinaryReader reader)
    {
        var toolIndex = reader.ReadByte();
        var data = HitData.ReadRecord(reader);

        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.DashHit(sender.AssociatedPlayerId.Value, toolIndex, data);
        }
    }

    private void ReceiveToolStartCharge(BinaryReader reader)
    {
        var toolIndex = reader.ReadByte();
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.ToolChargeStart(sender.AssociatedPlayerId.Value, toolIndex);
        }
    }

    public void SendToolEndChargeSuccess(ushort rpcId, bool accepted, float? charge)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageToolEndCharge);
        writer.Write(rpcId);
        writer.Write((byte) 0);
        writer.Write(accepted);
        writer.WriteOptionValue(charge, writer.Write);
        sender.Send(writer);
    }

    public void SendToolEndChargeFail(ushort rpcId, string error)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageToolEndCharge);
        writer.Write(rpcId);
        writer.Write(byte.MaxValue);
        writer.Write(error);
        sender.Send(writer);
    }

    private void ReceiveToolEndCharge(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var toolIndex = reader.ReadByte();
        
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.ToolChargeEnd(rpcId, sender.AssociatedPlayerId.Value, toolIndex, this);
        }
    }

    private void ReceiveGroundSlamCast(BinaryReader reader)
    {
        var toolIndex = reader.ReadByte();

        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.GroundSlamCast(sender.AssociatedPlayerId.Value, toolIndex);
        }
    }

    private void ReceiveGroundSlamHit(BinaryReader reader)
    {
        var toolIndex = reader.ReadByte();
        var data = HitData.ReadRecord(reader);
        
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.GroundSlamHit(sender.AssociatedPlayerId.Value, toolIndex, data);
        }
    }

    public void SendStartBuild(ushort rpcId, bool? accepted, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageStartBuild);
        writer.Write(rpcId);
        if (accepted.HasValue)
        {
            writer.Write((byte) 0);
            writer.Write(accepted.Value);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveStartBuild(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var info = BuildInfo.ReadRecord(reader);
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.BuildRequest(rpcId, sender.AssociatedPlayerId.Value, info, this);
        }
    }

    private void ReceiveCancelBuild(BinaryReader reader)
    {
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.CancelBuildRequest(sender.AssociatedPlayerId.Value);
        }
    }

    public void SendCastAbility(ushort rpcId, bool? accepted, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageCastAbility);
        writer.Write(rpcId);
        if (accepted.HasValue)
        {
            writer.Write((byte) 0);
            writer.Write(accepted.Value);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveCastAbility(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var data = AbilityCastData.ReadRecord(reader);

        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.AbilityCast(rpcId, sender.AssociatedPlayerId.Value, data, this);
        }
    }
    
    public void SendDoCastAbility(uint unitId, AbilityCastData data)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDoCastAbility);
        writer.Write(unitId);
        AbilityCastData.WriteRecord(writer, data);
        sender.Send(writer);
    }
    
    private void ReceiveCreateProjectile(BinaryReader reader)
    {
        var shotId = reader.ReadUInt64();
        var info = ProjectileInfo.ReadRecord(reader);
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.CreateProj(sender.AssociatedPlayerId.Value, shotId, info);
        }
    }

    private void ReceiveMoveProjectile(BinaryReader reader)
    {
        var shotId = reader.ReadUInt64();
        var time = reader.ReadUInt64();
        var transform = ZoneTransform.ReadRecord(reader);
        GameInstance?.MoveProj(shotId, time, transform);
    }

    private void ReceiveDropProjectile(BinaryReader reader)
    {
        var shotId = reader.ReadUInt64();
        GameInstance?.DropProj(shotId);
    }
    
    public void SendCreateProjectile(ulong shotId, ProjectileInfo info, Guid? creatingSession = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageCreateProjectile);
        writer.Write(shotId);
        ProjectileInfo.WriteRecord(writer, info);
        if (creatingSession.HasValue)
        {
            sender.SendExcept(writer, [creatingSession.Value]);
        }
        else
        {
            sender.Send(writer);
        }
    }

    public void SendMoveProjectile(ulong shotId, ulong time, ZoneTransform transform)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageMoveProjectile);
        writer.Write(shotId);
        writer.Write(time);
        ZoneTransform.WriteRecord(writer, transform);
        sender.Send(writer);
    }

    public void SendDropProjectile(ulong shotId)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDropProjectile);
        writer.Write(shotId);
        sender.Send(writer);
    }

    private void ReceivePickup(BinaryReader reader)
    {
        var pickupId = reader.ReadUInt32();
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.Pickup(sender.AssociatedPlayerId.Value, pickupId);
        }
    }

    private void ReceiveUpdateDrown(BinaryReader reader)
    {
        var drown = reader.ReadBoolean();
    }

    private void ReceiveFallHit(BinaryReader reader)
    {
        var unitId = reader.ReadUInt32();
        var height = reader.ReadSingle();
        var force = reader.ReadBoolean();
        
        GameInstance?.Fall(unitId, height, force);
    }

    private void ReceiveEmitZoneEvent(BinaryReader reader)
    {
        var data = ZoneEvent.ReadVariant(reader);
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.EventBroadcast(data);
        }
    }

    public void SendBroadcastZoneEvent(ZoneEvent data)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageBroadcastZoneEvent);
        ZoneEvent.WriteVariant(writer, data);
        sender.Send(writer);
    }

    public void SendPlayerCommand(uint playerId, Key commandKey)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessagePlayerCommand);
        writer.Write(playerId);
        Key.WriteRecord(writer, commandKey);
        sender.Send(writer);
    }

    private void ReceivePlayerCommand(BinaryReader reader)
    {
        var commandKey = Key.ReadRecord(reader);
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.PlayerCommand(sender.AssociatedPlayerId.Value, commandKey);
        }
    }

    private void ReceiveSetSpawnPoint(BinaryReader reader)
    {
        var spawnPointId = reader.ReadOptionValue(reader.ReadUInt32);

        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.SelectSpawnPoint(sender.AssociatedPlayerId.Value, spawnPointId);
        }
    }

    public void SendKill(KillInfo info)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageKill);
        KillInfo.WriteRecord(writer, info);
        sender.Send(writer);
    }

    public void SendDamage(DamageInfo info)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDamage);
        DamageInfo.WriteRecord(writer, info);
        sender.Send(writer);
    }

    public void SendDeviceBuilt(uint builderPlayerId, Key deviceKey, Vector3 position)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDeviceBuilt);
        writer.Write(builderPlayerId);
        Key.WriteRecord(writer, deviceKey);
        writer.Write(position);
        sender.Send(writer);
    }

    public void SendBlockMined(uint minerPlayerId, Key blockKey)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageBlockMined);
        writer.Write(minerPlayerId);
        Key.WriteRecord(writer, blockKey);
        sender.Send(writer);
    }

    public void SendResourceBonusIncreased()
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageResourceBonusIncreased);
        sender.Send(writer);
    }

    public void SendPickupTaken(uint playerId, Key pickupKey)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessagePickupTaken);
        writer.Write(playerId);
        Key.WriteRecord(writer, pickupKey);
        sender.Send(writer);
    }

    public void SendUpdateBarriers(List<BarrierLabel> barriers)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageUpdateBarriers);
        writer.WriteList(barriers, writer.WriteByteEnum);
        sender.Send(writer);
    }

    public void SendPortalTeleport(uint teleportedUnit, uint portalFrom, uint portalTo)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessagePortalTeleport);
        writer.Write(teleportedUnit);
        writer.Write(portalFrom);
        writer.Write(portalTo);
        sender.Send(writer);
    }

    private void ReceiveMapTriggerAction(BinaryReader reader)
    {
        var triggerTag = reader.ReadString();
        var isEnter = reader.ReadBoolean();
    }

    private void ReceiveStartRecall(BinaryReader reader)
    {
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.StartRecall(sender.AssociatedPlayerId.Value);
        }
    }

    public void SendDoStartRecall(uint unitId, float duration, ulong endTime)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDoStartRecall);
        writer.Write(unitId);
        writer.Write(duration);
        writer.Write(endTime);
        sender.Send(writer);
    }

    public void SendDoCancelRecall(uint unitId)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDoCancelRecall);
        writer.Write(unitId);
        sender.Send(writer);
    }

    public void SendDoRecall(uint unitId)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDoRecall);
        writer.Write(unitId);
        sender.Send(writer);
    }

    public void SendSurrenderStart(ushort rpcId, SurrenderStartResultType? result, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageSurrenderStart);
        writer.Write(rpcId);
        if (result != null)
        {
            writer.Write((byte) 0);
            writer.WriteByteEnum(result.Value);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveSurrenderStart(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.Surrender(rpcId, sender.AssociatedPlayerId.Value, this);
        }
    }

    public void SendSurrenderBegin(ulong endTime)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageSurrenderBegin);
        writer.Write(endTime);
        sender.Send(writer);
    }

    private void ReceiveSurrenderVote(BinaryReader reader)
    {
        var accept = reader.ReadBoolean();
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.SurrenderVote(sender.AssociatedPlayerId.Value, accept);
        }
    }

    public void SendSurrenderProgress(Dictionary<uint, bool?> votes)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageSurrenderProgress);
        writer.WriteMap(votes, writer.Write, item => writer.WriteOptionValue(item, writer.Write));
        sender.Send(writer);
    }

    public void SendSurrenderEnd(TeamType team, bool result)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageSurrenderEnd);
        writer.WriteByteEnum(team);
        writer.Write(result);
        sender.Send(writer);
    }

    private void ReceiveSetTurretTarget(BinaryReader reader)
    {
        var turretId = reader.ReadUInt32();
        var targetId = reader.ReadUInt32();
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.TurretTarget(sender.AssociatedPlayerId.Value, turretId, targetId);
        }
    }

    private void ReceiveTurretAttack(BinaryReader reader)
    {
        var turretId = reader.ReadUInt32();
        var shotPos = reader.ReadVector3();
        var shots = reader.ReadList<ShotData, List<ShotData>>(ShotData.ReadRecord);
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.TurretAttack(sender.AssociatedPlayerId.Value, turretId, shotPos, shots);
        }
    }

    public void SendTurretAttack(uint turretId, Vector3 shotPos, List<ShotData> shots)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageTurretAttack);
        writer.Write(turretId);
        writer.Write(shotPos);
        writer.WriteList(shots, ShotData.WriteRecord);
        sender.Send(writer);
    }

    public void SendFireMortar(uint mortarId, Vector3 target)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageFireMortar);
        writer.Write(mortarId);
        writer.Write(target);
        sender.Send(writer);
    }

    private void ReceiveMortarAttack(BinaryReader reader)
    {
        var mortarId = reader.ReadUInt32();
        var shotPos = reader.ReadVector3();
        var shots =  reader.ReadList<ShotData, List<ShotData>>(ShotData.ReadRecord);
        GameInstance?.MortarAttack(mortarId, shotPos, shots);
    }

    public void SendMortarAttack(uint mortarId, Vector3 shotPos, List<ShotData> shots)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageMortarAttack);
        writer.Write(mortarId);
        writer.Write(shotPos);
        writer.WriteList(shots, ShotData.WriteRecord);
        sender.Send(writer);
    }

    private void ReceiveUpdateTesla(BinaryReader reader)
    {
        var teslaId = reader.ReadUInt32();
        var targetId = reader.ReadOptionValue(reader.ReadUInt32);
        var teslasInRange = reader.ReadList<uint, List<uint>>(reader.ReadUInt32);
        
        GameInstance?.UpdateTesla(teslaId, targetId, teslasInRange);
    }

    public void SendTeslaAttack(uint teslaId, uint targetId, List<uint> chargePath)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageTeslaAttack);
        writer.Write(teslaId);
        writer.Write(targetId);
        writer.WriteList(chargePath, writer.Write);
        sender.Send(writer);
    }

    private void ReceiveDrillAttack(BinaryReader reader)
    {
        var drillId = reader.ReadUInt32();
        var shotPos = reader.ReadVector3();
        var shots = reader.ReadList<ShotData, List<ShotData>>(ShotData.ReadRecord);
        GameInstance?.DrillAttack(drillId, shotPos, shots);
    }

    public void SendDrillAttack(uint drillId, Vector3 shotPos, List<ShotData> shots)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceZoneId.MessageDrillAttack);
        writer.Write(drillId);
        writer.Write(shotPos);
        writer.WriteList(shots, ShotData.WriteRecord);
        sender.Send(writer);
    }

    private void ReceiveUnitProjectileHit(BinaryReader reader)
    {
        var unitId = reader.ReadUInt32();
        var data = HitData.ReadRecord(reader);
        GameInstance?.UnitProjectileHit(unitId, data);
    }

    private void ReceiveSkybeamHit(BinaryReader reader)
    {
        var unitId = reader.ReadUInt32();
        var data = HitData.ReadRecord(reader);
        GameInstance?.SkybeamHit(unitId, data);
    }

    private void ReceiveExecuteMapEditorCommand(BinaryReader reader)
    {
        var command = reader.ReadByteEnum<MapEditorCommand>();
        if (sender.AssociatedPlayerId.HasValue)
        {
            GameInstance?.EditorCommand(sender.AssociatedPlayerId.Value, command, false);
        }
    }
    
    public bool Receive(BinaryReader reader)
    {
        var serviceZoneId = reader.ReadByte();
        ServiceZoneId? zoneEnum = null;
        if (Enum.IsDefined(typeof(ServiceZoneId), serviceZoneId))
        {
            zoneEnum = (ServiceZoneId)serviceZoneId;
        }

        if (Databases.ConfigDatabase.DebugMode())
        {
            Console.WriteLine($"ServiceZoneId: {zoneEnum.ToString()}");
        }

        switch (zoneEnum)
        {
            case ServiceZoneId.MessageZoneReady:
                ReceiveZoneReady(reader);
                break;
            case ServiceZoneId.MessageZoneLeave:
                ReceiveZoneLeave(reader);
                break;
            case ServiceZoneId.MessageExitMatch:
                ReceiveExitMatch(reader);
                break;
            case ServiceZoneId.MessageFinishTutorial:
                ReceiveFinishTutorial(reader);
                break;
            case ServiceZoneId.MessageReceiveUnitMove:
                ReceiveUnitMove(reader);
                break;
            case ServiceZoneId.MessageReceiveCast:
                ReceiveCast(reader);
                break;
            case ServiceZoneId.MessageHit:
                ReceiveHit(reader);
                break;
            case ServiceZoneId.MessageSwitchGear:
                ReceiveSwitchGear(reader);
                break;
            case ServiceZoneId.MessageStartReload:
                ReceiveStartReload(reader);
                break;
            case ServiceZoneId.MessageReload:
                ReceiveReload(reader);
                break;
            case ServiceZoneId.MessageEndReload:
                ReceiveEndReload(reader);
                break;
            case ServiceZoneId.MessageCancelReload:
                ReceiveCancelReload(reader);
                break;
            case ServiceZoneId.MessageStartChannel:
                ReceiveStartChannel(reader);
                break;
            case ServiceZoneId.MessageEndChannel:
                ReceiveEndChannel(reader);
                break;
            case ServiceZoneId.MessageDashStartCharge:
                ReceiveDashStartCharge(reader);
                break;
            case ServiceZoneId.MessageDashEndCharge:
                ReceiveDashEndCharge(reader);
                break;
            case ServiceZoneId.MessageDashCast:
                ReceiveDashCast(reader);
                break;
            case ServiceZoneId.MessageDashHit:
                ReceiveDashHit(reader);
                break;
            case ServiceZoneId.MessageToolStartCharge:
                ReceiveToolStartCharge(reader);
                break;
            case ServiceZoneId.MessageToolEndCharge:
                ReceiveToolEndCharge(reader);
                break;
            case ServiceZoneId.MessageGroundSlamCast:
                ReceiveGroundSlamCast(reader);
                break;
            case ServiceZoneId.MessageGroundSlamHit:
                ReceiveGroundSlamHit(reader);
                break;
            case ServiceZoneId.MessageStartBuild:
                ReceiveStartBuild(reader);
                break;
            case ServiceZoneId.MessageCancelBuild:
                ReceiveCancelBuild(reader);
                break;
            case ServiceZoneId.MessageCastAbility:
                ReceiveCastAbility(reader);
                break;
            case ServiceZoneId.MessageReceiveCreateProjectile:
                ReceiveCreateProjectile(reader);
                break;
            case ServiceZoneId.MessageReceiveMoveProjectile:
                ReceiveMoveProjectile(reader);
                break;
            case ServiceZoneId.MessageReceiveDropProjectile:
                ReceiveDropProjectile(reader);
                break;
            case ServiceZoneId.MessagePickup:
                ReceivePickup(reader);
                break;
            case ServiceZoneId.MessageUpdateDrown:
                ReceiveUpdateDrown(reader);
                break;
            case ServiceZoneId.MessageFallHit:
                ReceiveFallHit(reader);
                break;
            case ServiceZoneId.MessageEmitZoneEvent:
                ReceiveEmitZoneEvent(reader);
                break;
            case ServiceZoneId.MessageReceivePlayerCommand:
                ReceivePlayerCommand(reader);
                break;
            case ServiceZoneId.MessageSetSpawnPoint:
                ReceiveSetSpawnPoint(reader);
                break;
            case ServiceZoneId.MessageMapTriggerAction:
                ReceiveMapTriggerAction(reader);
                break;
            case ServiceZoneId.MessageStartRecall:
                ReceiveStartRecall(reader);
                break;
            case ServiceZoneId.MessageSurrenderStart:
                ReceiveSurrenderStart(reader);
                break;
            case ServiceZoneId.MessageSurrenderVote:
                ReceiveSurrenderVote(reader);
                break;
            case ServiceZoneId.MessageSetTurretTarget:
                ReceiveSetTurretTarget(reader);
                break;
            case ServiceZoneId.MessageReceiveTurretAttack:
                ReceiveTurretAttack(reader);
                break;
            case ServiceZoneId.MessageReceiveMortarAttack:
                ReceiveMortarAttack(reader);
                break;
            case ServiceZoneId.MessageUpdateTesla:
                ReceiveUpdateTesla(reader);
                break;
            case ServiceZoneId.MessageReceiveDrillAttack:
                ReceiveDrillAttack(reader);
                break;
            case ServiceZoneId.MessageUnitProjectileHit:
                ReceiveUnitProjectileHit(reader);
                break;
            case ServiceZoneId.MessageSkybeamHit:
                ReceiveSkybeamHit(reader);
                break;
            case ServiceZoneId.MessageExecuteMapEditorCommand:
                ReceiveExecuteMapEditorCommand(reader);
                break;
            default:
                Console.WriteLine($"Zone service received unsupported serviceId: {serviceZoneId}");
                return false;
        }
        
        return true;
    }
}