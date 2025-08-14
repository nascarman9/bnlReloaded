using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitDataProjectile : UnitData
{
    public override UnitType Type => UnitType.Projectile;

    public float? MovementTimeout { get; set; }

    public float Acceleration { get; set; }

    public float MaxSpeed { get; set; }

    public float TriggerRadius { get; set; }

    public RelativeTeamType CollideWith { get; set; }

    public bool DieOnWorldCollision { get; set; }

    public bool DieOnPlayerCollision { get; set; }

    public InstEffect? WorldCollisionEffect { get; set; }

    public InstEffect? PlayerCollisionEffect { get; set; }

    public InstEffect? DeathEffect { get; set; }

    public ProjectileVisualAttachment? PlayerAttachment { get; set; }

    public ProjectileVisualAttachment? UnitAttachment { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(MovementTimeout.HasValue, true, true, true, true, true, true, WorldCollisionEffect != null,
        PlayerCollisionEffect != null, DeathEffect != null, PlayerAttachment != null,
        UnitAttachment != null).Write(writer);
      if (MovementTimeout.HasValue)
        writer.Write(MovementTimeout.Value);
      writer.Write(Acceleration);
      writer.Write(MaxSpeed);
      writer.Write(TriggerRadius);
      writer.WriteByteEnum(CollideWith);
      writer.Write(DieOnWorldCollision);
      writer.Write(DieOnPlayerCollision);
      if (WorldCollisionEffect != null)
        InstEffect.WriteVariant(writer, WorldCollisionEffect);
      if (PlayerCollisionEffect != null)
        InstEffect.WriteVariant(writer, PlayerCollisionEffect);
      if (DeathEffect != null)
        InstEffect.WriteVariant(writer, DeathEffect);
      if (PlayerAttachment != null)
        ProjectileVisualAttachment.WriteRecord(writer, PlayerAttachment);
      if (UnitAttachment == null)
        return;
      ProjectileVisualAttachment.WriteRecord(writer, UnitAttachment);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(12);
      bitField.Read(reader);
      MovementTimeout = bitField[0] ? reader.ReadSingle() : null;
      if (bitField[1])
        Acceleration = reader.ReadSingle();
      if (bitField[2])
        MaxSpeed = reader.ReadSingle();
      if (bitField[3])
        TriggerRadius = reader.ReadSingle();
      if (bitField[4])
        CollideWith = reader.ReadByteEnum<RelativeTeamType>();
      if (bitField[5])
        DieOnWorldCollision = reader.ReadBoolean();
      if (bitField[6])
        DieOnPlayerCollision = reader.ReadBoolean();
      WorldCollisionEffect = bitField[7] ? InstEffect.ReadVariant(reader) : null;
      PlayerCollisionEffect = bitField[8] ? InstEffect.ReadVariant(reader) : null;
      DeathEffect = bitField[9] ? InstEffect.ReadVariant(reader) : null;
      PlayerAttachment = bitField[10] ? ProjectileVisualAttachment.ReadRecord(reader) : null;
      UnitAttachment = bitField[11] ? ProjectileVisualAttachment.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, UnitDataProjectile value)
    {
      value.Write(writer);
    }

    public static UnitDataProjectile ReadRecord(BinaryReader reader)
    {
      var unitDataProjectile = new UnitDataProjectile();
      unitDataProjectile.Read(reader);
      return unitDataProjectile;
    }
}