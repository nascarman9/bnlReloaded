using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class AbilityApplicationProjectile : AbilityApplication
{  
    public override AbilityApplicationType Type => AbilityApplicationType.Projectile;

    public Key ProjectileKey { get; set; }

    public float Speed { get; set; }

    public MultipleBullets? Bullets { get; set; }

    /*public override void FromJsonData(JsonNode json)
    {
      if (json["projectile_key"] != null)
        ProjectileKey = json["projectile_key"]!.Deserialize<Key>();
      if (json["speed"] != null)
        Speed = json["speed"]!.Deserialize<float>();
      Bullets = json["bullets"]?.Deserialize<MultipleBullets>()!;
    }

    public override JsonNode ToJsonData() => JsonSerializer.SerializeToNode(this, GetType(), JsonHelper.DefaultSerializerSettings)!;*/

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, Bullets != null).Write(writer);
      Key.WriteRecord(writer, ProjectileKey);
      writer.Write(Speed);
      if (Bullets == null)
        return;
      MultipleBullets.WriteRecord(writer, Bullets);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      if (bitField[0])
        ProjectileKey = Key.ReadRecord(reader);
      if (bitField[1])
        Speed = reader.ReadSingle();
      Bullets = bitField[2] ? MultipleBullets.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, AbilityApplicationProjectile value)
    {
      value.Write(writer);
    }

    public static AbilityApplicationProjectile ReadRecord(BinaryReader reader)
    {
      var applicationProjectile = new AbilityApplicationProjectile();
      applicationProjectile.Read(reader);
      return applicationProjectile;
    }
}