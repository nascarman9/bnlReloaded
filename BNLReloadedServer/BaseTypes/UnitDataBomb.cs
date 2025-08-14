using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitDataBomb : UnitData
{
    public override UnitType Type => UnitType.Bomb;

    public float Timeout { get; set; }

    public bool TriggerOnDamage { get; set; }

    public bool TriggerOnBlockDestroyed { get; set; }

    public InstEffect? TriggerEffect { get; set; }

    public float? FallTimerResetHeightLimit { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, TriggerEffect != null, FallTimerResetHeightLimit.HasValue).Write(writer);
      writer.Write(Timeout);
      writer.Write(TriggerOnDamage);
      writer.Write(TriggerOnBlockDestroyed);
      if (TriggerEffect != null)
        InstEffect.WriteVariant(writer, TriggerEffect);
      if (!FallTimerResetHeightLimit.HasValue)
        return;
      writer.Write(FallTimerResetHeightLimit.Value);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      if (bitField[0])
        Timeout = reader.ReadSingle();
      if (bitField[1])
        TriggerOnDamage = reader.ReadBoolean();
      if (bitField[2])
        TriggerOnBlockDestroyed = reader.ReadBoolean();
      TriggerEffect = bitField[3] ? InstEffect.ReadVariant(reader) : null;
      FallTimerResetHeightLimit = bitField[4] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, UnitDataBomb value) => value.Write(writer);

    public static UnitDataBomb ReadRecord(BinaryReader reader)
    {
      var unitDataBomb = new UnitDataBomb();
      unitDataBomb.Read(reader);
      return unitDataBomb;
    }
}