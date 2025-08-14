using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class GraveyardLogic
{
    public float GraveyardThreshold { get; set; } = -50f;

    public List<GraveyardEnter>? Enters { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, Enters != null).Write(writer);
      writer.Write(GraveyardThreshold);
      if (Enters != null)
        writer.WriteList(Enters, GraveyardEnter.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      if (bitField[0])
        GraveyardThreshold = reader.ReadSingle();
      Enters = bitField[1] ? reader.ReadList<GraveyardEnter, List<GraveyardEnter>>(GraveyardEnter.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, GraveyardLogic value)
    {
      value.Write(writer);
    }

    public static GraveyardLogic ReadRecord(BinaryReader reader)
    {
      var graveyardLogic = new GraveyardLogic();
      graveyardLogic.Read(reader);
      return graveyardLogic;
    }
}