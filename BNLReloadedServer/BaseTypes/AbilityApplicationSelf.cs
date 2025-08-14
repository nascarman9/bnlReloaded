using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class AbilityApplicationSelf : AbilityApplication
{
    public override AbilityApplicationType Type => AbilityApplicationType.Self;

    public bool IncludeSelfUnit { get; set; } = true;

    /*public override void FromJsonData(JsonNode json)
    {
        IncludeSelfUnit = json["include_self_unit"]!.Deserialize<bool>();
    }

    public override JsonNode ToJsonData() => JsonSerializer.SerializeToNode(this, GetType(), JsonHelper.DefaultSerializerSettings)!;*/

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.Write(IncludeSelfUnit);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        IncludeSelfUnit = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, AbilityApplicationSelf value)
    {
        value.Write(writer);
    }

    public static AbilityApplicationSelf ReadRecord(BinaryReader reader)
    {
        var abilityApplicationSelf = new AbilityApplicationSelf();
        abilityApplicationSelf.Read(reader);
        return abilityApplicationSelf;
    }
}