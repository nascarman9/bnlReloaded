using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchObjectiveCollectPickups : MatchObjective
{
    public override MatchObjectiveType Type => MatchObjectiveType.CollectPickups;

    public int? Limit { get; set; }

    public Key? PickupKey { get; set; }

    public UnitLabel? PickupLabel { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, Description != null, Limit.HasValue, PickupKey.HasValue, PickupLabel.HasValue).Write(writer);
      writer.Write(Id);
      writer.WriteByteEnum(Team);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      if (Limit.HasValue)
        writer.Write(Limit.Value);
      if (PickupKey.HasValue)
        Key.WriteRecord(writer, PickupKey.Value);
      if (!PickupLabel.HasValue)
        return;
      writer.WriteByteEnum(PickupLabel.Value);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      if (bitField[0])
        Id = reader.ReadInt32();
      if (bitField[1])
        Team = reader.ReadByteEnum<TeamType>();
      Description = bitField[2] ? LocalizedString.ReadRecord(reader) : null;
      Limit = bitField[3] ? reader.ReadInt32() : null;
      PickupKey = bitField[4] ? Key.ReadRecord(reader) : null;
      PickupLabel = bitField[5] ? reader.ReadByteEnum<UnitLabel>() : null;
    }

    public static void WriteRecord(BinaryWriter writer, MatchObjectiveCollectPickups value)
    {
      value.Write(writer);
    }

    public static MatchObjectiveCollectPickups ReadRecord(BinaryReader reader)
    {
      var objectiveCollectPickups = new MatchObjectiveCollectPickups();
      objectiveCollectPickups.Read(reader);
      return objectiveCollectPickups;
    }
}