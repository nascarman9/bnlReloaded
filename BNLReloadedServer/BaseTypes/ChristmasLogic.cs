using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ChristmasLogic
{
    public int MatchCountUnlockReward { get; set; }

    public Key RewardKey { get; set; }

    public DateTime EventBegin { get; set; }

    public DateTime EventEnd { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true).Write(writer);
      writer.Write(MatchCountUnlockReward);
      Key.WriteRecord(writer, RewardKey);
      writer.WriteDateTime(EventBegin);
      writer.WriteDateTime(EventEnd);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      if (bitField[0])
        MatchCountUnlockReward = reader.ReadInt32();
      if (bitField[1])
        RewardKey = Key.ReadRecord(reader);
      if (bitField[2])
        EventBegin = reader.ReadDateTime();
      if (!bitField[3])
        return;
      EventEnd = reader.ReadDateTime();
    }

    public static void WriteRecord(BinaryWriter writer, ChristmasLogic value)
    {
      value.Write(writer);
    }

    public static ChristmasLogic ReadRecord(BinaryReader reader)
    {
      var christmasLogic = new ChristmasLogic();
      christmasLogic.Read(reader);
      return christmasLogic;
    }
}