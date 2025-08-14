using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitDataTurret : UnitData
{
    public override UnitType Type => UnitType.Turret;

    public bool RequiresTarget { get; set; } = true;

    public float AzimuthAngle { get; set; }

    public float ZenithAngle { get; set; }

    public float AttackRange { get; set; }

    public float SpinupTime { get; set; }

    public float PreAttackTime { get; set; }

    public float AttackTime { get; set; }

    public Key? ProjectileKey { get; set; }

    public float? ProjectileSpeed { get; set; }

    public ConeOfFire? ConeOfFire { get; set; }

    public MultipleBullets? Bullets { get; set; }

    public bool PredictTargetMovement { get; set; }

    public float PredictionModifier { get; set; } = 1.5f;

    public InstEffect? HitEffect { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, true, true, ProjectileKey.HasValue, ProjectileSpeed.HasValue,
        ConeOfFire != null, Bullets != null, true, true, HitEffect != null).Write(writer);
      writer.Write(RequiresTarget);
      writer.Write(AzimuthAngle);
      writer.Write(ZenithAngle);
      writer.Write(AttackRange);
      writer.Write(SpinupTime);
      writer.Write(PreAttackTime);
      writer.Write(AttackTime);
      if (ProjectileKey.HasValue)
        Key.WriteRecord(writer, ProjectileKey.Value);
      if (ProjectileSpeed.HasValue)
        writer.Write(ProjectileSpeed.Value);
      if (ConeOfFire != null)
        ConeOfFire.WriteRecord(writer, ConeOfFire);
      if (Bullets != null)
        MultipleBullets.WriteRecord(writer, Bullets);
      writer.Write(PredictTargetMovement);
      writer.Write(PredictionModifier);
      if (HitEffect != null)
        InstEffect.WriteVariant(writer, HitEffect);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(14);
      bitField.Read(reader);
      if (bitField[0])
        RequiresTarget = reader.ReadBoolean();
      if (bitField[1])
        AzimuthAngle = reader.ReadSingle();
      if (bitField[2])
        ZenithAngle = reader.ReadSingle();
      if (bitField[3])
        AttackRange = reader.ReadSingle();
      if (bitField[4])
        SpinupTime = reader.ReadSingle();
      if (bitField[5])
        PreAttackTime = reader.ReadSingle();
      if (bitField[6])
        AttackTime = reader.ReadSingle();
      ProjectileKey = bitField[7] ? Key.ReadRecord(reader) : null;
      ProjectileSpeed = bitField[8] ? reader.ReadSingle() : null;
      ConeOfFire = bitField[9] ? ConeOfFire.ReadRecord(reader) : null;
      Bullets = bitField[10] ? MultipleBullets.ReadRecord(reader) : null;
      if (bitField[11])
        PredictTargetMovement = reader.ReadBoolean();
      if (bitField[12])
        PredictionModifier = reader.ReadSingle();
      HitEffect = bitField[13] ? InstEffect.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, UnitDataTurret value)
    {
      value.Write(writer);
    }

    public static UnitDataTurret ReadRecord(BinaryReader reader)
    {
      var unitDataTurret = new UnitDataTurret();
      unitDataTurret.Read(reader);
      return unitDataTurret;
    }
}