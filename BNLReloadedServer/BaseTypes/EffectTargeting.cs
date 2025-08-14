using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class EffectTargeting
{
    public List<UnitLabel>? AffectedLabels { get; set; }

    public List<UnitType>? AffectedUnits { get; set; }

    public RelativeTeamType AffectedTeam { get; set; }

    public bool CasterOwnedOnly { get; set; }

    public bool IgnoreCaster { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(AffectedLabels != null, AffectedUnits != null, true, true, true).Write(writer);
      if (AffectedLabels != null)
        writer.WriteList(AffectedLabels, writer.WriteByteEnum);
      if (AffectedUnits != null)
        writer.WriteList(AffectedUnits, writer.WriteByteEnum);
      writer.WriteByteEnum(AffectedTeam);
      writer.Write(CasterOwnedOnly);
      writer.Write(IgnoreCaster);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      AffectedLabels = !bitField[0] ? null : reader.ReadList<UnitLabel, List<UnitLabel>>(reader.ReadByteEnum<UnitLabel>);
      AffectedUnits = !bitField[1] ? null : reader.ReadList<UnitType, List<UnitType>>(reader.ReadByteEnum<UnitType>);
      if (bitField[2])
        AffectedTeam = reader.ReadByteEnum<RelativeTeamType>();
      if (bitField[3])
        CasterOwnedOnly = reader.ReadBoolean();
      if (!bitField[4])
        return;
      IgnoreCaster = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, EffectTargeting value)
    {
      value.Write(writer);
    }

    public static EffectTargeting ReadRecord(BinaryReader reader)
    {
      var effectTargeting = new EffectTargeting();
      effectTargeting.Read(reader);
      return effectTargeting;
    }
}