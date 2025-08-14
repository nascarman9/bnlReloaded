using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class AbilityBehaviorTrigger : AbilityBehavior
{
    public override AbilityBehaviorType Type => AbilityBehaviorType.Trigger;

    public List<Key>? TriggerEffects { get; set; }

    public float? MaxDuration { get; set; }

    /*public override void FromJsonData(JsonNode json)
    {
      TriggerEffects = json["trigger_effects"]?.Deserialize<List<Key>>();
      MaxDuration = json["max_duration"]?.Deserialize<float>();
    }

    public override JsonNode ToJsonData() => JsonSerializer.SerializeToNode(this, GetType(), JsonHelper.DefaultSerializerSettings)!;*/

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, MaxDuration.HasValue).Write(writer);
      writer.WriteList(TriggerEffects!, Key.WriteRecord);
      if (!MaxDuration.HasValue)
        return;
      writer.Write(MaxDuration.Value);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      TriggerEffects = !bitField[0] ? null : reader.ReadList<Key, List<Key>>(Key.ReadRecord);
      MaxDuration = bitField[1] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, AbilityBehaviorTrigger value)
    {
      value.Write(writer);
    }

    public static AbilityBehaviorTrigger ReadRecord(BinaryReader reader)
    {
      var abilityBehaviorTrigger = new AbilityBehaviorTrigger();
      abilityBehaviorTrigger.Read(reader);
      return abilityBehaviorTrigger;
    }
}