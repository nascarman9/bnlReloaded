using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class NoobMapsLogic
{
    public int NoobLevelThreshold { get; set; }

    public int NoobsPerMatchThreshold { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        writer.Write(NoobLevelThreshold);
        writer.Write(NoobsPerMatchThreshold);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            NoobLevelThreshold = reader.ReadInt32();
        if (!bitField[1])
            return;
        NoobsPerMatchThreshold = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, NoobMapsLogic value) => value.Write(writer);

    public static NoobMapsLogic ReadRecord(BinaryReader reader)
    {
        var noobMapsLogic = new NoobMapsLogic();
        noobMapsLogic.Read(reader);
        return noobMapsLogic;
    }
}