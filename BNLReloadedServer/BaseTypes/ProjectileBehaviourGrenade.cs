using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ProjectileBehaviourGrenade : ProjectileBehaviour
{
    public override ProjectileBehaviourType Type => ProjectileBehaviourType.Grenade;

    public ProjectileCollisionMask? CollisionMask { get; set; }

    public string? CustomPhysMaterial { get; set; }

    public bool StopOnCollision { get; set; }

    public bool RandomTorque { get; set; }

    public float TriggerRadius { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Timeout.HasValue, true, true, CollisionMask != null, CustomPhysMaterial != null, true, true, true).Write(writer);
      if (Timeout.HasValue)
        writer.Write(Timeout.Value);
      writer.Write(HitOnTimeout);
      writer.WriteByteEnum(CollideWith);
      if (CollisionMask != null)
        ProjectileCollisionMask.WriteRecord(writer, CollisionMask);
      if (CustomPhysMaterial != null)
        writer.Write(CustomPhysMaterial);
      writer.Write(StopOnCollision);
      writer.Write(RandomTorque);
      writer.Write(TriggerRadius);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(8);
      bitField.Read(reader);
      Timeout = bitField[0] ? reader.ReadSingle() : null;
      if (bitField[1])
        HitOnTimeout = reader.ReadBoolean();
      if (bitField[2])
        CollideWith = reader.ReadByteEnum<RelativeTeamType>();
      CollisionMask = bitField[3] ? ProjectileCollisionMask.ReadRecord(reader) : null;
      CustomPhysMaterial = bitField[4] ? reader.ReadString() : null;
      if (bitField[5])
        StopOnCollision = reader.ReadBoolean();
      if (bitField[6])
        RandomTorque = reader.ReadBoolean();
      if (!bitField[7])
        return;
      TriggerRadius = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, ProjectileBehaviourGrenade value)
    {
      value.Write(writer);
    }

    public static ProjectileBehaviourGrenade ReadRecord(BinaryReader reader)
    {
      var behaviourGrenade = new ProjectileBehaviourGrenade();
      behaviourGrenade.Read(reader);
      return behaviourGrenade;
    }
}