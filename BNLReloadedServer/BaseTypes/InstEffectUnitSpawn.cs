using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectUnitSpawn : InstEffect
{
    public override InstEffectType Type => InstEffectType.UnitSpawn;

    public Key UnitKey { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      Key.WriteRecord(writer, UnitKey);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (!bitField[3])
        return;
      UnitKey = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectUnitSpawn value)
    {
      value.Write(writer);
    }

    public static InstEffectUnitSpawn ReadRecord(BinaryReader reader)
    {
      var instEffectUnitSpawn = new InstEffectUnitSpawn();
      instEffectUnitSpawn.Read(reader);
      return instEffectUnitSpawn;
    }
}