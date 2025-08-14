using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class DodgePenalty
{
    public string? MessageTag { get; set; }

    public int PenaltyMinutes { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(MessageTag != null, true).Write(writer);
        if (MessageTag != null)
            writer.Write(MessageTag);
        writer.Write(PenaltyMinutes);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        MessageTag = bitField[0] ? reader.ReadString() : null;
        if (!bitField[1])
            return;
        PenaltyMinutes = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, DodgePenalty value) => value.Write(writer);

    public static DodgePenalty ReadRecord(BinaryReader reader)
    {
        var dodgePenalty = new DodgePenalty();
        dodgePenalty.Read(reader);
        return dodgePenalty;
    }
}