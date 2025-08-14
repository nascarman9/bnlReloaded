using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ProtocolInterfaces;

namespace BNLReloadedServer.BaseTypes;

public class CardUnit : Card, IPrefab, IKillscoreIcon, IInternalDevice, ILootCrateItem
{
    [JsonIgnore]
    public bool IsObjective => Labels != null && Labels.Any(a => a == UnitLabel.Objective);

    [JsonIgnore]
    public bool IsBase => Labels != null && Labels.Any(a => a == UnitLabel.Base);

    [JsonIgnore]
    public bool IsLine1 => Labels != null && Labels.Any(a => a == UnitLabel.Line1);

    [JsonIgnore]
    public bool IsLine2 => Labels != null && Labels.Any(a => a == UnitLabel.Line2);

    [JsonIgnore]
    public bool IsLine3 => Labels != null && Labels.Any(a => a == UnitLabel.Line3);

    [JsonIgnore]
    public bool IsDropPoint => Labels != null && Labels.Any(a => a is UnitLabel.DropPointResource or UnitLabel.DropPointBlockbuster or UnitLabel.DropPointBase);

    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Unit;

    public float? BuildTime { get; set; }

    public float? Cooldown { get; set; }

    public float? BaseCost { get; set; }

    public float? CostIncPerUnit { get; set; }

    public int Level { get; set; } = 1;

    public string? Prefab { get; set; }

    public string? KillscoreIcon { get; set; }

    public LocalizedString? Name { get; set; }

    public LocalizedString? Description { get; set; }

    public UnitMinimapType? MinimapType { get; set; }

    public WorldSpaceIndicator? Indicator { get; set; }

    public string? IconMapEditor { get; set; }

    public Vector3s? Size { get; set; }

    public UnitPivotType PivotType { get; set; }

    public bool GroundOnly { get; set; }

    public bool CanRotate { get; set; }

    public bool BlockMovement { get; set; }

    public Capturing? Capturing { get; set; }

    public UnitHealth? Health { get; set; }

    public UnitLoot? Loot { get; set; }

    public UnitSpawnPoint? SpawnPoint { get; set; }

    public UnitCountLimit? CountLimit { get; set; }

    public float? Lifetime { get; set; }

    public List<UnitLabel>? Labels { get; set; }

    public float FallHitModifier { get; set; }

    public UnitBlockBindingType BlockBinding { get; set; } = UnitBlockBindingType.None;

    public bool AllowUnderwater { get; set; }

    public UnitMovement? Movement { get; set; }

    public DeviceType DeviceType { get; set; }

    public List<Key>? InitEffects { get; set; }

    public List<Key>? EnabledEffects { get; set; }

    public Key Material { get; set; }

    public bool TreatHitsAsOwnerHits { get; set; }

