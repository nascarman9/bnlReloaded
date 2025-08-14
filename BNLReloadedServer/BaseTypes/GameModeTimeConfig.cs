using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class GameModeTimeConfig
{
    public bool StartEnabled { get; set; }

    public List<TimeTrigger>? Triggers { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, Triggers != null).Write(writer);
      writer.Write(StartEnabled);
      if (Triggers != null)
        writer.WriteList(Triggers, TimeTrigger.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      if (bitField[0])
        StartEnabled = reader.ReadBoolean();
      Triggers = bitField[1] ? reader.ReadList<TimeTrigger, List<TimeTrigger>>(TimeTrigger.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, GameModeTimeConfig value)
    {
      value.Write(writer);
    }

    public static GameModeTimeConfig ReadRecord(BinaryReader reader)
    {
      var gameModeTimeConfig = new GameModeTimeConfig();
      gameModeTimeConfig.Read(reader);
      return gameModeTimeConfig;
    }
}