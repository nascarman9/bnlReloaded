using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SquadLogic
{
    public int MaxPlayersInSquad { get; set; }

    public bool MembersCanInvite { get; set; }

    public bool MembersCanEnterQueue { get; set; }

    public bool MembersCanLeaveQueue { get; set; }

    public float InviteDelay { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true).Write(writer);
      writer.Write(MaxPlayersInSquad);
      writer.Write(MembersCanInvite);
      writer.Write(MembersCanEnterQueue);
      writer.Write(MembersCanLeaveQueue);
      writer.Write(InviteDelay);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      if (bitField[0])
        MaxPlayersInSquad = reader.ReadInt32();
      if (bitField[1])
        MembersCanInvite = reader.ReadBoolean();
      if (bitField[2])
        MembersCanEnterQueue = reader.ReadBoolean();
      if (bitField[3])
        MembersCanLeaveQueue = reader.ReadBoolean();
      if (!bitField[4])
        return;
      InviteDelay = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, SquadLogic value) => value.Write(writer);

    public static SquadLogic ReadRecord(BinaryReader reader)
    {
      var squadLogic = new SquadLogic();
      squadLogic.Read(reader);
      return squadLogic;
    }
}