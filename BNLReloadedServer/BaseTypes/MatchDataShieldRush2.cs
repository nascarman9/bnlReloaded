using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchDataShieldRush2 : MatchData
{
    public override MatchType Type => MatchType.ShieldRush2;

    public bool UseMapBuildTime { get; set; }

    [JsonPropertyName("build_1_time")]
    public int Build1Time { get; set; }

    public float EndMatchDelay { get; set; }

    public bool UseMapStartingResources { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true).Write(writer);
      writer.Write(UseMapBuildTime);
      writer.Write(Build1Time);
      writer.Write(EndMatchDelay);
      writer.Write(UseMapStartingResources);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      if (bitField[0])
        UseMapBuildTime = reader.ReadBoolean();
      if (bitField[1])
        Build1Time = reader.ReadInt32();
      if (bitField[2])
        EndMatchDelay = reader.ReadSingle();
      if (!bitField[3])
        return;
      UseMapStartingResources = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, MatchDataShieldRush2 value)
    {
      value.Write(writer);
    }

    public static MatchDataShieldRush2 ReadRecord(BinaryReader reader)
    {
      var matchDataShieldRush2 = new MatchDataShieldRush2();
      matchDataShieldRush2.Read(reader);
      return matchDataShieldRush2;
    }
}