using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitUpdate
{
    public TeamType? Team { get; set; }

    public float? Health { get; set; }

    public float? Forcefield { get; set; }

    public float? Shield { get; set; }

    public float? CapturePoints { get; set; }

    public bool? MovementActive { get; set; }

    public Dictionary<Key, List<Ammo>>? Ammo { get; set; }

    public Key? CurrentGear { get; set; }

    public Key? Ability { get; set; }

    public int? AbilityCharges { get; set; }

    public ulong? AbilityChargeCooldownEnd { get; set; }

    public float? Resource { get; set; }

    public Dictionary<Key, ulong?>? Effects { get; set; }

    public Dictionary<BuffType, float>? Buffs { get; set; }

    public Dictionary<int, DeviceData>? Devices { get; set; }

    public uint? TurretTargetId { get; set; }

    public List<Vector3s>? CloudAffectedBlocks { get; set; }

    public float? ProjectileInitSpeed { get; set; }

    public ulong? BombTimeoutEnd { get; set; }

    public List<uint>? DamageCapturers { get; set; }

    public PortalLink? PortalLink { get; set; }

    public TeslaChargeType? TeslaCharge { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Team.HasValue, Health.HasValue, Forcefield.HasValue, Shield.HasValue, CapturePoints.HasValue,
        MovementActive.HasValue, Ammo != null, CurrentGear.HasValue, Ability.HasValue, AbilityCharges.HasValue,
        AbilityChargeCooldownEnd.HasValue, Resource.HasValue, Effects != null, Buffs != null, Devices != null,
        TurretTargetId.HasValue, CloudAffectedBlocks != null, ProjectileInitSpeed.HasValue, BombTimeoutEnd.HasValue,
        DamageCapturers != null, PortalLink != null, TeslaCharge.HasValue).Write(writer);
      if (Team.HasValue)
        writer.WriteByteEnum(Team.Value);
      if (Health.HasValue)
        writer.Write(Health.Value);
      if (Forcefield.HasValue)
        writer.Write(Forcefield.Value);
      if (Shield.HasValue)
        writer.Write(Shield.Value);
      if (CapturePoints.HasValue)
        writer.Write(CapturePoints.Value);
      if (MovementActive.HasValue)
        writer.Write(MovementActive.Value);
      if (Ammo != null)
        writer.WriteMap(Ammo, Key.WriteRecord, item => writer.WriteList(item, BaseTypes.Ammo.WriteRecord));
      if (CurrentGear.HasValue)
        Key.WriteRecord(writer, CurrentGear.Value);
      if (Ability.HasValue)
        Key.WriteRecord(writer, Ability.Value);
      if (AbilityCharges.HasValue)
        writer.Write(AbilityCharges.Value);
      if (AbilityChargeCooldownEnd.HasValue)
        writer.Write(AbilityChargeCooldownEnd.Value);
      if (Resource.HasValue)
        writer.Write(Resource.Value);
      if (Effects != null)
        writer.WriteMap(Effects, Key.WriteRecord, item => writer.WriteOptionValue(item, writer.Write));
      if (Buffs != null)
        writer.WriteMap(Buffs, writer.WriteByteEnum, writer.Write);
      if (Devices != null)
        writer.WriteMap(Devices, writer.Write, DeviceData.WriteRecord);
      if (TurretTargetId.HasValue)
        writer.Write(TurretTargetId.Value);
      if (CloudAffectedBlocks != null)
        writer.WriteList(CloudAffectedBlocks, writer.Write);
      if (ProjectileInitSpeed.HasValue)
        writer.Write(ProjectileInitSpeed.Value);
      if (BombTimeoutEnd.HasValue)
        writer.Write(BombTimeoutEnd.Value);
      if (DamageCapturers != null)
        writer.WriteList(DamageCapturers, writer.Write);
      if (PortalLink != null)
        PortalLink.WriteRecord(writer, PortalLink);
      if (!TeslaCharge.HasValue)
        return;
      writer.WriteByteEnum(TeslaCharge.Value);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(22);
      bitField.Read(reader);
      Team = bitField[0] ? reader.ReadByteEnum<TeamType>() : null;
      Health = bitField[1] ? reader.ReadSingle() : null;
      Forcefield = bitField[2] ? reader.ReadSingle() : null;
      Shield = bitField[3] ? reader.ReadSingle() : null;
      CapturePoints = bitField[4] ? reader.ReadSingle() : null;
      MovementActive = bitField[5] ? reader.ReadBoolean() : null;
      Ammo = bitField[6] ? reader.ReadMap<Key, List<Ammo>, Dictionary<Key, List<Ammo>>>(Key.ReadRecord, () => reader.ReadList<Ammo, List<Ammo>>(BaseTypes.Ammo.ReadRecord)) : null;
      CurrentGear = bitField[7] ? Key.ReadRecord(reader) : null;
      Ability = bitField[8] ? Key.ReadRecord(reader) : null;
      AbilityCharges = bitField[9] ? reader.ReadInt32() : null;
      AbilityChargeCooldownEnd = bitField[10] ? reader.ReadUInt64() : null;
      Resource = bitField[11] ? reader.ReadSingle() : null;
      Effects = bitField[12] ? reader.ReadMap<Key, ulong?, Dictionary<Key, ulong?>>(Key.ReadRecord, () => reader.ReadOptionValue(reader.ReadUInt64)) : null;
      Buffs = bitField[13] ? reader.ReadMap<BuffType, float, Dictionary<BuffType, float>>(reader.ReadByteEnum<BuffType>, reader.ReadSingle) : null;
      Devices = bitField[14] ? reader.ReadMap<int, DeviceData, Dictionary<int, DeviceData>>(reader.ReadInt32, DeviceData.ReadRecord) : null;
      TurretTargetId = bitField[15] ? reader.ReadUInt32() : null;
      CloudAffectedBlocks = bitField[16] ? reader.ReadList<Vector3s, List<Vector3s>>(reader.ReadVector3s) : null;
      ProjectileInitSpeed = bitField[17] ? reader.ReadSingle() : null;
      BombTimeoutEnd = bitField[18] ? reader.ReadUInt64() : null;
      DamageCapturers = bitField[19] ? reader.ReadList<uint, List<uint>>(reader.ReadUInt32) : null;
      PortalLink = bitField[20] ? PortalLink.ReadRecord(reader) : null;
      TeslaCharge = bitField[21] ? reader.ReadByteEnum<TeslaChargeType>() : null;
    }

    public static void WriteRecord(BinaryWriter writer, UnitUpdate value) => value.Write(writer);

    public static UnitUpdate ReadRecord(BinaryReader reader)
    {
      var unitUpdate = new UnitUpdate();
      unitUpdate.Read(reader);
      return unitUpdate;
    }
}