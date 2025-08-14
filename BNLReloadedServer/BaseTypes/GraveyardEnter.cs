using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class GraveyardEnter
{
    public int Number { get; set; }

    public float? BanHours { get; set; }

    public float MeritsOnUnban { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, BanHours.HasValue, true).Write(writer);
      writer.Write(Number);
      if (BanHours.HasValue)
        writer.Write(BanHours.Value);
      writer.Write(MeritsOnUnban);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      if (bitField[0])
        Number = reader.ReadInt32();
      BanHours = bitField[1] ? reader.ReadSingle() : null;
      if (!bitField[2])
        return;
      MeritsOnUnban = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, GraveyardEnter value)
    {
      value.Write(writer);
    }

    public static GraveyardEnter ReadRecord(BinaryReader reader)
    {
      var graveyardEnter = new GraveyardEnter();
      graveyardEnter.Read(reader);
      return graveyardEnter;
    }
}