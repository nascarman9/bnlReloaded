using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ChatOffensive
{
    public List<string>? OffensiveWords { get; set; }

    public List<string>? OffensiveWordsRegexp { get; set; }

    public List<string>? OffensiveNamesStartsEnds { get; set; }

    public List<string>? OffensiveNamesContains { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(OffensiveWords != null, OffensiveWordsRegexp != null, OffensiveNamesStartsEnds != null,
        OffensiveNamesContains != null).Write(writer);
      if (OffensiveWords != null)
        writer.WriteList(OffensiveWords, writer.Write);
      if (OffensiveWordsRegexp != null)
        writer.WriteList(OffensiveWordsRegexp, writer.Write);
      if (OffensiveNamesStartsEnds != null)
        writer.WriteList(OffensiveNamesStartsEnds, writer.Write);
      if (OffensiveNamesContains != null)
        writer.WriteList(OffensiveNamesContains, writer.Write);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      OffensiveWords = bitField[0] ? reader.ReadList<string, List<string>>(reader.ReadString) : null;
      OffensiveWordsRegexp = bitField[1] ? reader.ReadList<string, List<string>>(reader.ReadString) : null;
      OffensiveNamesStartsEnds = bitField[2] ? reader.ReadList<string, List<string>>(reader.ReadString) : null;
      OffensiveNamesContains = bitField[3] ? reader.ReadList<string, List<string>>(reader.ReadString) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ChatOffensive value) => value.Write(writer);

    public static ChatOffensive ReadRecord(BinaryReader reader)
    {
      var chatOffensive = new ChatOffensive();
      chatOffensive.Read(reader);
      return chatOffensive;
    }
}