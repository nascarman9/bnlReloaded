using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchmakerState
{
    public MatchmakerStateType State { get; set; }

    public int? PlayersConfirmed { get; set; }

    public ulong? ConfirmationTimeout { get; set; }

    public bool? AbortingByDecline { get; set; }

    public Key? QueueGameMode { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, PlayersConfirmed.HasValue, ConfirmationTimeout.HasValue, AbortingByDecline.HasValue, QueueGameMode.HasValue).Write(writer);
      writer.WriteByteEnum(State);
      if (PlayersConfirmed.HasValue)
        writer.Write(PlayersConfirmed.Value);
      if (ConfirmationTimeout.HasValue)
        writer.Write(ConfirmationTimeout.Value);
      if (AbortingByDecline.HasValue)
        writer.Write(AbortingByDecline.Value);
      if (!QueueGameMode.HasValue)
        return;
      Key.WriteRecord(writer, QueueGameMode.Value);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      if (bitField[0])
        State = reader.ReadByteEnum<MatchmakerStateType>();
      PlayersConfirmed = bitField[1] ? reader.ReadInt32() : null;
      ConfirmationTimeout = bitField[2] ? reader.ReadUInt64() : null;
      AbortingByDecline = bitField[3] ? reader.ReadBoolean() : null;
      QueueGameMode = bitField[4] ? Key.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, MatchmakerState value)
    {
      value.Write(writer);
    }

    public static MatchmakerState ReadRecord(BinaryReader reader)
    {
      var matchmakerState = new MatchmakerState();
      matchmakerState.Read(reader);
      return matchmakerState;
    }
}