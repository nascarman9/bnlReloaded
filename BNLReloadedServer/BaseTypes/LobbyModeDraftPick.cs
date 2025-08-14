using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LobbyModeDraftPick : LobbyMode
{
    public override LobbyModeType Type => LobbyModeType.DraftPick;

    public float WaitTime { get; set; } = 10f;

    public float SelectionTime { get; set; } = 30f;

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true).Write(writer);
      writer.Write(PrestartTime);
      writer.Write(ReconnectSelectionTime);
      writer.Write(WaitTime);
      writer.Write(SelectionTime);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      if (bitField[0])
        PrestartTime = reader.ReadSingle();
      if (bitField[1])
        ReconnectSelectionTime = reader.ReadSingle();
      if (bitField[2])
        WaitTime = reader.ReadSingle();
      if (!bitField[3])
        return;
      SelectionTime = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, LobbyModeDraftPick value)
    {
      value.Write(writer);
    }

    public static LobbyModeDraftPick ReadRecord(BinaryReader reader)
    {
      var lobbyModeDraftPick = new LobbyModeDraftPick();
      lobbyModeDraftPick.Read(reader);
      return lobbyModeDraftPick;
    }
}