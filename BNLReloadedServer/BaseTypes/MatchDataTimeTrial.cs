using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchDataTimeTrial : MatchData
{
    public override MatchType Type => MatchType.TimeTrial;

    public float PrestartTime { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.Write(PrestartTime);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        PrestartTime = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, MatchDataTimeTrial value)
    {
        value.Write(writer);
    }

    public static MatchDataTimeTrial ReadRecord(BinaryReader reader)
    {
        var matchDataTimeTrial = new MatchDataTimeTrial();
        matchDataTimeTrial.Read(reader);
        return matchDataTimeTrial;
    }
}