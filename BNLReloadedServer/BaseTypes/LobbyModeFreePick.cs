using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LobbyModeFreePick : LobbyMode
{
    public override LobbyModeType Type => LobbyModeType.FreePick;

    public float SelectionTime { get; set; } = 60f;

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, true).Write(writer);
      writer.Write(PrestartTime);
      writer.Write(ReconnectSelectionTime);
      writer.Write(SelectionTime);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      if (bitField[0])
        PrestartTime = reader.ReadSingle();
      if (bitField[1])
        ReconnectSelectionTime = reader.ReadSingle();
      if (!bitField[2])
        return;
      SelectionTime = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, LobbyModeFreePick value)
    {
      value.Write(writer);
    }

    public static LobbyModeFreePick ReadRecord(BinaryReader reader)
    {
      var lobbyModeFreePick = new LobbyModeFreePick();
      lobbyModeFreePick.Read(reader);
      return lobbyModeFreePick;
    }
}