using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class EffectInterrupt
{
    public bool Recall { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.Write(Recall);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        Recall = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, EffectInterrupt value)
    {
        value.Write(writer);
    }

    public static EffectInterrupt ReadRecord(BinaryReader reader)
    {
        var effectInterrupt = new EffectInterrupt();
        effectInterrupt.Read(reader);
        return effectInterrupt;
    }
}