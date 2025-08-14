using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ProjectileBehaviourRocket : ProjectileBehaviour
{
    public override ProjectileBehaviourType Type => ProjectileBehaviourType.Rocket;

    public float TriggerRadius { get; set; }

    public float? MaxSpeed { get; set; }

    public float? Acceleration { get; set; }

    public float? Drag { get; set; }

    public float? Gravity { get; set; }

    public bool? NoPenetration { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Timeout.HasValue, true, true, true, MaxSpeed.HasValue, Acceleration.HasValue, Drag.HasValue,
        Gravity.HasValue, NoPenetration.HasValue).Write(writer);
      if (Timeout.HasValue)
        writer.Write(Timeout.Value);
      writer.Write(HitOnTimeout);
      writer.WriteByteEnum(CollideWith);
      writer.Write(TriggerRadius);
      if (MaxSpeed.HasValue)
        writer.Write(MaxSpeed.Value);
      if (Acceleration.HasValue)
        writer.Write(Acceleration.Value);
      if (Drag.HasValue)
        writer.Write(Drag.Value);
      if (Gravity.HasValue)
        writer.Write(Gravity.Value);
      if (!NoPenetration.HasValue)
        return;
      writer.Write(NoPenetration.Value);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(9);
      bitField.Read(reader);
      Timeout = bitField[0] ? reader.ReadSingle() : null;
      if (bitField[1])
        HitOnTimeout = reader.ReadBoolean();
      if (bitField[2])
        CollideWith = reader.ReadByteEnum<RelativeTeamType>();
      if (bitField[3])
        TriggerRadius = reader.ReadSingle();
      MaxSpeed = bitField[4] ? reader.ReadSingle() : null;
      Acceleration = bitField[5] ? reader.ReadSingle() : null;
      Drag = bitField[6] ? reader.ReadSingle() : null;
      Gravity = bitField[7] ? reader.ReadSingle() : null;
      NoPenetration = bitField[8] ? reader.ReadBoolean() : null;
    }

    public static void WriteRecord(BinaryWriter writer, ProjectileBehaviourRocket value)
    {
      value.Write(writer);
    }

    public static ProjectileBehaviourRocket ReadRecord(BinaryReader reader)
    {
      var projectileBehaviourRocket = new ProjectileBehaviourRocket();
      projectileBehaviourRocket.Read(reader);
      return projectileBehaviourRocket;
    }
}