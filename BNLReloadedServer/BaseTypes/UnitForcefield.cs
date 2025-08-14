using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitForcefield
{
    public float MaxAmount { get; set; }

    public float RechargeRate { get; set; }

    public float HitRechargeDelay { get; set; }

    public float EmptyRechargeDelay { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true).Write(writer);
      writer.Write(MaxAmount);
      writer.Write(RechargeRate);
      writer.Write(HitRechargeDelay);
      writer.Write(EmptyRechargeDelay);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      if (bitField[0])
        MaxAmount = reader.ReadSingle();
      if (bitField[1])
        RechargeRate = reader.ReadSingle();
      if (bitField[2])
        HitRechargeDelay = reader.ReadSingle();
      if (!bitField[3])
        return;
      EmptyRechargeDelay = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, UnitForcefield value)
    {
      value.Write(writer);
    }

    public static UnitForcefield ReadRecord(BinaryReader reader)
    {
      var unitForcefield = new UnitForcefield();
      unitForcefield.Read(reader);
      return unitForcefield;
    }
}