using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitDataDamageCapture : UnitData
{
    public override UnitType Type => UnitType.DamageCapture;

    public UnitLabel CapturerLabel { get; set; }

    public Dictionary<int, float>? DamagePerCapturer { get; set; }

    public Vector3 CaptureZone { get; set; }

    public List<Key>? ZoneEffects { get; set; }

    public Key? DamageImpact { get; set; }

    public Key DamageSource { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, DamagePerCapturer != null, true, ZoneEffects != null, DamageImpact.HasValue, true).Write(writer);
      writer.WriteByteEnum(CapturerLabel);
      if (DamagePerCapturer != null)
        writer.WriteMap(DamagePerCapturer, writer.Write, writer.Write);
      writer.Write(CaptureZone);
      if (ZoneEffects != null)
        writer.WriteList(ZoneEffects, Key.WriteRecord);
      if (DamageImpact.HasValue)
        Key.WriteRecord(writer, DamageImpact.Value);
      Key.WriteRecord(writer, DamageSource);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      if (bitField[0])
        CapturerLabel = reader.ReadByteEnum<UnitLabel>();
      DamagePerCapturer = bitField[1] ? reader.ReadMap<int, float, Dictionary<int, float>>(reader.ReadInt32, reader.ReadSingle) : null;
      if (bitField[2])
        CaptureZone = reader.ReadVector3();
      ZoneEffects = bitField[3] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      DamageImpact = bitField[4] ? Key.ReadRecord(reader) : null;
      if (!bitField[5])
        return;
      DamageSource = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, UnitDataDamageCapture value)
    {
      value.Write(writer);
    }

    public static UnitDataDamageCapture ReadRecord(BinaryReader reader)
    {
      var dataDamageCapture = new UnitDataDamageCapture();
      dataDamageCapture.Read(reader);
      return dataDamageCapture;
    }
}