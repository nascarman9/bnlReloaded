using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectReplaceBlocks : InstEffect
{
    public override InstEffectType Type => InstEffectType.ReplaceBlocks;

    public Key ReplaceWith { get; set; }

    public float Range { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true, true).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      Key.WriteRecord(writer, ReplaceWith);
      writer.Write(Range);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (bitField[3])
        ReplaceWith = Key.ReadRecord(reader);
      if (!bitField[4])
        return;
      Range = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectReplaceBlocks value)
    {
      value.Write(writer);
    }

    public static InstEffectReplaceBlocks ReadRecord(BinaryReader reader)
    {
      var effectReplaceBlocks = new InstEffectReplaceBlocks();
      effectReplaceBlocks.Read(reader);
      return effectReplaceBlocks;
    }
}