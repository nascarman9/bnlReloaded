using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchDataTutorial : MatchData
{
    public override MatchType Type => MatchType.Tutorial;

    public int BuildTime { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.Write(BuildTime);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        BuildTime = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, MatchDataTutorial value)
    {
        value.Write(writer);
    }

    public static MatchDataTutorial ReadRecord(BinaryReader reader)
    {
        var matchDataTutorial = new MatchDataTutorial();
        matchDataTutorial.Read(reader);
        return matchDataTutorial;
    }
}