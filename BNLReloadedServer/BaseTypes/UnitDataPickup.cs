using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitDataPickup : UnitData
{
    public override UnitType Type => UnitType.Pickup;

    public bool PickupOnUse { get; set; }

    public float FallSpeed { get; set; }

    public float? Timeout { get; set; }

    public string? ScreenEffect { get; set; }

    public InstEffect? TakeEffect { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, Timeout.HasValue, ScreenEffect != null, TakeEffect != null).Write(writer);
      writer.Write(PickupOnUse);
      writer.Write(FallSpeed);
      if (Timeout.HasValue)
        writer.Write(Timeout.Value);
      if (ScreenEffect != null)
        writer.Write(ScreenEffect);
      if (TakeEffect == null)
        return;
      InstEffect.WriteVariant(writer, TakeEffect);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      if (bitField[0])
        PickupOnUse = reader.ReadBoolean();
      if (bitField[1])
        FallSpeed = reader.ReadSingle();
      Timeout = bitField[2] ? reader.ReadSingle() : null;
      ScreenEffect = bitField[3] ? reader.ReadString() : null;
      TakeEffect = bitField[4] ? InstEffect.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, UnitDataPickup value)
    {
      value.Write(writer);
    }

    public static UnitDataPickup ReadRecord(BinaryReader reader)
    {
      var unitDataPickup = new UnitDataPickup();
      unitDataPickup.Read(reader);
      return unitDataPickup;
    }
}