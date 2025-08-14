using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class AimAssistInfo
{
    public bool IsMelee { get; set; }

    public float? Range { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, Range.HasValue).Write(writer);
        writer.Write(IsMelee);
        if (!Range.HasValue)
            return;
        writer.Write(Range.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            IsMelee = reader.ReadBoolean();
        Range = bitField[1] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, AimAssistInfo value) => value.Write(writer);

    public static AimAssistInfo ReadRecord(BinaryReader reader)
    {
        var aimAssistInfo = new AimAssistInfo();
        aimAssistInfo.Read(reader);
        return aimAssistInfo;
    }
}