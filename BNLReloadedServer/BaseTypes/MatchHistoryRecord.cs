using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchHistoryRecord
{
    public byte[]? MatchId { get; set; }

    public Key HeroKey { get; set; }

    public Key SkinKey { get; set; }

    public Key MapKey { get; set; }

    public Key GameModeKey { get; set; }

    public ulong MatchEndTime { get; set; }

    public float MatchSeconds { get; set; }

    public bool IsWinner { get; set; }

    public bool IsBackfiller { get; set; }

    public bool IsQuit { get; set; }

    public int ResourcesEarned { get; set; }

    public int BlocksBuilt { get; set; }

    public int BlockAssist { get; set; }

    public int Destruction { get; set; }

    public int ObjectiveDamage { get; set; }

    public int Kill { get; set; }

    public int Death { get; set; }

    public int Assist { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(MatchId != null, true, true, true, true, true, true, true, true, true, true, true, true, true, true,
        true, true, true).Write(writer);
      if (MatchId != null)
        writer.WriteBinary(MatchId);
      Key.WriteRecord(writer, HeroKey);
      Key.WriteRecord(writer, SkinKey);
      Key.WriteRecord(writer, MapKey);
      Key.WriteRecord(writer, GameModeKey);
      writer.Write(MatchEndTime);
      writer.Write(MatchSeconds);
      writer.Write(IsWinner);
      writer.Write(IsBackfiller);
      writer.Write(IsQuit);
      writer.Write(ResourcesEarned);
      writer.Write(BlocksBuilt);
      writer.Write(BlockAssist);
      writer.Write(Destruction);
      writer.Write(ObjectiveDamage);
      writer.Write(Kill);
      writer.Write(Death);
      writer.Write(Assist);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(18);
      bitField.Read(reader);
      MatchId = bitField[0] ? reader.ReadBinary() : null;
      if (bitField[1])
        HeroKey = Key.ReadRecord(reader);
      if (bitField[2])
        SkinKey = Key.ReadRecord(reader);
      if (bitField[3])
        MapKey = Key.ReadRecord(reader);
      if (bitField[4])
        GameModeKey = Key.ReadRecord(reader);
      if (bitField[5])
        MatchEndTime = reader.ReadUInt64();
      if (bitField[6])
        MatchSeconds = reader.ReadSingle();
      if (bitField[7])
        IsWinner = reader.ReadBoolean();
      if (bitField[8])
        IsBackfiller = reader.ReadBoolean();
      if (bitField[9])
        IsQuit = reader.ReadBoolean();
      if (bitField[10])
        ResourcesEarned = reader.ReadInt32();
      if (bitField[11])
        BlocksBuilt = reader.ReadInt32();
      if (bitField[12])
        BlockAssist = reader.ReadInt32();
      if (bitField[13])
        Destruction = reader.ReadInt32();
      if (bitField[14])
        ObjectiveDamage = reader.ReadInt32();
      if (bitField[15])
        Kill = reader.ReadInt32();
      if (bitField[16])
        Death = reader.ReadInt32();
      if (!bitField[17])
        return;
      Assist = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, MatchHistoryRecord value)
    {
      value.Write(writer);
    }

    public static MatchHistoryRecord ReadRecord(BinaryReader reader)
    {
      var matchHistoryRecord = new MatchHistoryRecord();
      matchHistoryRecord.Read(reader);
      return matchHistoryRecord;
    }
}