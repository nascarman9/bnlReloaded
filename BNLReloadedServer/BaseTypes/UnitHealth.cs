using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitHealth
{
    public Health? Health { get; set; }

    public UnitForcefield? Forcefield { get; set; }

    public ResourceReward? KillReward { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Health != null, Forcefield != null, KillReward != null).Write(writer);
      if (Health != null)
        Health.WriteRecord(writer, Health);
      if (Forcefield != null)
        UnitForcefield.WriteRecord(writer, Forcefield);
      if (KillReward == null)
        return;
      ResourceReward.WriteRecord(writer, KillReward);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Health = bitField[0] ? Health.ReadRecord(reader) : null;
      Forcefield = bitField[1] ? UnitForcefield.ReadRecord(reader) : null;
      KillReward = bitField[2] ? ResourceReward.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, UnitHealth value) => value.Write(writer);

    public static UnitHealth ReadRecord(BinaryReader reader)
    {
      var unitHealth = new UnitHealth();
      unitHealth.Read(reader);
      return unitHealth;
    }
}