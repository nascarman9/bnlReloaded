using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectSlip : InstEffect
{
    public override InstEffectType Type => InstEffectType.Slip;

    public float DistanceFrom { get; set; }

    public float DistanceTo { get; set; }

    public float Time { get; set; }

    public float RotationTime { get; set; }

    public float DirectionAngleRandom { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true, true, true, true, true).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      writer.Write(DistanceFrom);
      writer.Write(DistanceTo);
      writer.Write(Time);
      writer.Write(RotationTime);
      writer.Write(DirectionAngleRandom);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(8);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (bitField[3])
        DistanceFrom = reader.ReadSingle();
      if (bitField[4])
        DistanceTo = reader.ReadSingle();
      if (bitField[5])
        Time = reader.ReadSingle();
      if (bitField[6])
        RotationTime = reader.ReadSingle();
      if (!bitField[7])
        return;
      DirectionAngleRandom = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectSlip value)
    {
      value.Write(writer);
    }

    public static InstEffectSlip ReadRecord(BinaryReader reader)
    {
      var instEffectSlip = new InstEffectSlip();
      instEffectSlip.Read(reader);
      return instEffectSlip;
    }
}