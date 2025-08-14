using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class BlockPrefab
{
    public string? Icon { get; set; }

    public LocalizedString? Name { get; set; }

    public string? Prefab { get; set; }

    public BlockMirrorType Mirror { get; set; } = BlockMirrorType.Square;

    public bool IsTop { get; set; }

    public bool IsBottom { get; set; }

    public bool IsRight { get; set; }

    public bool IsLeft { get; set; }

    public bool IsForward { get; set; }

    public bool IsBack { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Icon != null, Name != null, Prefab != null, true, true, true, true, true, true, true).Write(writer);
      if (Icon != null)
        writer.Write(Icon);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Prefab != null)
        writer.Write(Prefab);
      writer.WriteByteEnum(Mirror);
      writer.Write(IsTop);
      writer.Write(IsBottom);
      writer.Write(IsRight);
      writer.Write(IsLeft);
      writer.Write(IsForward);
      writer.Write(IsBack);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(10);
      bitField.Read(reader);
      Icon = bitField[0] ? reader.ReadString() : null;
      Name = bitField[1] ? LocalizedString.ReadRecord(reader) : null;
      Prefab = bitField[2] ? reader.ReadString() : null;
      if (bitField[3])
        Mirror = reader.ReadByteEnum<BlockMirrorType>();
      if (bitField[4])
        IsTop = reader.ReadBoolean();
      if (bitField[5])
        IsBottom = reader.ReadBoolean();
      if (bitField[6])
        IsRight = reader.ReadBoolean();
      if (bitField[7])
        IsLeft = reader.ReadBoolean();
      if (bitField[8])
        IsForward = reader.ReadBoolean();
      if (!bitField[9])
        return;
      IsBack = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, BlockPrefab value) => value.Write(writer);

    public static BlockPrefab ReadRecord(BinaryReader reader)
    {
      var blockPrefab = new BlockPrefab();
      blockPrefab.Read(reader);
      return blockPrefab;
    }
}