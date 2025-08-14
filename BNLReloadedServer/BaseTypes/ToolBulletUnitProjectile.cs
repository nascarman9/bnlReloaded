using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ToolBulletUnitProjectile : ToolBullet
{
    public override float GetSpeed(float holdInterval)
    {
      var t = !HoldInterval.HasValue ? 0.0f : Math.Clamp(holdInterval / HoldInterval.Value, 0, 1);
      return MaxSpeed.HasValue ? float.Lerp(MinSpeed, MaxSpeed.Value, t) : MinSpeed;
    }

    public override float? GetHoldInterval() => HoldInterval;

    public override ToolBulletType Type => ToolBulletType.UnitProjectile;

    public Key UnitProjectileKey { get; set; }

    public float MinSpeed { get; set; }

    public float? MaxSpeed { get; set; }

    public float? HoldInterval { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, MaxSpeed.HasValue, HoldInterval.HasValue).Write(writer);
      Key.WriteRecord(writer, UnitProjectileKey);
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
        UnitProjectileKey = Key.ReadRecord(reader);
      if (bitField[1])
        MinSpeed = reader.ReadSingle();
      MaxSpeed = bitField[2] ? reader.ReadSingle() : null;
      HoldInterval = bitField[3] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, ToolBulletUnitProjectile value)
    {
      value.Write(writer);
    }

    public static ToolBulletUnitProjectile ReadRecord(BinaryReader reader)
    {
      var bulletUnitProjectile = new ToolBulletUnitProjectile();
      bulletUnitProjectile.Read(reader);
      return bulletUnitProjectile;
    }
}