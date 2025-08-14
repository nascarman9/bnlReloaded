using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public abstract class ZoneEvent
{
    public abstract ZoneEventType Type { get; }

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, ZoneEvent value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static ZoneEvent ReadVariant(BinaryReader reader)
    {
        var zoneEvent = Create(reader.ReadByteEnum<ZoneEventType>());
        zoneEvent.Read(reader);
        return zoneEvent;
    }

    public static ZoneEvent Create(ZoneEventType type)
    {
        return type switch
        {
            ZoneEventType.UnitCommonLand => new ZoneEventUnitCommonLand(),
            ZoneEventType.SpeedPadUsed => new ZoneEventSpeedPadUsed(),
            ZoneEventType.JumpPadUsed => new ZoneEventJumpPadUsed(),
            ZoneEventType.DoubleJump => new ZoneEventDoubleJump(),
            ZoneEventType.ForceFall => new ZoneEventForceFall(),
            ZoneEventType.ToolFire => new ZoneEventToolFire(),
            ZoneEventType.ToolFireLoop => new ZoneEventToolFireLoop(),
            ZoneEventType.ToolHold => new ZoneEventToolHold(),
            ZoneEventType.TurretFire => new ZoneEventTurretFire(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}