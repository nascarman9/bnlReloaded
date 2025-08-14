using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitDataPiggyBank : UnitData
{
    public override UnitType Type => UnitType.PiggyBank;

    public float GenerationInterval { get; set; }

    public float ResourcePerInterval { get; set; }

    public bool IsTeamColored { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, true).Write(writer);
      writer.Write(GenerationInterval);
      writer.Write(ResourcePerInterval);
      writer.Write(IsTeamColored);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      if (bitField[0])
        GenerationInterval = reader.ReadSingle();
      if (bitField[1])
        ResourcePerInterval = reader.ReadSingle();
      if (!bitField[2])
        return;
      IsTeamColored = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, UnitDataPiggyBank value)
    {
      value.Write(writer);
    }

    public static UnitDataPiggyBank ReadRecord(BinaryReader reader)
    {
      var unitDataPiggyBank = new UnitDataPiggyBank();
      unitDataPiggyBank.Read(reader);
      return unitDataPiggyBank;
    }
}