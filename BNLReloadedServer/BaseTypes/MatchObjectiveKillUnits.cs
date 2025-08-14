using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchObjectiveKillUnits : MatchObjective
{
    public override MatchObjectiveType Type => MatchObjectiveType.KillUnits;

    public int? Limit { get; set; }

    public Key? UnitKey { get; set; }

    public UnitLabel? UnitLabel { get; set; }

    public RelativeTeamType UnitTeam { get; set; } = RelativeTeamType.Both;

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, Description != null, Limit.HasValue, UnitKey.HasValue, UnitLabel.HasValue, true).Write(writer);
      writer.Write(Id);
      writer.WriteByteEnum(Team);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      if (Limit.HasValue)
        writer.Write(Limit.Value);
      if (UnitKey.HasValue)
        Key.WriteRecord(writer, UnitKey.Value);
      if (UnitLabel.HasValue)
        writer.WriteByteEnum(UnitLabel.Value);
      writer.WriteByteEnum(UnitTeam);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      if (bitField[0])
        Id = reader.ReadInt32();
      if (bitField[1])
        Team = reader.ReadByteEnum<TeamType>();
      Description = bitField[2] ? LocalizedString.ReadRecord(reader) : null;
      Limit = bitField[3] ? reader.ReadInt32() : null;
      UnitKey = bitField[4] ? Key.ReadRecord(reader) : null;
      UnitLabel = bitField[5] ? reader.ReadByteEnum<UnitLabel>() : null;
      if (!bitField[6])
        return;
      UnitTeam = reader.ReadByteEnum<RelativeTeamType>();
    }

    public static void WriteRecord(BinaryWriter writer, MatchObjectiveKillUnits value)
    {
      value.Write(writer);
    }

    public static MatchObjectiveKillUnits ReadRecord(BinaryReader reader)
    {
      var objectiveKillUnits = new MatchObjectiveKillUnits();
      objectiveKillUnits.Read(reader);
      return objectiveKillUnits;
    }
}