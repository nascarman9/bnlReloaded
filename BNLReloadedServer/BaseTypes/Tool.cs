using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<Tool>))]
public abstract class Tool : IJsonFactory<Tool>
{
    public abstract ToolType Type { get; }

    public ToolAmmo? Ammo { get; set; }

    public bool AutoSwitch { get; set; } = true;

    public static Tool CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<ToolType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, Tool value)
    {
      writer.WriteByteEnum(value.Type);
      value.Write(writer);
    }

    public static Tool ReadVariant(BinaryReader reader)
    {
      var tool = Create(reader.ReadByteEnum<ToolType>());
      tool.Read(reader);
      return tool;
    }

    public static Tool Create(ToolType type)
    {
      return type switch
      {
        ToolType.Shot => new ToolShot(),
        ToolType.Build => new ToolBuild(),
        ToolType.Melee => new ToolMelee(),
        ToolType.Throw => new ToolThrow(),
        ToolType.Channel => new ToolChannel(),
        ToolType.Aiming => new ToolAiming(),
        ToolType.Spinup => new ToolSpinup(),
        ToolType.Dash => new ToolDash(),
        ToolType.Charge => new ToolCharge(),
        ToolType.Burst => new ToolBurst(),
        ToolType.GroundSlam => new ToolGroundSlam(),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
      };
    }
}