using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchMedalsLogic
{
    public List<Key>? PositiveMedals { get; set; }

    public List<Key>? NegativeMedals { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(PositiveMedals != null, NegativeMedals != null).Write(writer);
      if (PositiveMedals != null)
        writer.WriteList(PositiveMedals, Key.WriteRecord);
      if (NegativeMedals != null)
        writer.WriteList(NegativeMedals, Key.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      PositiveMedals = bitField[0] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      NegativeMedals = bitField[1] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, MatchMedalsLogic value)
    {
      value.Write(writer);
    }

    public static MatchMedalsLogic ReadRecord(BinaryReader reader)
    {
      var matchMedalsLogic = new MatchMedalsLogic();
      matchMedalsLogic.Read(reader);
      return matchMedalsLogic;
    }
}