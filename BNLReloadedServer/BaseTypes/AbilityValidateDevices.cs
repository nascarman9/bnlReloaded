using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class AbilityValidateDevices : AbilityValidate
{
    public override AbilityValidateType Type => AbilityValidateType.Devices;

    public EffectTargeting? DeviceTargeting { get; set; }

    public bool OwnedOnly { get; set; }

    public int MinCount { get; set; }

    public float? Range { get; set; }

    /* public override void FromJsonData(JsonNode json)
    {
      DeviceTargeting = json["device_targeting"]?.Deserialize<EffectTargeting>();
      OwnedOnly = json["owned_only"]!.Deserialize<bool>();
      MinCount = json["min_count"]!.Deserialize<int>();
      Range = json["range"]?.Deserialize<float>();
    }

    public override JsonNode ToJsonData() => JsonSerializer.SerializeToNode(this, GetType(), JsonHelper.DefaultSerializerSettings)!; */

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, Range.HasValue).Write(writer);
      EffectTargeting.WriteRecord(writer, DeviceTargeting);
      writer.Write(OwnedOnly);
      writer.Write(MinCount);
      if (!Range.HasValue)
        return;
      writer.Write(Range.Value);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      DeviceTargeting = !bitField[0] ? null : EffectTargeting.ReadRecord(reader);
      if (bitField[1])
        OwnedOnly = reader.ReadBoolean();
      if (bitField[2])
        MinCount = reader.ReadInt32();
      Range = bitField[3] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, AbilityValidateDevices value)
    {
      value.Write(writer);
    }

    public static AbilityValidateDevices ReadRecord(BinaryReader reader)
    {
      var abilityValidateDevices = new AbilityValidateDevices();
      abilityValidateDevices.Read(reader);
      return abilityValidateDevices;
    }
}