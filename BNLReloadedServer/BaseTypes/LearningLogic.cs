using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LearningLogic
{
    public string? VisitSteamGuidesUrl { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(VisitSteamGuidesUrl != null).Write(writer);
        if (VisitSteamGuidesUrl != null)
            writer.Write(VisitSteamGuidesUrl);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        VisitSteamGuidesUrl = bitField[0] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, LearningLogic value) => value.Write(writer);

    public static LearningLogic ReadRecord(BinaryReader reader)
    {
        var learningLogic = new LearningLogic();
        learningLogic.Read(reader);
        return learningLogic;
    }
}