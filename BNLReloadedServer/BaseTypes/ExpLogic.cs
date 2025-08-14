using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ExpLogic
{
    public float FlatCoeff { get; set; }

    public float MultCoeff { get; set; }

    public float PowerCoeff { get; set; }

    public int? MaxLevel { get; set; }

    public Dictionary<int, LevelReward>? LevelRewards { get; set; }

    public Dictionary<int, LevelReward>? EveryLevelRewards { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, MaxLevel.HasValue, LevelRewards != null, EveryLevelRewards != null).Write(writer);
      writer.Write(FlatCoeff);
      writer.Write(MultCoeff);
      writer.Write(PowerCoeff);
      if (MaxLevel.HasValue)
        writer.Write(MaxLevel.Value);
      if (LevelRewards != null)
        writer.WriteMap(LevelRewards, writer.Write, LevelReward.WriteVariant);
      if (EveryLevelRewards != null)
        writer.WriteMap(EveryLevelRewards, writer.Write, LevelReward.WriteVariant);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      if (bitField[0])
        FlatCoeff = reader.ReadSingle();
      if (bitField[1])
        MultCoeff = reader.ReadSingle();
      if (bitField[2])
        PowerCoeff = reader.ReadSingle();
      MaxLevel = bitField[3] ? reader.ReadInt32() : null;
      LevelRewards = bitField[4] ? reader.ReadMap<int, LevelReward, Dictionary<int, LevelReward>>(reader.ReadInt32, LevelReward.ReadVariant) : null;
      EveryLevelRewards = bitField[5] ? reader.ReadMap<int, LevelReward, Dictionary<int, LevelReward>>(reader.ReadInt32, LevelReward.ReadVariant) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ExpLogic value) => value.Write(writer);

    public static ExpLogic ReadRecord(BinaryReader reader)
    {
      var expLogic = new ExpLogic();
      expLogic.Read(reader);
      return expLogic;
    }
}