using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class PlayerCommandsLogic
{
    public int MaxCommandsInInterval { get; set; }

    public float CommandsInterval { get; set; }

    public List<PlayerCommandEntry>? LeftMenu { get; set; }

    public List<PlayerCommandEntry>? RightMenu { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, LeftMenu != null, RightMenu != null).Write(writer);
      writer.Write(MaxCommandsInInterval);
      writer.Write(CommandsInterval);
      if (LeftMenu != null)
        writer.WriteList(LeftMenu, PlayerCommandEntry.WriteRecord);
      if (RightMenu != null)
        writer.WriteList(RightMenu, PlayerCommandEntry.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      if (bitField[0])
        MaxCommandsInInterval = reader.ReadInt32();
      if (bitField[1])
        CommandsInterval = reader.ReadSingle();
      LeftMenu = bitField[2] ? reader.ReadList<PlayerCommandEntry, List<PlayerCommandEntry>>(PlayerCommandEntry.ReadRecord) : null;
      RightMenu = bitField[3] ? reader.ReadList<PlayerCommandEntry, List<PlayerCommandEntry>>(PlayerCommandEntry.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, PlayerCommandsLogic value)
    {
      value.Write(writer);
    }

    public static PlayerCommandsLogic ReadRecord(BinaryReader reader)
    {
      var playerCommandsLogic = new PlayerCommandsLogic();
      playerCommandsLogic.Read(reader);
      return playerCommandsLogic;
    }
}