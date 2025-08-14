using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectAddResource : InstEffect
{
    public override InstEffectType Type => InstEffectType.AddResource;

    public float Amount { get; set; }

    public bool Supply { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true, true).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      writer.Write(Amount);
      writer.Write(Supply);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (bitField[3])
        Amount = reader.ReadSingle();
      if (!bitField[4])
        return;
      Supply = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectAddResource value)
    {
      value.Write(writer);
    }

    public static InstEffectAddResource ReadRecord(BinaryReader reader)
    {
      var effectAddResource = new InstEffectAddResource();
      effectAddResource.Read(reader);
      return effectAddResource;
    }
}