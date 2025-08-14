using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectBlocksSpawn : InstEffect
{
    public override InstEffectType Type => InstEffectType.BlocksSpawn;

    public BlocksPattern? Pattern { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, Pattern != null).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      if (Pattern != null)
        BlocksPattern.WriteVariant(writer, Pattern);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      Pattern = bitField[3] ? BlocksPattern.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectBlocksSpawn value)
    {
      value.Write(writer);
    }

    public static InstEffectBlocksSpawn ReadRecord(BinaryReader reader)
    {
      var effectBlocksSpawn = new InstEffectBlocksSpawn();
      effectBlocksSpawn.Read(reader);
      return effectBlocksSpawn;
    }
}