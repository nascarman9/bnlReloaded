using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ProtocolInterfaces;

namespace BNLReloadedServer.BaseTypes;

public class CardBlock : Card, IKillscoreIcon, IInternalDevice
{
    [JsonIgnore]
    public bool IsVisualNone => Visual?.Type == BlockVisualType.None;

    [JsonIgnore]
    public bool IsVisualGeneric
    {
      get
      {
        return Visual?.Type switch
        {
          BlockVisualType.Cube or BlockVisualType.CroppedCube or BlockVisualType.Highgrass or BlockVisualType.Flatgrass
            or BlockVisualType.Slope or BlockVisualType.Crosscube or BlockVisualType.Leaf
            or BlockVisualType.Gate => true,
          _ => false
        };
      }
    }

    [JsonIgnore]
    public bool IsVisualClone => Visual?.Type == BlockVisualType.Clone;

    [JsonIgnore]
    public bool IsVisualPrefab => Visual?.Type == BlockVisualType.Prefab;

    [JsonIgnore]
    public bool IsVisualSlope => Visual?.Type == BlockVisualType.Slope;

    [JsonIgnore] 
    public bool CanFloat => BlockId is 10 or 44 || CanStayInAir;

    public string? GetName(int index = 0)
    {
      return IsVisualPrefab ? Visual?.Prefabs?[index].Name?.Text : Visual?.Name?.Text;
    }

    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Block;

    public float? BuildTime { get; set; }

    public float? Cooldown { get; set; }

    public float? BaseCost { get; set; }

    public float? CostIncPerUnit { get; set; }

    public int Level { get; set; } = 1;

    public string? KillscoreIcon { get; set; }

    public ushort BlockId { get; set; }

    public BlockVisual? Visual { get; set; }

    public BlockInsideScreenEffectType? InsideScreenEffect { get; set; }

    public LocalizedString? Description { get; set; }

    public bool Transparent { get; set; }

    public bool LightTransparent { get; set; }

    public bool SkylightTransparent { get; set; }

    public byte LightFade { get; set; } = 1;

    public byte SelfLight { get; set; }

    public bool Solid { get; set; }

    public bool Grounded { get; set; }

    public bool Replaceable { get; set; }

    public bool Destructible { get; set; }

    public bool CanStayInAir { get; set; }

    public BlockPassableType Passable { get; set; }

    public bool PassableBlockFalling { get; set; }

    public bool CanSwim { get; set; }

    public bool HasTeam { get; set; }

    public Health? Health { get; set; }

    public int SplashFalloff { get; set; } = 10;

    public int SplashResistance { get; set; }

    public DeviceType DeviceType { get; set; }

    public ResourceReward? Reward { get; set; }

    public BlockSpecial? Special { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, BuildTime.HasValue, Cooldown.HasValue, BaseCost.HasValue, CostIncPerUnit.HasValue,
        true, KillscoreIcon != null, true, Visual != null, InsideScreenEffect.HasValue, Description != null, true, true,
        true, true, true, true, true, true, true, true, true, true, true, true, Health != null, true, true, true,
        Reward != null, Special != null).Write(writer);
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
      if (KillscoreIcon != null)
        writer.Write(KillscoreIcon);
      writer.Write(BlockId);
      if (Visual != null)
        BlockVisual.WriteRecord(writer, Visual);
      if (InsideScreenEffect.HasValue)
        writer.WriteByteEnum(InsideScreenEffect.Value);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      writer.Write(Transparent);
      writer.Write(LightTransparent);
      writer.Write(SkylightTransparent);
      writer.Write(LightFade);
      writer.Write(SelfLight);
      writer.Write(Solid);
      writer.Write(Grounded);
      writer.Write(Replaceable);
      writer.Write(Destructible);
      writer.Write(CanStayInAir);
      writer.WriteByteEnum(Passable);
      writer.Write(PassableBlockFalling);
      writer.Write(CanSwim);
      writer.Write(HasTeam);
      if (Health != null)
        Health.WriteRecord(writer, Health);
      writer.Write(SplashFalloff);
      writer.Write(SplashResistance);
      writer.WriteByteEnum(DeviceType);
      if (Reward != null)
        ResourceReward.WriteRecord(writer, Reward);
      if (Special == null)
        return;
      BlockSpecial.WriteVariant(writer, Special);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(32);
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
      KillscoreIcon = bitField[7] ? reader.ReadString() : null;
      if (bitField[8])
        BlockId = reader.ReadUInt16();
      Visual = bitField[9] ? BlockVisual.ReadRecord(reader) : null;
      InsideScreenEffect = bitField[10] ? reader.ReadByteEnum<BlockInsideScreenEffectType>() : null;
      Description = bitField[11] ? LocalizedString.ReadRecord(reader) : null;
      if (bitField[12])
        Transparent = reader.ReadBoolean();
      if (bitField[13])
        LightTransparent = reader.ReadBoolean();
      if (bitField[14])
        SkylightTransparent = reader.ReadBoolean();
      if (bitField[15])
        LightFade = reader.ReadByte();
      if (bitField[16])
        SelfLight = reader.ReadByte();
      if (bitField[17])
        Solid = reader.ReadBoolean();
      if (bitField[18])
        Grounded = reader.ReadBoolean();
      if (bitField[19])
        Replaceable = reader.ReadBoolean();
      if (bitField[20])
        Destructible = reader.ReadBoolean();
      if (bitField[21])
        CanStayInAir = reader.ReadBoolean();
      if (bitField[22])
        Passable = reader.ReadByteEnum<BlockPassableType>();
      if (bitField[23])
        PassableBlockFalling = reader.ReadBoolean();
      if (bitField[24])
        CanSwim = reader.ReadBoolean();
      if (bitField[25])
        HasTeam = reader.ReadBoolean();
      Health = !bitField[26] ? null : Health.ReadRecord(reader);
      if (bitField[27])
        SplashFalloff = reader.ReadInt32();
      if (bitField[28])
        SplashResistance = reader.ReadInt32();
      if (bitField[29])
        DeviceType = reader.ReadByteEnum<DeviceType>();
      Reward = bitField[30] ? ResourceReward.ReadRecord(reader) : null;
      Special = bitField[31] ? BlockSpecial.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardBlock value) => value.Write(writer);

    public static CardBlock ReadRecord(BinaryReader reader)
    {
      var cardBlock = new CardBlock();
      cardBlock.Read(reader);
      return cardBlock;
    }
}