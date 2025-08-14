using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class TimeTrialCourse
{
    public Key Map { get; set; }

    public Key Hero { get; set; }

    public List<Key>? Devices { get; set; }

    public LocalizedString? Description { get; set; }

    public Dictionary<int, TimeTrialGoal>? Goals { get; set; }

    public List<MatchObjective>? MatchObjectives { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, Devices != null, Description != null, Goals != null, MatchObjectives != null).Write(writer);
      Key.WriteRecord(writer, Map);
      Key.WriteRecord(writer, Hero);
      if (Devices != null)
        writer.WriteList(Devices, Key.WriteRecord);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      if (Goals != null)
        writer.WriteMap(Goals, writer.Write, TimeTrialGoal.WriteRecord);
      if (MatchObjectives != null)
        writer.WriteList(MatchObjectives, MatchObjective.WriteVariant);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      if (bitField[0])
        Map = Key.ReadRecord(reader);
      if (bitField[1])
        Hero = Key.ReadRecord(reader);
      Devices = bitField[2] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Description = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      Goals = bitField[4] ? reader.ReadMap<int, TimeTrialGoal, Dictionary<int, TimeTrialGoal>>(reader.ReadInt32, TimeTrialGoal.ReadRecord) : null;
      MatchObjectives = bitField[5] ? reader.ReadList<MatchObjective, List<MatchObjective>>(MatchObjective.ReadVariant) : null;
    }

    public static void WriteRecord(BinaryWriter writer, TimeTrialCourse value)
    {
      value.Write(writer);
    }

    public static TimeTrialCourse ReadRecord(BinaryReader reader)
    {
      var timeTrialCourse = new TimeTrialCourse();
      timeTrialCourse.Read(reader);
      return timeTrialCourse;
    }
}