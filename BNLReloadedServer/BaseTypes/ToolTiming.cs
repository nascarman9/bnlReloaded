using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ToolTiming
{
    public float? PreAttackTime { get; set; }

    public float AttackTime { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(PreAttackTime.HasValue, true).Write(writer);
        if (PreAttackTime.HasValue)
            writer.Write(PreAttackTime.Value);
        writer.Write(AttackTime);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        PreAttackTime = bitField[0] ? reader.ReadSingle() : null;
        if (!bitField[1])
            return;
        AttackTime = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, ToolTiming value) => value.Write(writer);

    public static ToolTiming ReadRecord(BinaryReader reader)
    {
        var toolTiming = new ToolTiming();
        toolTiming.Read(reader);
        return toolTiming;
    }
}