    public UnitData? Data { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, BuildTime.HasValue, Cooldown.HasValue, BaseCost.HasValue, CostIncPerUnit.HasValue,
        true, Prefab != null, KillscoreIcon != null, Name != null, Description != null, MinimapType.HasValue,
        Indicator != null, IconMapEditor != null, Size.HasValue, true, true, true, true, Capturing != null,
        Health != null, Loot != null, SpawnPoint != null, CountLimit != null, Lifetime.HasValue, Labels != null, true,
        true, true, Movement != null, true, InitEffects != null, EnabledEffects != null, true, true,
        Data != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (BuildTime.HasValue)
        writer.Write(BuildTime.Value);
      if (Cooldown.HasValue)
        writer.Write(Cooldown.Value);
      if (BaseCost.HasValue)
        writer.Write(BaseCost.Value);
      if (CostIncPerUnit.HasValue)
        writer.Write(CostIncPerUnit.Value);
      writer.Write(Level);
      if (Prefab != null)
        writer.Write(Prefab);
      if (KillscoreIcon != null)
        writer.Write(KillscoreIcon);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      if (MinimapType.HasValue)
        writer.WriteByteEnum(MinimapType.Value);
      if (Indicator != null)
        WorldSpaceIndicator.WriteRecord(writer, Indicator);
      if (IconMapEditor != null)
        writer.Write(IconMapEditor);
      if (Size.HasValue)
        writer.Write(Size.Value);
      writer.WriteByteEnum(PivotType);
      writer.Write(GroundOnly);
      writer.Write(CanRotate);
      writer.Write(BlockMovement);
      if (Capturing != null)
        Capturing.WriteRecord(writer, Capturing);
      if (Health != null)
        UnitHealth.WriteRecord(writer, Health);
      if (Loot != null)
        UnitLoot.WriteRecord(writer, Loot);
      if (SpawnPoint != null)
        UnitSpawnPoint.WriteRecord(writer, SpawnPoint);
      if (CountLimit != null)
        UnitCountLimit.WriteRecord(writer, CountLimit);
      if (Lifetime.HasValue)
        writer.Write(Lifetime.Value);
      if (Labels != null)
        writer.WriteList(Labels, writer.WriteByteEnum);
      writer.Write(FallHitModifier);
      writer.WriteByteEnum(BlockBinding);
      writer.Write(AllowUnderwater);
      if (Movement != null)
        UnitMovement.WriteVariant(writer, Movement);
      writer.WriteByteEnum(DeviceType);
      if (InitEffects != null)
        writer.WriteList(InitEffects, Key.WriteRecord);
      if (EnabledEffects != null)
        writer.WriteList(EnabledEffects, Key.WriteRecord);
      Key.WriteRecord(writer, Material);
      writer.Write(TreatHitsAsOwnerHits);
      if (Data == null)
        return;
      UnitData.WriteVariant(writer, Data);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(36);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      BuildTime = bitField[2] ? reader.ReadSingle() : null;
      Cooldown = bitField[3] ? reader.ReadSingle() : null;
      BaseCost = bitField[4] ? reader.ReadSingle() : null;
      CostIncPerUnit = bitField[5] ? reader.ReadSingle() : null;
      if (bitField[6])
        Level = reader.ReadInt32();
      Prefab = bitField[7] ? reader.ReadString() : null;
      KillscoreIcon = bitField[8] ? reader.ReadString() : null;
      Name = bitField[9] ? LocalizedString.ReadRecord(reader) : null;
      Description = bitField[10] ? LocalizedString.ReadRecord(reader) : null;
      MinimapType = bitField[11] ? reader.ReadByteEnum<UnitMinimapType>() : null;
      Indicator = bitField[12] ? WorldSpaceIndicator.ReadRecord(reader) : null;
      IconMapEditor = bitField[13] ? reader.ReadString() : null;
      Size = bitField[14] ? reader.ReadVector3s() : null;
      if (bitField[15])
        PivotType = reader.ReadByteEnum<UnitPivotType>();
      if (bitField[16])
        GroundOnly = reader.ReadBoolean();
      if (bitField[17])
        CanRotate = reader.ReadBoolean();
      if (bitField[18])
        BlockMovement = reader.ReadBoolean();
      Capturing = bitField[19] ? Capturing.ReadRecord(reader) : null;
      Health = bitField[20] ? UnitHealth.ReadRecord(reader) : null;
      Loot = bitField[21] ? UnitLoot.ReadRecord(reader) : null;
      SpawnPoint = !bitField[22] ? null : UnitSpawnPoint.ReadRecord(reader);
      CountLimit = !bitField[23] ? null : UnitCountLimit.ReadRecord(reader);
      Lifetime = bitField[24] ? reader.ReadSingle() : null;
      Labels = bitField[25] ? reader.ReadList<UnitLabel, List<UnitLabel>>(reader.ReadByteEnum<UnitLabel>) : null;
      if (bitField[26])
        FallHitModifier = reader.ReadSingle();
      if (bitField[27])
        BlockBinding = reader.ReadByteEnum<UnitBlockBindingType>();
      if (bitField[28])
        AllowUnderwater = reader.ReadBoolean();
      Movement = bitField[29] ? UnitMovement.ReadVariant(reader) : null;
      if (bitField[30])
        DeviceType = reader.ReadByteEnum<DeviceType>();
      InitEffects = bitField[31] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      EnabledEffects = bitField[32] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      if (bitField[33])
        Material = Key.ReadRecord(reader);
      if (bitField[34])
        TreatHitsAsOwnerHits = reader.ReadBoolean();
      Data = bitField[35] ? UnitData.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardUnit value) => value.Write(writer);

    public static CardUnit ReadRecord(BinaryReader reader)
    {
      var cardUnit = new CardUnit();
      cardUnit.Read(reader);
      return cardUnit;
    }
}