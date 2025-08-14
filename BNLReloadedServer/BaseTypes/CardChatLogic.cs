using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardChatLogic : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.ChatLogic;

    public Dictionary<Locale, ChatOffensive>? Offensive { get; set; }

    public int SpamMessageLimit { get; set; } = 5;

    public float SpamMessageWindow { get; set; } = 2f;

    public float SpamMessageCooldown { get; set; } = 10f;

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Offensive != null, true, true, true).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Offensive != null)
        writer.WriteMap(Offensive, writer.WriteByteEnum, ChatOffensive.WriteRecord);
      writer.Write(SpamMessageLimit);
      writer.Write(SpamMessageWindow);
      writer.Write(SpamMessageCooldown);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Offensive = bitField[2] ? reader.ReadMap<Locale, ChatOffensive, Dictionary<Locale, ChatOffensive>>(reader.ReadByteEnum<Locale>, ChatOffensive.ReadRecord) : null;
      if (bitField[3])
        SpamMessageLimit = reader.ReadInt32();
      if (bitField[4])
        SpamMessageWindow = reader.ReadSingle();
      if (!bitField[5])
        return;
      SpamMessageCooldown = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, CardChatLogic value) => value.Write(writer);

    public static CardChatLogic ReadRecord(BinaryReader reader)
    {
      var cardChatLogic = new CardChatLogic();
      cardChatLogic.Read(reader);
      return cardChatLogic;
    }
}