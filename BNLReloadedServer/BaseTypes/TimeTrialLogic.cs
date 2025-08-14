using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class TimeTrialLogic
{
    public Key GameMode { get; set; }

    public List<TimeTrialCourse>? Courses { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, Courses != null).Write(writer);
      Key.WriteRecord(writer, GameMode);
      if (Courses != null)
        writer.WriteList(Courses, TimeTrialCourse.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      if (bitField[0])
        GameMode = Key.ReadRecord(reader);
      Courses = bitField[1] ? reader.ReadList<TimeTrialCourse, List<TimeTrialCourse>>(TimeTrialCourse.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, TimeTrialLogic value)
    {
      value.Write(writer);
    }

    public static TimeTrialLogic ReadRecord(BinaryReader reader)
    {
      var timeTrialLogic = new TimeTrialLogic();
      timeTrialLogic.Read(reader);
      return timeTrialLogic;
    }
}