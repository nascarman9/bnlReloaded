using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ToolDash : Tool
{
    public override ToolType Type => ToolType.Dash;

    public float MinChargeTime { get; set; }

    public float MaxChargeTime { get; set; }

    public bool ChargeRoot { get; set; }

    public bool CanHoldCharge { get; set; }

    public bool HitWithoutCollision { get; set; }

    public float MinAmmoRateMultiplier { get; set; }

    public float MaxAmmoRateMultiplier { get; set; }

    public float MinDashDistance { get; set; }

    public float MaxDashDistance { get; set; }

    public float MinDashTime { get; set; }

    public float MaxDashTime { get; set; }

    public InstEffect? HitEffect { get; set; }

    public InstEffect? MaxHitEffect { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Ammo != null, true, true, true, true, true, true, true, true, true, true, true, true,
        HitEffect != null, MaxHitEffect != null).Write(writer);
      if (Ammo != null)
        ToolAmmo.WriteRecord(writer, Ammo);
      writer.Write(AutoSwitch);
      writer.Write(MinChargeTime);
      writer.Write(MaxChargeTime);
      writer.Write(ChargeRoot);
      writer.Write(CanHoldCharge);
      writer.Write(HitWithoutCollision);
      writer.Write(MinAmmoRateMultiplier);
      writer.Write(MaxAmmoRateMultiplier);
      writer.Write(MinDashDistance);
      writer.Write(MaxDashDistance);
      writer.Write(MinDashTime);
      writer.Write(MaxDashTime);
      if (HitEffect != null)
        InstEffect.WriteVariant(writer, HitEffect);
      if (MaxHitEffect == null)
        return;
      InstEffect.WriteVariant(writer, MaxHitEffect);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(15);
      bitField.Read(reader);
      Ammo = bitField[0] ? ToolAmmo.ReadRecord(reader) : null;
      if (bitField[1])
        AutoSwitch = reader.ReadBoolean();
      if (bitField[2])
        MinChargeTime = reader.ReadSingle();
      if (bitField[3])
        MaxChargeTime = reader.ReadSingle();
      if (bitField[4])
        ChargeRoot = reader.ReadBoolean();
      if (bitField[5])
        CanHoldCharge = reader.ReadBoolean();
      if (bitField[6])
        HitWithoutCollision = reader.ReadBoolean();
      if (bitField[7])
        MinAmmoRateMultiplier = reader.ReadSingle();
      if (bitField[8])
        MaxAmmoRateMultiplier = reader.ReadSingle();
      if (bitField[9])
        MinDashDistance = reader.ReadSingle();
      if (bitField[10])
        MaxDashDistance = reader.ReadSingle();
      if (bitField[11])
        MinDashTime = reader.ReadSingle();
      if (bitField[12])
        MaxDashTime = reader.ReadSingle();
      HitEffect = bitField[13] ? InstEffect.ReadVariant(reader) : null;
      MaxHitEffect = bitField[14] ? InstEffect.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ToolDash value) => value.Write(writer);

    public static ToolDash ReadRecord(BinaryReader reader)
    {
      var toolDash = new ToolDash();
      toolDash.Read(reader);
      return toolDash;
    }
}