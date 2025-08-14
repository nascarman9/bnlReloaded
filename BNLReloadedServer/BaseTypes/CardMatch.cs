using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardMatch : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Match;

    public LocalizedString? Name { get; set; }

    public LocalizedString? Description { get; set; }

    public FriedlyFire? FriendlyFire { get; set; }

    public RespawnLogic? RespawnLogic { get; set; }

    public SurrenderLogic? SurrenderLogic { get; set; }

    public SupplyDropsLogic? SupplyLogic { get; set; }

    public float InitResource { get; set; }

    public float? ResourceCap { get; set; }

    public FallingBlocksLogic? FallingBlocksLogic { get; set; }

    public KillPlainLogic? KillPlainLogic { get; set; }

    public float RadarIndicationTime { get; set; }

    public float RecallDuration { get; set; }

    public HeroSwitchLogic? HeroSwitchLogic { get; set; }

    public List<Key>? PlayerEffects { get; set; }

    public MatchDevicesLogic? DevicesLogic { get; set; }

    public MatchResourceTicker? ResourceTicker { get; set; }

    public MatchStatsLogic? Stats { get; set; }

    public MatchData? Data { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Name != null, Description != null, FriendlyFire != null, RespawnLogic != null,
        SurrenderLogic != null, SupplyLogic != null, true, ResourceCap.HasValue, FallingBlocksLogic != null,
        KillPlainLogic != null, true, true, HeroSwitchLogic != null, PlayerEffects != null, DevicesLogic != null,
        ResourceTicker != null, Stats != null, Data != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      if (FriendlyFire != null)
        FriedlyFire.WriteRecord(writer, FriendlyFire);
      if (RespawnLogic != null)
        RespawnLogic.WriteRecord(writer, RespawnLogic);
      if (SurrenderLogic != null)
        SurrenderLogic.WriteRecord(writer, SurrenderLogic);
      if (SupplyLogic != null)
        SupplyDropsLogic.WriteRecord(writer, SupplyLogic);
      writer.Write(InitResource);
      if (ResourceCap.HasValue)
        writer.Write(ResourceCap.Value);
      if (FallingBlocksLogic != null)
        FallingBlocksLogic.WriteRecord(writer, FallingBlocksLogic);
      if (KillPlainLogic != null)
        KillPlainLogic.WriteRecord(writer, KillPlainLogic);
      writer.Write(RadarIndicationTime);
      writer.Write(RecallDuration);
      if (HeroSwitchLogic != null)
        HeroSwitchLogic.WriteRecord(writer, HeroSwitchLogic);
      if (PlayerEffects != null)
        writer.WriteList(PlayerEffects, Key.WriteRecord);
      if (DevicesLogic != null)
        MatchDevicesLogic.WriteRecord(writer, DevicesLogic);
      if (ResourceTicker != null)
        MatchResourceTicker.WriteRecord(writer, ResourceTicker);
      if (Stats != null)
        MatchStatsLogic.WriteRecord(writer, Stats);
      if (Data != null)
        MatchData.WriteVariant(writer, Data);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(20);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Name = bitField[2] ? LocalizedString.ReadRecord(reader) : null;
      Description = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      FriendlyFire = bitField[4] ? FriedlyFire.ReadRecord(reader) : null;
      RespawnLogic = bitField[5] ? RespawnLogic.ReadRecord(reader) : null;
      SurrenderLogic = bitField[6] ? SurrenderLogic.ReadRecord(reader) : null;
      SupplyLogic = bitField[7] ? SupplyDropsLogic.ReadRecord(reader) : null;
      if (bitField[8])
        InitResource = reader.ReadSingle();
      ResourceCap = bitField[9] ? reader.ReadSingle() : null;
      FallingBlocksLogic = bitField[10] ? FallingBlocksLogic.ReadRecord(reader) : null;
      KillPlainLogic = bitField[11] ? KillPlainLogic.ReadRecord(reader) : null;
      if (bitField[12])
        RadarIndicationTime = reader.ReadSingle();
      if (bitField[13])
        RecallDuration = reader.ReadSingle();
      HeroSwitchLogic = bitField[14] ? HeroSwitchLogic.ReadRecord(reader) : null;
      PlayerEffects = bitField[15] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      DevicesLogic = bitField[16] ? MatchDevicesLogic.ReadRecord(reader) : null;
      ResourceTicker = bitField[17] ? MatchResourceTicker.ReadRecord(reader) : null;
      Stats = bitField[18] ? MatchStatsLogic.ReadRecord(reader) : null;
      Data = bitField[19] ? MatchData.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardMatch value) => value.Write(writer);

    public static CardMatch ReadRecord(BinaryReader reader)
    {
      var cardMatch = new CardMatch();
      cardMatch.Read(reader);
      return cardMatch;
    }
}