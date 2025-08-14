using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class GuiLogic
{
    public float LowHealthVignettePercent { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.Write(LowHealthVignettePercent);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        LowHealthVignettePercent = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, GuiLogic value) => value.Write(writer);

    public static GuiLogic ReadRecord(BinaryReader reader)
    {
        var guiLogic = new GuiLogic();
        guiLogic.Read(reader);
        return guiLogic;
    }
}