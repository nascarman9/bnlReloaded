using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class EndMatchPlayerData
{
    public uint PlayerId { get; set; }

    public ulong? SquadId { get; set; }

    public bool Backfiller { get; set; }

    public bool Noob { get; set; }

    public EndMatchPlayerStats? Stats { get; set; }

    public Key MedalPositive { get; set; }

    public Key MedalNegative { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, SquadId.HasValue, true, true, Stats != null, true, true).Write(writer);
      writer.Write(PlayerId);
      if (SquadId.HasValue)
        writer.Write(SquadId.Value);
      writer.Write(Backfiller);
      writer.Write(Noob);
      if (Stats != null)
        EndMatchPlayerStats.WriteRecord(writer, Stats);
      Key.WriteRecord(writer, MedalPositive);
      Key.WriteRecord(writer, MedalNegative);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      if (bitField[0])
        PlayerId = reader.ReadUInt32();
      SquadId = bitField[1] ? reader.ReadUInt64() : null;
      if (bitField[2])
        Backfiller = reader.ReadBoolean();
      if (bitField[3])
        Noob = reader.ReadBoolean();
      Stats = !bitField[4] ? null : EndMatchPlayerStats.ReadRecord(reader);
      if (bitField[5])
        MedalPositive = Key.ReadRecord(reader);
      if (!bitField[6])
        return;
      MedalNegative = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, EndMatchPlayerData value)
    {
      value.Write(writer);
    }

    public static EndMatchPlayerData ReadRecord(BinaryReader reader)
    {
      var endMatchPlayerData = new EndMatchPlayerData();
      endMatchPlayerData.Read(reader);
      return endMatchPlayerData;
    }
}