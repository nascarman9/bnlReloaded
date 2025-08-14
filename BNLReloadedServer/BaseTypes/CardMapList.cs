using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardMapList : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.MapList;

    public List<Key>? Custom { get; set; }

    public List<Key>? Friendly { get; set; }

    public List<Key>? FriendlyNoob { get; set; }

    public List<Key>? Ranked { get; set; }

    public Key Tutorial { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Custom != null, Friendly != null, FriendlyNoob != null, Ranked != null, true).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Custom != null)
        writer.WriteList(Custom, Key.WriteRecord);
      if (Friendly != null)
        writer.WriteList(Friendly, Key.WriteRecord);
      if (FriendlyNoob != null)
        writer.WriteList(FriendlyNoob, Key.WriteRecord);
      if (Ranked != null)
        writer.WriteList(Ranked, Key.WriteRecord);
      Key.WriteRecord(writer, Tutorial);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Custom = bitField[2] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Friendly = bitField[3] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      FriendlyNoob = bitField[4] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Ranked = bitField[5] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      if (!bitField[6])
        return;
      Tutorial = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, CardMapList value) => value.Write(writer);

    public static CardMapList ReadRecord(BinaryReader reader)
    {
      var cardMapList = new CardMapList();
      cardMapList.Read(reader);
      return cardMapList;
    }
}