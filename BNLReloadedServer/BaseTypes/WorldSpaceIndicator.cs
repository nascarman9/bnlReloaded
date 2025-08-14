using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class WorldSpaceIndicator
{
    public string? Icon { get; set; }

    public float? DisplayRange { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(Icon != null, DisplayRange.HasValue).Write(writer);
        if (Icon != null)
            writer.Write(Icon);
        if (!DisplayRange.HasValue)
            return;
        writer.Write(DisplayRange.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        Icon = bitField[0] ? reader.ReadString() : null;
        DisplayRange = bitField[1] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, WorldSpaceIndicator value)
    {
        value.Write(writer);
    }

    public static WorldSpaceIndicator ReadRecord(BinaryReader reader)
    {
        var worldSpaceIndicator = new WorldSpaceIndicator();
        worldSpaceIndicator.Read(reader);
        return worldSpaceIndicator;
    }
}