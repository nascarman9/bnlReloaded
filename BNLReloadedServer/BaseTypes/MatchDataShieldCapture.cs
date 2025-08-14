using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchDataShieldCapture : MatchData
{
    public override MatchType Type => MatchType.ShieldCapture;

    public bool UseMapBuildTime { get; set; }

    [JsonPropertyName("build_1_time")]
    public int Build1Time { get; set; }

    public float EndMatchDelay { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, true).Write(writer);
      writer.Write(UseMapBuildTime);
      writer.Write(Build1Time);
      writer.Write(EndMatchDelay);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      if (bitField[0])
        UseMapBuildTime = reader.ReadBoolean();
      if (bitField[1])
        Build1Time = reader.ReadInt32();
      if (!bitField[2])
        return;
      EndMatchDelay = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, MatchDataShieldCapture value)
    {
      value.Write(writer);
    }

    public static MatchDataShieldCapture ReadRecord(BinaryReader reader)
    {
      var dataShieldCapture = new MatchDataShieldCapture();
      dataShieldCapture.Read(reader);
      return dataShieldCapture;
    }
}