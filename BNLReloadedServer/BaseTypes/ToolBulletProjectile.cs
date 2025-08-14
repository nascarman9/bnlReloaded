using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ToolBulletProjectile : ToolBullet
{
    public override float GetSpeed(float holdInterval)
    {
      var t = !HoldInterval.HasValue ? 0.0f : Math.Clamp(holdInterval / HoldInterval.Value, 0, 1);
      return MaxSpeed.HasValue ? float.Lerp(MinSpeed, MaxSpeed.Value, t) : MinSpeed;
    }

    public override float? GetHoldInterval() => HoldInterval;

    public override ToolBulletType Type => ToolBulletType.Projectile;

    public Key ProjectileKey { get; set; }

    public float MinSpeed { get; set; }

    public float? MaxSpeed { get; set; }

    public float? HoldInterval { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, MaxSpeed.HasValue, HoldInterval.HasValue).Write(writer);
      Key.WriteRecord(writer, ProjectileKey);
      writer.Write(MinSpeed);
      if (MaxSpeed.HasValue)
        writer.Write(MaxSpeed.Value);
      if (!HoldInterval.HasValue)
        return;
      writer.Write(HoldInterval.Value);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      if (bitField[0])
        ProjectileKey = Key.ReadRecord(reader);
      if (bitField[1])
        MinSpeed = reader.ReadSingle();
      MaxSpeed = bitField[2] ? reader.ReadSingle() : null;
      HoldInterval = bitField[3] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, ToolBulletProjectile value)
    {
      value.Write(writer);
    }

    public static ToolBulletProjectile ReadRecord(BinaryReader reader)
    {
      var bulletProjectile = new ToolBulletProjectile();
      bulletProjectile.Read(reader);
      return bulletProjectile;
    }
}