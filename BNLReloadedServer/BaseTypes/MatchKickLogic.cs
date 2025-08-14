using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchKickLogic
{
    public Dictionary<KickReason, string>? ChatMessageTags { get; set; }

    public Dictionary<KickReason, string>? PopupMessageTags { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(ChatMessageTags != null, PopupMessageTags != null).Write(writer);
      if (ChatMessageTags != null)
        writer.WriteMap(ChatMessageTags, writer.WriteByteEnum, writer.Write);
      if (PopupMessageTags != null)
        writer.WriteMap(PopupMessageTags, writer.WriteByteEnum, writer.Write);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      ChatMessageTags = bitField[0] ? reader.ReadMap<KickReason, string, Dictionary<KickReason, string>>(reader.ReadByteEnum<KickReason>, reader.ReadString) : null;
      PopupMessageTags = bitField[1] ? reader.ReadMap<KickReason, string, Dictionary<KickReason, string>>(reader.ReadByteEnum<KickReason>, reader.ReadString) : null;
    }

    public static void WriteRecord(BinaryWriter writer, MatchKickLogic value)
    {
      value.Write(writer);
    }

    public static MatchKickLogic ReadRecord(BinaryReader reader)
    {
      var matchKickLogic = new MatchKickLogic();
      matchKickLogic.Read(reader);
      return matchKickLogic;
    }
}