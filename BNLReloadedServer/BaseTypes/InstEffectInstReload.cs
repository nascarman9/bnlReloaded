using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectInstReload : InstEffect
{
  public override InstEffectType Type => InstEffectType.InstReload;

    public bool AllWeapons { get; set; }

    public float ClipPart { get; set; } = 1f;

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true, true).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      writer.Write(AllWeapons);
      writer.Write(ClipPart);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (bitField[3])
        AllWeapons = reader.ReadBoolean();
      if (!bitField[4])
        return;
      ClipPart = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectInstReload value)
    {
      value.Write(writer);
    }

    public static InstEffectInstReload ReadRecord(BinaryReader reader)
    {
      var effectInstReload = new InstEffectInstReload();
      effectInstReload.Read(reader);
      return effectInstReload;
    }
}