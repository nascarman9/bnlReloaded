using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ProtocolInterfaces;

namespace BNLReloadedServer.BaseTypes;

public class CardGear : Card, IPrefab, IIcon, IKillscoreIcon
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Gear;

    public string? Prefab { get; set; }

    public string? Icon { get; set; }

    public string? KillscoreIcon { get; set; }

    public LocalizedString? Name { get; set; }

    public LocalizedString? Description { get; set; }

    public string? AnimationTag { get; set; }

    public string? PrimaryFireIcon { get; set; }

    public float RenderFov { get; set; } = 30f;

    public float PickupTime { get; set; }

    public float DropTime { get; set; }

    public List<Key>? EquipEffects { get; set; }

    public List<AmmoData>? Ammo { get; set; }

    public Reload? Reload { get; set; }

    public float? Sway { get; set; }

    public float? SwayCrouchMod { get; set; }

    public ConeOfFire? ConeOfFire { get; set; }

    public List<ReticleInfo>? Reticles { get; set; }

    public AimAssistInfo? AimAssist { get; set; }

    public List<Tool>? Tools { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Prefab != null, Icon != null, KillscoreIcon != null, Name != null,
        Description != null, AnimationTag != null, PrimaryFireIcon != null, true, true, true, EquipEffects != null,
        Ammo != null, Reload != null, Sway.HasValue, SwayCrouchMod.HasValue, ConeOfFire != null, Reticles != null,
        AimAssist != null, Tools != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Prefab != null)
        writer.Write(Prefab);
      if (Icon != null)
        writer.Write(Icon);
      if (KillscoreIcon != null)
        writer.Write(KillscoreIcon);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      if (AnimationTag != null)
        writer.Write(AnimationTag);
      if (PrimaryFireIcon != null)
        writer.Write(PrimaryFireIcon);
      writer.Write(RenderFov);
      writer.Write(PickupTime);
      writer.Write(DropTime);
      if (EquipEffects != null)
        writer.WriteList(EquipEffects, Key.WriteRecord);
      if (Ammo != null)
        writer.WriteList(Ammo, AmmoData.WriteRecord);
      if (Reload != null)
        Reload.WriteVariant(writer, Reload);
      if (Sway.HasValue)
        writer.Write(Sway.Value);
      if (SwayCrouchMod.HasValue)
        writer.Write(SwayCrouchMod.Value);
      if (ConeOfFire != null)
        ConeOfFire.WriteRecord(writer, ConeOfFire);
      if (Reticles != null)
        writer.WriteList(Reticles, ReticleInfo.WriteRecord);
      if (AimAssist != null)
        AimAssistInfo.WriteRecord(writer, AimAssist);
      if (Tools != null)
        writer.WriteList(Tools, Tool.WriteVariant);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(21);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Prefab = bitField[2] ? reader.ReadString() : null;
      Icon = bitField[3] ? reader.ReadString() : null;
      KillscoreIcon = bitField[4] ? reader.ReadString() : null;
      Name = bitField[5] ? LocalizedString.ReadRecord(reader) : null;
      Description = bitField[6] ? LocalizedString.ReadRecord(reader) : null;
      AnimationTag = bitField[7] ? reader.ReadString() : null;
      PrimaryFireIcon = bitField[8] ? reader.ReadString() : null;
      if (bitField[9])
        RenderFov = reader.ReadSingle();
      if (bitField[10])
        PickupTime = reader.ReadSingle();
      if (bitField[11])
        DropTime = reader.ReadSingle();
      EquipEffects = bitField[12] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Ammo = bitField[13] ? reader.ReadList<AmmoData, List<AmmoData>>(AmmoData.ReadRecord) : null;
      Reload = bitField[14] ? Reload.ReadVariant(reader) : null;
      Sway = bitField[15] ? reader.ReadSingle() : null;
      SwayCrouchMod = bitField[16] ? reader.ReadSingle() : null;
      ConeOfFire = bitField[17] ? ConeOfFire.ReadRecord(reader) : null;
      Reticles = bitField[18] ? reader.ReadList<ReticleInfo, List<ReticleInfo>>(ReticleInfo.ReadRecord) : null;
      AimAssist = bitField[19] ? AimAssistInfo.ReadRecord(reader) : null;
      Tools = bitField[20] ? reader.ReadList<Tool, List<Tool>>(Tool.ReadVariant) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardGear value) => value.Write(writer);

    public static CardGear ReadRecord(BinaryReader reader)
    {
      var cardGear = new CardGear();
      cardGear.Read(reader);
      return cardGear;
    }
}