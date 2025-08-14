using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardChallenge : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Challenge;

    public LocalizedString? Name { get; set; }

    public LocalizedString? Description { get; set; }

    public ChallengeType ChallengeType { get; set; }

    public bool FriendChallengeAllowed { get; set; }

    public float RandomWeight { get; set; }

    public string? Label { get; set; }

    public List<MatchCounter>? Counters { get; set; }

    public float TotalValueRequired { get; set; }

    public Dictionary<CurrencyType, float>? Reward { get; set; }

    public Dictionary<Key, int> RubbleRewards { get; set; } = new();

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Name != null, Description != null, true, true, true, Label != null,
        Counters != null, true, Reward != null, true).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      writer.WriteByteEnum(ChallengeType);
      writer.Write(FriendChallengeAllowed);
      writer.Write(RandomWeight);
      if (Label != null)
        writer.Write(Label);
      if (Counters != null)
        writer.WriteList(Counters, MatchCounter.WriteVariant);
      writer.Write(TotalValueRequired);
      if (Reward != null)
        writer.WriteMap(Reward, writer.WriteByteEnum, writer.Write);
      writer.WriteMap(RubbleRewards, Key.WriteRecord, writer.Write);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(12);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Name = bitField[2] ? LocalizedString.ReadRecord(reader) : null;
      Description = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      if (bitField[4])
        ChallengeType = reader.ReadByteEnum<ChallengeType>();
      if (bitField[5])
        FriendChallengeAllowed = reader.ReadBoolean();
      if (bitField[6])
        RandomWeight = reader.ReadSingle();
      Label = bitField[7] ? reader.ReadString() : null;
      Counters = bitField[8] ? reader.ReadList<MatchCounter, List<MatchCounter>>(MatchCounter.ReadVariant) : null;
      if (bitField[9])
        TotalValueRequired = reader.ReadSingle();
      Reward = bitField[10] ? reader.ReadMap<CurrencyType, float, Dictionary<CurrencyType, float>>(reader.ReadByteEnum<CurrencyType>, reader.ReadSingle): null;
      if (bitField[11])
        RubbleRewards = reader.ReadMap<Key, int, Dictionary<Key, int>>(Key.ReadRecord, reader.ReadInt32);
    }

    public static void WriteRecord(BinaryWriter writer, CardChallenge value) => value.Write(writer);

    public static CardChallenge ReadRecord(BinaryReader reader)
    {
      var cardChallenge = new CardChallenge();
      cardChallenge.Read(reader);
      return cardChallenge;
    }
}