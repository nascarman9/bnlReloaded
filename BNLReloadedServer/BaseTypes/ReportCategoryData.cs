using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ReportCategoryData
{
    public LocalizedString? Title { get; set; }

    public int Demerits { get; set; }

    public string OrtsQueue { get; set; } = "Block & Load::In-game Reports";

    public int OrtsPriority { get; set; } = 5;

    public void Write(BinaryWriter writer)
    {
      new BitField(Title != null, true, true, true).Write(writer);
      if (Title != null)
        LocalizedString.WriteRecord(writer, Title);
      writer.Write(Demerits);
      writer.Write(OrtsQueue);
      writer.Write(OrtsPriority);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Title = bitField[0] ? LocalizedString.ReadRecord(reader) : null;
      if (bitField[1])
        Demerits = reader.ReadInt32();
      if (bitField[2])
        OrtsQueue = reader.ReadString();
      if (!bitField[3])
        return;
      OrtsPriority = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, ReportCategoryData value)
    {
      value.Write(writer);
    }

    public static ReportCategoryData ReadRecord(BinaryReader reader)
    {
      var reportCategoryData = new ReportCategoryData();
      reportCategoryData.Read(reader);
      return reportCategoryData;
    }
}