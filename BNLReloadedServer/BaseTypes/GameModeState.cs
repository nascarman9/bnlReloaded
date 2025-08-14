using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class GameModeState
{
    public bool IsAvailable { get; set; }

    public ulong? NextToggleTime { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, NextToggleTime.HasValue).Write(writer);
        writer.Write(IsAvailable);
        if (!NextToggleTime.HasValue)
            return;
        writer.Write(NextToggleTime.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            IsAvailable = reader.ReadBoolean();
        NextToggleTime = bitField[1] ? reader.ReadUInt64() : null;
    }

    public static void WriteRecord(BinaryWriter writer, GameModeState value) => value.Write(writer);

    public static GameModeState ReadRecord(BinaryReader reader)
    {
        var gameModeState = new GameModeState();
        gameModeState.Read(reader);
        return gameModeState;
    }
}