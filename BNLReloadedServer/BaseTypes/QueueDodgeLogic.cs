using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class QueueDodgeLogic
{
    public int QueueDodgeTimeoutHours { get; set; } = 12;

    public Dictionary<int, DodgePenalty>? Dodges { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, Dodges != null).Write(writer);
      writer.Write(QueueDodgeTimeoutHours);
      if (Dodges != null)
        writer.WriteMap(Dodges, writer.Write, DodgePenalty.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      if (bitField[0])
        QueueDodgeTimeoutHours = reader.ReadInt32();
      Dodges = bitField[1] ? reader.ReadMap<int, DodgePenalty, Dictionary<int, DodgePenalty>>(reader.ReadInt32, DodgePenalty.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, QueueDodgeLogic value)
    {
      value.Write(writer);
    }

    public static QueueDodgeLogic ReadRecord(BinaryReader reader)
    {
      var queueDodgeLogic = new QueueDodgeLogic();
      queueDodgeLogic.Read(reader);
      return queueDodgeLogic;
    }
}