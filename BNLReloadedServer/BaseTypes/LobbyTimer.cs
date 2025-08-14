using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LobbyTimer
{
    public LobbyTimerType TimerType { get; set; }

    public ulong StartTime { get; set; }

    public ulong EndTime { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true).Write(writer);
        writer.WriteByteEnum(TimerType);
        writer.Write(StartTime);
        writer.Write(EndTime);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            TimerType = reader.ReadByteEnum<LobbyTimerType>();
        if (bitField[1])
            StartTime = reader.ReadUInt64();
        if (!bitField[2])
            return;
        EndTime = reader.ReadUInt64();
    }

    public static void WriteRecord(BinaryWriter writer, LobbyTimer value) => value.Write(writer);

    public static LobbyTimer ReadRecord(BinaryReader reader)
    {
        var lobbyTimer = new LobbyTimer();
        lobbyTimer.Read(reader);
        return lobbyTimer;
    }
}