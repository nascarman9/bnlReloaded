using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LobbyUpdate
{
    public Key? MatchMode { get; set; }

    public Key? GameMode { get; set; }

    public List<LobbyMapData>? Maps { get; set; }

    public bool? Started { get; set; }

    public LobbyTimer? Timer { get; set; }

    public List<PlayerLobbyState>? Players { get; set; }

    public Dictionary<TeamType, List<uint>>? RequeuePlayers { get; set; }

    public Dictionary<TeamType, LobbyTimer>? RequeueTimers { get; set; }

    public string? SessionName { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(MatchMode.HasValue, GameMode.HasValue, Maps != null, Started.HasValue, Timer != null,
        Players != null, RequeuePlayers != null, RequeueTimers != null, SessionName != null).Write(writer);
      if (MatchMode.HasValue)
        Key.WriteRecord(writer, MatchMode.Value);
      if (GameMode.HasValue)
        Key.WriteRecord(writer, GameMode.Value);
      if (Maps != null)
        writer.WriteList(Maps, LobbyMapData.WriteRecord);
      if (Started.HasValue)
        writer.Write(Started.Value);
      if (Timer != null)
        LobbyTimer.WriteRecord(writer, Timer);
      if (Players != null)
        writer.WriteList(Players, PlayerLobbyState.WriteRecord);
      if (RequeuePlayers != null)
        writer.WriteMap(RequeuePlayers, writer.WriteByteEnum, item => writer.WriteList(item, writer.Write));
      if (RequeueTimers != null)
        writer.WriteMap(RequeueTimers, writer.WriteByteEnum, LobbyTimer.WriteRecord);
      if (SessionName == null)
        return;
      writer.Write(SessionName);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(9);
      bitField.Read(reader);
      MatchMode = bitField[0] ? Key.ReadRecord(reader) : null;
      GameMode = bitField[1] ? Key.ReadRecord(reader) : null;
      Maps = bitField[2] ? reader.ReadList<LobbyMapData, List<LobbyMapData>>(LobbyMapData.ReadRecord) : null;
      Started = bitField[3] ? reader.ReadBoolean() : null;
      Timer = bitField[4] ? LobbyTimer.ReadRecord(reader) : null;
      Players = bitField[5] ? reader.ReadList<PlayerLobbyState, List<PlayerLobbyState>>(PlayerLobbyState.ReadRecord) : null;
      RequeuePlayers = bitField[6] ? reader.ReadMap<TeamType, List<uint>, Dictionary<TeamType, List<uint>>>(reader.ReadByteEnum<TeamType>, () => reader.ReadList<uint, List<uint>>(reader.ReadUInt32)) : null;
      RequeueTimers = bitField[7] ? reader.ReadMap<TeamType, LobbyTimer, Dictionary<TeamType, LobbyTimer>>(reader.ReadByteEnum<TeamType>, LobbyTimer.ReadRecord) : null;
      SessionName = bitField[8] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, LobbyUpdate value) => value.Write(writer);

    public static LobbyUpdate ReadRecord(BinaryReader reader)
    {
      var lobbyUpdate = new LobbyUpdate();
      lobbyUpdate.Read(reader);
      return lobbyUpdate;
    }
}