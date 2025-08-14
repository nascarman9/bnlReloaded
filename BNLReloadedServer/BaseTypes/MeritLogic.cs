using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MeritLogic
{
    public float MeritInitial { get; set; }

    public float MeritMax { get; set; }

    public float MeritMin { get; set; }

    public float MeritMatchGain { get; set; }

    public float InfluenceInitial { get; set; }

    public float InfluenceGain { get; set; }

    public float InfluenceLoss { get; set; }

    public float InfluenceMax { get; set; }

    public Dictionary<ReportCategory, ReportCategoryData>? ReportData { get; set; }

    public Dictionary<DemeritReason, DemeritData>? DemeritData { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, true, true, true, ReportData != null, DemeritData != null).Write(writer);
      writer.Write(MeritInitial);
      writer.Write(MeritMax);
      writer.Write(MeritMin);
      writer.Write(MeritMatchGain);
      writer.Write(InfluenceInitial);
      writer.Write(InfluenceGain);
      writer.Write(InfluenceLoss);
      writer.Write(InfluenceMax);
      if (ReportData != null)
        writer.WriteMap(ReportData, writer.WriteByteEnum, ReportCategoryData.WriteRecord);
      if (DemeritData != null)
        writer.WriteMap(DemeritData, writer.WriteByteEnum, BaseTypes.DemeritData.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(10);
      bitField.Read(reader);
      if (bitField[0])
        MeritInitial = reader.ReadSingle();
      if (bitField[1])
        MeritMax = reader.ReadSingle();
      if (bitField[2])
        MeritMin = reader.ReadSingle();
      if (bitField[3])
        MeritMatchGain = reader.ReadSingle();
      if (bitField[4])
        InfluenceInitial = reader.ReadSingle();
      if (bitField[5])
        InfluenceGain = reader.ReadSingle();
      if (bitField[6])
        InfluenceLoss = reader.ReadSingle();
      if (bitField[7])
        InfluenceMax = reader.ReadSingle();
      ReportData = bitField[8] ? reader.ReadMap<ReportCategory, ReportCategoryData, Dictionary<ReportCategory, ReportCategoryData>>(reader.ReadByteEnum<ReportCategory>,ReportCategoryData.ReadRecord) : null;
      DemeritData = bitField[9] ? reader.ReadMap<DemeritReason, DemeritData, Dictionary<DemeritReason, DemeritData>>(reader.ReadByteEnum<DemeritReason>, BaseTypes.DemeritData.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, MeritLogic value) => value.Write(writer);

    public static MeritLogic ReadRecord(BinaryReader reader)
    {
      var meritLogic = new MeritLogic();
      meritLogic.Read(reader);
      return meritLogic;
    }
}