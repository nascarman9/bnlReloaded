using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardMaterial : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Material;

    public string? ImpactPrefab { get; set; }

    public SoundReference? ImpactSound { get; set; }

    public string? ImpactForcefieldPrefab { get; set; }

    public SoundReference? ImpactForcefieldSound { get; set; }

    public string? ImpactCritPrefab { get; set; }

    public SoundReference? ImpactCritSound { get; set; }

    public string? CreatePrefab { get; set; }

    public SoundReference? CreateSound { get; set; }

    public string? DestroyPrefab { get; set; }

    public SoundReference? DestroySound { get; set; }

    public SoundReference? BlockLoopPlay { get; set; }

    public SoundReference? BlockLoopStop { get; set; }

    public string? BlockCenterEffect { get; set; }

    public string? BlockFallingImpactSound { get; set; }

    public string? FootstepSurfaceName { get; set; }

    public SoundReference? PlayerEnterSound { get; set; }

    public SoundReference? PlayerExitSound { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, ImpactPrefab != null, ImpactSound != null, ImpactForcefieldPrefab != null,
        ImpactForcefieldSound != null, ImpactCritPrefab != null, ImpactCritSound != null, CreatePrefab != null,
        CreateSound != null, DestroyPrefab != null, DestroySound != null, BlockLoopPlay != null, BlockLoopStop != null,
        BlockCenterEffect != null, BlockFallingImpactSound != null, FootstepSurfaceName != null,
        PlayerEnterSound != null, PlayerExitSound != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (ImpactPrefab != null)
        writer.Write(ImpactPrefab);
      if (ImpactSound != null)
        SoundReference.WriteRecord(writer, ImpactSound);
      if (ImpactForcefieldPrefab != null)
        writer.Write(ImpactForcefieldPrefab);
      if (ImpactForcefieldSound != null)
        SoundReference.WriteRecord(writer, ImpactForcefieldSound);
      if (ImpactCritPrefab != null)
        writer.Write(ImpactCritPrefab);
      if (ImpactCritSound != null)
        SoundReference.WriteRecord(writer, ImpactCritSound);
      if (CreatePrefab != null)
        writer.Write(CreatePrefab);
      if (CreateSound != null)
        SoundReference.WriteRecord(writer, CreateSound);
      if (DestroyPrefab != null)
        writer.Write(DestroyPrefab);
      if (DestroySound != null)
        SoundReference.WriteRecord(writer, DestroySound);
      if (BlockLoopPlay != null)
        SoundReference.WriteRecord(writer, BlockLoopPlay);
      if (BlockLoopStop != null)
        SoundReference.WriteRecord(writer, BlockLoopStop);
      if (BlockCenterEffect != null)
        writer.Write(BlockCenterEffect);
      if (BlockFallingImpactSound != null)
        writer.Write(BlockFallingImpactSound);
      if (FootstepSurfaceName != null)
        writer.Write(FootstepSurfaceName);
      if (PlayerEnterSound != null)
        SoundReference.WriteRecord(writer, PlayerEnterSound);
      if (PlayerExitSound == null)
        return;
      SoundReference.WriteRecord(writer, PlayerExitSound);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(19);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      ImpactPrefab = bitField[2] ? reader.ReadString() : null;
      ImpactSound = bitField[3] ? SoundReference.ReadRecord(reader) : null;
      ImpactForcefieldPrefab = bitField[4] ? reader.ReadString() : null;
      ImpactForcefieldSound = bitField[5] ? SoundReference.ReadRecord(reader) : null;
      ImpactCritPrefab = bitField[6] ? reader.ReadString() : null;
      ImpactCritSound = bitField[7] ? SoundReference.ReadRecord(reader) : null;
      CreatePrefab = bitField[8] ? reader.ReadString() : null;
      CreateSound = bitField[9] ? SoundReference.ReadRecord(reader) : null;
      DestroyPrefab = bitField[10] ? reader.ReadString() : null;
      DestroySound = bitField[11] ? SoundReference.ReadRecord(reader) : null;
      BlockLoopPlay = bitField[12] ? SoundReference.ReadRecord(reader) : null;
      BlockLoopStop = bitField[13] ? SoundReference.ReadRecord(reader) : null;
      BlockCenterEffect = bitField[14] ? reader.ReadString() : null;
      BlockFallingImpactSound = bitField[15] ? reader.ReadString() : null;
      FootstepSurfaceName = bitField[16] ? reader.ReadString() : null;
      PlayerEnterSound = bitField[17] ? SoundReference.ReadRecord(reader) : null;
      PlayerExitSound = bitField[18] ? SoundReference.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardMaterial value) => value.Write(writer);

    public static CardMaterial ReadRecord(BinaryReader reader)
    {
      var cardMaterial = new CardMaterial();
      cardMaterial.Read(reader);
      return cardMaterial;
    }
}