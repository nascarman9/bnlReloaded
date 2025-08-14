using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class AnticheatLogic
{
    public float MinProbability { get; set; } = 0.15f;

    public float SumProbablityForNote { get; set; } = 3f;

    public float SumProbablityForBan { get; set; } = 10f;

    public float ClientAnticheatWeight { get; set; } = 100f;

    public bool KickFromMatch { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true).Write(writer);
      writer.Write(MinProbability);
      writer.Write(SumProbablityForNote);
      writer.Write(SumProbablityForBan);
      writer.Write(ClientAnticheatWeight);
      writer.Write(KickFromMatch);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      if (bitField[0])
        MinProbability = reader.ReadSingle();
      if (bitField[1])
        SumProbablityForNote = reader.ReadSingle();
      if (bitField[2])
        SumProbablityForBan = reader.ReadSingle();
      if (bitField[3])
        ClientAnticheatWeight = reader.ReadSingle();
      if (!bitField[4])
        return;
      KickFromMatch = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, AnticheatLogic value)
    {
      value.Write(writer);
    }

    public static AnticheatLogic ReadRecord(BinaryReader reader)
    {
      var anticheatLogic = new AnticheatLogic();
      anticheatLogic.Read(reader);
      return anticheatLogic;
    }
}