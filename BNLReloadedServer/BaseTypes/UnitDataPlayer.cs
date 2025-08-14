using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitDataPlayer : UnitData
{
    public override UnitType Type => UnitType.Player;

    public HeroGuiInfo? GuiInfo { get; set; }

    public List<Key>? Skins { get; set; }

    public Key Class { get; set; }

    public List<Key>? Gears { get; set; }

    public List<Key>? SpecialDevices { get; set; }

    public List<Key>? DefaultDevices { get; set; }

    public Key ActiveAbilityKey { get; set; }

    public List<Key>? Passive { get; set; }

    public float GroundAccelerationMod { get; set; } = 1f;

    public float CrouchSpeed { get; set; }

    public float RunSpeed { get; set; }

    public float JumpSpeed { get; set; }

    public float SprintSpeed { get; set; }

    public float SwimSpeed { get; set; }

    public float SwimSprintSpeed { get; set; }

    public float JumpHeight { get; set; }

    public float? DoubleJumpHeight { get; set; }

    public float InitResourceBonus { get; set; }

    public ForceFall? ForceFall { get; set; }

    public float DrownDamage { get; set; }

    public Dictionary<int, Unlockable>? Unlockables { get; set; }

    public Vector3 AimAssistSize { get; set; }

    public Vector3 AimAssistAdsSize { get; set; }

    public Vector3 AimAssistCenter { get; set; }

    public Vector3 AimAssistCrouchSize { get; set; }

    public Vector3 AimAssistCrouchAdsSize { get; set; }

    public Vector3 AimAssistCrouchCenter { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(GuiInfo != null, Skins != null, true, Gears != null, SpecialDevices != null, DefaultDevices != null,
        true, Passive != null, true, true, true, true, true, true, true, true, DoubleJumpHeight.HasValue, true,
        ForceFall != null, true, Unlockables != null, true, true, true, true, true, true).Write(writer);
      if (GuiInfo != null)
        HeroGuiInfo.WriteRecord(writer, GuiInfo);
      if (Skins != null)
        writer.WriteList(Skins, Key.WriteRecord);
      Key.WriteRecord(writer, Class);
      if (Gears != null)
        writer.WriteList(Gears, Key.WriteRecord);
      if (SpecialDevices != null)
        writer.WriteList(SpecialDevices, Key.WriteRecord);
      if (DefaultDevices != null)
        writer.WriteList(DefaultDevices, Key.WriteRecord);
      Key.WriteRecord(writer, ActiveAbilityKey);
      if (Passive != null)
        writer.WriteList(Passive, Key.WriteRecord);
      writer.Write(GroundAccelerationMod);
      writer.Write(CrouchSpeed);
      writer.Write(RunSpeed);
      writer.Write(JumpSpeed);
      writer.Write(SprintSpeed);
      writer.Write(SwimSpeed);
      writer.Write(SwimSprintSpeed);
      writer.Write(JumpHeight);
      if (DoubleJumpHeight.HasValue)
        writer.Write(DoubleJumpHeight.Value);
      writer.Write(InitResourceBonus);
      if (ForceFall != null)
        ForceFall.WriteRecord(writer, ForceFall);
      writer.Write(DrownDamage);
      if (Unlockables != null)
        writer.WriteMap(Unlockables, writer.Write, Unlockable.WriteRecord);
      writer.Write(AimAssistSize);
      writer.Write(AimAssistAdsSize);
      writer.Write(AimAssistCenter);
      writer.Write(AimAssistCrouchSize);
      writer.Write(AimAssistCrouchAdsSize);
      writer.Write(AimAssistCrouchCenter);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(27);
      bitField.Read(reader);
      GuiInfo = bitField[0] ? HeroGuiInfo.ReadRecord(reader) : null;
      Skins = bitField[1] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      if (bitField[2])
        Class = Key.ReadRecord(reader);
      Gears = bitField[3] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      SpecialDevices = bitField[4] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      DefaultDevices = bitField[5] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      if (bitField[6])
        ActiveAbilityKey = Key.ReadRecord(reader);
      Passive = bitField[7] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      if (bitField[8])
        GroundAccelerationMod = reader.ReadSingle();
      if (bitField[9])
        CrouchSpeed = reader.ReadSingle();
      if (bitField[10])
        RunSpeed = reader.ReadSingle();
      if (bitField[11])
        JumpSpeed = reader.ReadSingle();
      if (bitField[12])
        SprintSpeed = reader.ReadSingle();
      if (bitField[13])
        SwimSpeed = reader.ReadSingle();
      if (bitField[14])
        SwimSprintSpeed = reader.ReadSingle();
      if (bitField[15])
        JumpHeight = reader.ReadSingle();
      DoubleJumpHeight = bitField[16] ? reader.ReadSingle() : null;
      if (bitField[17])
        InitResourceBonus = reader.ReadSingle();
      ForceFall = bitField[18] ? ForceFall.ReadRecord(reader) : null;
      if (bitField[19])
        DrownDamage = reader.ReadSingle();
      Unlockables = bitField[20] ? reader.ReadMap<int, Unlockable, Dictionary<int, Unlockable>>(reader.ReadInt32, Unlockable.ReadRecord) : null;
      if (bitField[21])
        AimAssistSize = reader.ReadVector3();
      if (bitField[22])
        AimAssistAdsSize = reader.ReadVector3();
      if (bitField[23])
        AimAssistCenter = reader.ReadVector3();
      if (bitField[24])
        AimAssistCrouchSize = reader.ReadVector3();
      if (bitField[25])
        AimAssistCrouchAdsSize = reader.ReadVector3();
      if (!bitField[26])
        return;
      AimAssistCrouchCenter = reader.ReadVector3();
    }

    public static void WriteRecord(BinaryWriter writer, UnitDataPlayer value)
    {
      value.Write(writer);
    }

    public static UnitDataPlayer ReadRecord(BinaryReader reader)
    {
      var unitDataPlayer = new UnitDataPlayer();
      unitDataPlayer.Read(reader);
      return unitDataPlayer;
    }
}