using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<UnitData>))]
public abstract class UnitData : IJsonFactory<UnitData>
{
    public abstract UnitType Type { get; }

    public static UnitData CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<UnitType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, UnitData value)
    {
      writer.WriteByteEnum(value.Type);
      value.Write(writer);
    }

    public static UnitData ReadVariant(BinaryReader reader)
    {
      var unitData = Create(reader.ReadByteEnum<UnitType>());
      unitData.Read(reader);
      return unitData;
    }

    public static UnitData Create(UnitType type)
    {
      return type switch
      {
        UnitType.Common => new UnitDataCommon(),
        UnitType.Player => new UnitDataPlayer(),
        UnitType.Landmine => new UnitDataLandmine(),
        UnitType.Bomb => new UnitDataBomb(),
        UnitType.Turret => new UnitDataTurret(),
        UnitType.Pickup => new UnitDataPickup(),
        UnitType.Mortar => new UnitDataMortar(),
        UnitType.Cloud => new UnitDataCloud(),
        UnitType.Projectile => new UnitDataProjectile(),
        UnitType.Skybeam => new UnitDataSkybeam(),
        UnitType.DamageCapture => new UnitDataDamageCapture(),
        UnitType.TeslaCoil => new UnitDataTeslaCoil(),
        UnitType.Portal => new UnitDataPortal(),
        UnitType.Shower => new UnitDataShower(),
        UnitType.Drill => new UnitDataDrill(),
        UnitType.PiggyBank => new UnitDataPiggyBank(),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
      };
    }
}