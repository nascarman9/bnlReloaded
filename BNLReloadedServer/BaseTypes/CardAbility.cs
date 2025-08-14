using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ProtocolInterfaces;

namespace BNLReloadedServer.BaseTypes;

public class CardAbility : Card, IPrefab, IIcon, IKillscoreIcon, IUnlockable
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Ability;

    public string? Icon { get; set; }

    public string? KillscoreIcon { get; set; }

    public string? Prefab { get; set; }

    public AbilityCharges? Charges { get; set; }

    public AbilityValidate? Validate { get; set; }

    public AbilityBehavior? Behavior { get; set; }

    /* public override void FromJsonData(JsonNode json)
    {
      Id = json["_id"]?.Deserialize<string>();
      Scope = json["scope"]!.Deserialize<ScopeType>();
      Icon = json["icon"]?.Deserialize<string>();
      KillscoreIcon = json["killscore_icon"]?.Deserialize<string>();
      Prefab = json["prefab"]?.Deserialize<string>();
      Charges = json["charges"]?.Deserialize<AbilityCharges>();
      if (json["validate"] != null)
        Validate = AbilityValidate.CreateFromJson(json["validate"]!);
      if (json["behavior"] == null)
        return;
      Behavior = AbilityBehavior.CreateFromJson(json["behavior"]);
    }

    public override JsonNode ToJsonData() => JsonSerializer.SerializeToNode(this, GetType(), JsonHelper.DefaultSerializerSettings)!; */

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, true, Validate != null, true).Write(writer);
      writer.Write(Id!);
      writer.WriteByteEnum(Scope);
      writer.Write(Icon!);
      writer.Write(KillscoreIcon!);
      writer.Write(Prefab!);
      AbilityCharges.WriteRecord(writer, Charges!);
      if (Validate != null)
        AbilityValidate.WriteVariant(writer, Validate);
      AbilityBehavior.WriteVariant(writer, Behavior!);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(8);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Icon = !bitField[2] ? null : reader.ReadString();
      KillscoreIcon = !bitField[3] ? null : reader.ReadString();
      Prefab = !bitField[4] ? null : reader.ReadString();
      Charges = !bitField[5] ? null : AbilityCharges.ReadRecord(reader);
      Validate = !bitField[6] ? null : AbilityValidate.ReadVariant(reader);
      Behavior = bitField[7] ? AbilityBehavior.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardAbility value) => value.Write(writer);

    public static CardAbility ReadRecord(BinaryReader reader)
    {
      var cardAbility = new CardAbility();
      cardAbility.Read(reader);
      return cardAbility;
    }
}