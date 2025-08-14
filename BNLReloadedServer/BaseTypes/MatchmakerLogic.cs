using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchmakerLogic
{
    public List<Key>? GameModesForQueues { get; set; }

    public float ConfirmTime { get; set; }

    public int AfkCountForDemerit { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(GameModesForQueues != null, true, true).Write(writer);
      if (GameModesForQueues != null)
        writer.WriteList(GameModesForQueues, Key.WriteRecord);
      writer.Write(ConfirmTime);
      writer.Write(AfkCountForDemerit);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      GameModesForQueues = bitField[0] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      if (bitField[1])
        ConfirmTime = reader.ReadSingle();
      if (!bitField[2])
        return;
      AfkCountForDemerit = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, MatchmakerLogic value)
    {
      value.Write(writer);
    }

    public static MatchmakerLogic ReadRecord(BinaryReader reader)
    {
      var matchmakerLogic = new MatchmakerLogic();
      matchmakerLogic.Read(reader);
      return matchmakerLogic;
    }
}