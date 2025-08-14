using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectHeal : InstEffect
{
    public override InstEffectType Type => InstEffectType.Heal;

    public float PlayerHeal { get; set; }

    public float WorldHeal { get; set; }

    public float ObjectiveHeal { get; set; }

    public float ForcefieldAmount { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true, true, true, true).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      writer.Write(PlayerHeal);
      writer.Write(WorldHeal);
      writer.Write(ObjectiveHeal);
      writer.Write(ForcefieldAmount);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (bitField[3])
        PlayerHeal = reader.ReadSingle();
      if (bitField[4])
        WorldHeal = reader.ReadSingle();
      if (bitField[5])
        ObjectiveHeal = reader.ReadSingle();
      if (!bitField[6])
        return;
      ForcefieldAmount = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectHeal value)
    {
      value.Write(writer);
    }

    public static InstEffectHeal ReadRecord(BinaryReader reader)
    {
      var instEffectHeal = new InstEffectHeal();
      instEffectHeal.Read(reader);
      return instEffectHeal;
    }
}