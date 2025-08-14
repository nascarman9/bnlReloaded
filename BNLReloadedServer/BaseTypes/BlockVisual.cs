using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class BlockVisual
{
    public BlockVisualType Type { get; set; }

    public string? Icon { get; set; }

    public LocalizedString? Name { get; set; }

    public Key? Material { get; set; }

    public BlockColliderType Collider { get; set; }

    public List<string>? Materials { get; set; }

    public List<List<byte>>? TextureIndices { get; set; }

    public List<byte>? DecalIndices { get; set; }

    public List<BlockPrefab>? Prefabs { get; set; }

    public bool? FaceAlign { get; set; }

    public bool? Rotation { get; set; }

    public bool? CanBePassedByShot { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, Icon != null, Name != null, Material.HasValue, true, Materials != null, TextureIndices != null,
        DecalIndices != null, Prefabs != null, FaceAlign.HasValue, Rotation.HasValue,
        CanBePassedByShot.HasValue).Write(writer);
      writer.WriteByteEnum(Type);
      if (Icon != null)
        writer.Write(Icon);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Material.HasValue)
        Key.WriteRecord(writer, Material.Value);
      writer.WriteByteEnum(Collider);
      if (Materials != null)
        writer.WriteList(Materials, writer.Write);
      if (TextureIndices != null)
        writer.WriteList(TextureIndices, item => writer.WriteList(item, writer.Write));
      if (DecalIndices != null)
        writer.WriteList(DecalIndices, writer.Write);
      if (Prefabs != null)
        writer.WriteList(Prefabs, BlockPrefab.WriteRecord);
      if (FaceAlign.HasValue)
        writer.Write(FaceAlign.Value);
      if (Rotation.HasValue)
        writer.Write(Rotation.Value);
      if (!CanBePassedByShot.HasValue)
        return;
      writer.Write(CanBePassedByShot.Value);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(12);
      bitField.Read(reader);
      if (bitField[0])
        Type = reader.ReadByteEnum<BlockVisualType>();
      Icon = bitField[1] ? reader.ReadString() : null;
      Name = bitField[2] ? LocalizedString.ReadRecord(reader) : null;
      Material = bitField[3] ? Key.ReadRecord(reader) : null;
      if (bitField[4])
        Collider = reader.ReadByteEnum<BlockColliderType>();
      Materials = bitField[5] ? reader.ReadList<string, List<string>>(reader.ReadString) : null;
      TextureIndices = bitField[6] ? reader.ReadList<List<byte>, List<List<byte>>>(() => reader.ReadList<byte, List<byte>>(reader.ReadByte)) : null;
      DecalIndices = bitField[7] ? reader.ReadList<byte, List<byte>>(reader.ReadByte) : null;
      Prefabs = bitField[8] ? reader.ReadList<BlockPrefab, List<BlockPrefab>>(BlockPrefab.ReadRecord) : null;
      FaceAlign = bitField[9] ? reader.ReadBoolean() : null;
      Rotation = bitField[10] ? reader.ReadBoolean() : null;
      CanBePassedByShot = bitField[11] ? reader.ReadBoolean() : null;
    }

    public static void WriteRecord(BinaryWriter writer, BlockVisual value) => value.Write(writer);

    public static BlockVisual ReadRecord(BinaryReader reader)
    {
      var blockVisual = new BlockVisual();
      blockVisual.Read(reader);
      return blockVisual;
    }
}