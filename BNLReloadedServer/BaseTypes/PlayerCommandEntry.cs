using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class PlayerCommandEntry
{
    public LocalizedString? Title { get; set; }

    public Key? CommandKey { get; set; }

    public List<PlayerCommandEntry>? Subcommands { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Title != null, CommandKey.HasValue, Subcommands != null).Write(writer);
      if (Title != null)
        LocalizedString.WriteRecord(writer, Title);
      if (CommandKey.HasValue)
        Key.WriteRecord(writer, CommandKey.Value);
      if (Subcommands != null)
        writer.WriteList(Subcommands, WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Title = bitField[0] ? LocalizedString.ReadRecord(reader) : null;
      CommandKey = bitField[1] ? Key.ReadRecord(reader) : null;
      Subcommands = bitField[2] ? reader.ReadList<PlayerCommandEntry, List<PlayerCommandEntry>>(ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, PlayerCommandEntry value)
    {
      value.Write(writer);
    }

    public static PlayerCommandEntry ReadRecord(BinaryReader reader)
    {
      var playerCommandEntry = new PlayerCommandEntry();
      playerCommandEntry.Read(reader);
      return playerCommandEntry;
    }
}