using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ToolMelee : Tool
{
    public override ToolType Type => ToolType.Melee;

    public float ArcAngle { get; set; }

    public float HeelingAngle { get; set; }

    public float Range { get; set; }

    public float? Bloom { get; set; }

    public MeleeAttackType AttackType { get; set; }

    public InstEffect? HitEffect { get; set; }

    public ToolTiming? Timing { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Ammo != null, true, true, true, true, Bloom.HasValue, true, HitEffect != null, Timing != null).Write(writer);
      if (Ammo != null)
        ToolAmmo.WriteRecord(writer, Ammo);
      writer.Write(AutoSwitch);
      writer.Write(ArcAngle);
      writer.Write(HeelingAngle);
      writer.Write(Range);
      if (Bloom.HasValue)
        writer.Write(Bloom.Value);
      writer.WriteByteEnum(AttackType);
      if (HitEffect != null)
        InstEffect.WriteVariant(writer, HitEffect);
      if (Timing != null)
        ToolTiming.WriteRecord(writer, Timing);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(9);
      bitField.Read(reader);
      Ammo = bitField[0] ? ToolAmmo.ReadRecord(reader) : null;
      if (bitField[1])
        AutoSwitch = reader.ReadBoolean();
      if (bitField[2])
        ArcAngle = reader.ReadSingle();
      if (bitField[3])
        HeelingAngle = reader.ReadSingle();
      if (bitField[4])
        Range = reader.ReadSingle();
      Bloom = bitField[5] ? reader.ReadSingle() : null;
      if (bitField[6])
        AttackType = reader.ReadByteEnum<MeleeAttackType>();
      HitEffect = bitField[7] ? InstEffect.ReadVariant(reader) : null;
      Timing = bitField[8] ? ToolTiming.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ToolMelee value) => value.Write(writer);

    public static ToolMelee ReadRecord(BinaryReader reader)
    {
      var toolMelee = new ToolMelee();
      toolMelee.Read(reader);
      return toolMelee;
    }
}