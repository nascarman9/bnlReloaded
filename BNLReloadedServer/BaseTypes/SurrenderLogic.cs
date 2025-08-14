using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SurrenderLogic
{
    public float TimeBeforeSurrender { get; set; }

    public float VotingTime { get; set; }

    public float TimeBetweenVoting { get; set; }

    public Dictionary<int, int>? MinVotes { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, MinVotes != null).Write(writer);
      writer.Write(TimeBeforeSurrender);
      writer.Write(VotingTime);
      writer.Write(TimeBetweenVoting);
      if (MinVotes != null)
        writer.WriteMap(MinVotes, writer.Write, writer.Write);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      if (bitField[0])
        TimeBeforeSurrender = reader.ReadSingle();
      if (bitField[1])
        VotingTime = reader.ReadSingle();
      if (bitField[2])
        TimeBetweenVoting = reader.ReadSingle();
      MinVotes = bitField[3] ? reader.ReadMap<int, int, Dictionary<int, int>>(reader.ReadInt32, reader.ReadInt32) : null;
    }

    public static void WriteRecord(BinaryWriter writer, SurrenderLogic value)
    {
      value.Write(writer);
    }

    public static SurrenderLogic ReadRecord(BinaryReader reader)
    {
      var surrenderLogic = new SurrenderLogic();
      surrenderLogic.Read(reader);
      return surrenderLogic;
    }
}