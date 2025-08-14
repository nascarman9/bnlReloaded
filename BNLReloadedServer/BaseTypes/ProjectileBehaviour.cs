using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<ProjectileBehaviour>))]
public abstract class ProjectileBehaviour : IJsonFactory<ProjectileBehaviour>
{
    public abstract ProjectileBehaviourType Type { get; }

    public float? Timeout { get; set; }

    public bool HitOnTimeout { get; set; }

    public RelativeTeamType CollideWith { get; set; }

    public static ProjectileBehaviour CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<ProjectileBehaviourType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, ProjectileBehaviour value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static ProjectileBehaviour ReadVariant(BinaryReader reader)
    {
        var projectileBehaviour = Create(reader.ReadByteEnum<ProjectileBehaviourType>());
        projectileBehaviour.Read(reader);
        return projectileBehaviour;
    }

    public static ProjectileBehaviour Create(ProjectileBehaviourType type)
    {
        return type switch
        {
            ProjectileBehaviourType.Rocket => new ProjectileBehaviourRocket(),
            ProjectileBehaviourType.Grenade => new ProjectileBehaviourGrenade(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}