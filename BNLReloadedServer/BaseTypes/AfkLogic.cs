using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class AfkLogic
{
    public int AfkWarningSeconds { get; set; } = 180;

    public string? AfkWarningTag { get; set; }

    public bool PunishOnce { get; set; }

    public bool KickFromMatch { get; set; }

    public float? AddDemerits { get; set; }

    public float? AddBanHours { get; set; }

    public int AfkPunishSeconds { get; set; } = 300;

    public string? AfkPunishTag { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, AfkWarningTag != null, true, true, AddDemerits.HasValue, AddBanHours.HasValue, true, AfkPunishTag != null).Write(writer);
      writer.Write(AfkWarningSeconds);
      if (AfkWarningTag != null)
        writer.Write(AfkWarningTag);
      writer.Write(PunishOnce);
      writer.Write(KickFromMatch);
      if (AddDemerits.HasValue)
        writer.Write(AddDemerits.Value);
      if (AddBanHours.HasValue)
        writer.Write(AddBanHours.Value);
      writer.Write(AfkPunishSeconds);
      if (AfkPunishTag != null)
        writer.Write(AfkPunishTag);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(8);
      bitField.Read(reader);
      if (bitField[0])
        AfkWarningSeconds = reader.ReadInt32();
      AfkWarningTag = bitField[1] ? reader.ReadString() : null;
      if (bitField[2])
        PunishOnce = reader.ReadBoolean();
      if (bitField[3])
        KickFromMatch = reader.ReadBoolean();
      AddDemerits = bitField[4] ? reader.ReadSingle() : null;
      AddBanHours = bitField[5] ? reader.ReadSingle() : null;
      if (bitField[6])
        AfkPunishSeconds = reader.ReadInt32();
      AfkPunishTag = bitField[7] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, AfkLogic value) => value.Write(writer);

    public static AfkLogic ReadRecord(BinaryReader reader)
    {
      var afkLogic = new AfkLogic();
      afkLogic.Read(reader);
      return afkLogic;
    }
}