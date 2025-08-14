using System.Drawing;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardMapLogic : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.MapLogic;

    public string? DefaultPlane { get; set; }

    public string? DefaultRender { get; set; }

    public List<Color>? DefaultColors { get; set; }

    public List<MapEditorPlaneInfo>? PlaneList { get; set; }

    public List<MapEditorRenderInfo>? RenderList { get; set; }

    public List<MapEditorAudioInfo>? AudioList { get; set; }

    public MapEditorModeInfo? ShieldCapture { get; set; }

    public MapEditorModeInfo? ShieldRush { get; set; }

    public MapEditorModeInfo? Tutorial { get; set; }

    public MapEditorModeInfo? TimeTrial { get; set; }

    public List<MapEditorItemList>? Items { get; set; }

    public List<MapEditorLayer>? DefaultGeneratorLayers { get; set; }

    public List<Key>? GeneratorBlocks { get; set; }

    public Vector3s MinSize { get; set; }

    public Vector3s MaxSize { get; set; }

    public int MaxBlocksCount { get; set; }

    public int SteamSmallThreshold { get; set; }

    public int SteamMediumThreshold { get; set; }

    public List<MapEditorDefaultSize>? DefaultGeneratorSizes { get; set; }

    [JsonPropertyName("max_size_brush_2d")]
    public int MaxSizeBrush2D { get; set; }

    [JsonPropertyName("max_size_brush_3d")]
    public int MaxSizeBrush3D { get; set; }

    public Vector3s MaxSizeBrushCustom { get; set; }

    public List<Key>? PlayHeroes { get; set; }

    public Vector3s CopyMaxOffset { get; set; }

    public Vector3s PasteMaxOffset { get; set; }

    public int TreeMaxRadius { get; set; }

    public int TreeMaxHeight { get; set; }

    public int PrefabsLimitWarning { get; set; }

    public int PrefabsLimitRefusing { get; set; }

    public int ForceGatesLimitWarning { get; set; }

    public int ForceGatesLimitRefusing { get; set; }

    public Dictionary<MapEditorToolType, List<MapToolTip>>? SpecificToolTips { get; set; }

    public float CameraMinFov { get; set; }

    public float CameraMaxFov { get; set; }

    public float CameraDefaultFov { get; set; }

    public float CameraMinSpeed { get; set; }

    public float CameraMaxSpeed { get; set; }

    public float CameraDefaultSpeed { get; set; }

    public float CameraMinSensitivity { get; set; }

    public float CameraMaxSensitivity { get; set; }

    public float CameraDefaultSensitivity { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, DefaultPlane != null, DefaultRender != null, DefaultColors != null,
        PlaneList != null, RenderList != null, AudioList != null, ShieldCapture != null, ShieldRush != null,
        Tutorial != null, TimeTrial != null, Items != null, DefaultGeneratorLayers != null, GeneratorBlocks != null,
        true, true, true, true, true, DefaultGeneratorSizes != null, true, true, true, PlayHeroes != null, true, true,
        true, true, true, true, true, true, SpecificToolTips != null, true, true, true, true, true, true, true, true,
        true).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (DefaultPlane != null)
        writer.Write(DefaultPlane);
      if (DefaultRender != null)
        writer.Write(DefaultRender);
      if (DefaultColors != null)
        writer.WriteList(DefaultColors, writer.Write);
      if (PlaneList != null)
        writer.WriteList(PlaneList, MapEditorPlaneInfo.WriteRecord);
      if (RenderList != null)
        writer.WriteList(RenderList, MapEditorRenderInfo.WriteRecord);
      if (AudioList != null)
        writer.WriteList(AudioList, MapEditorAudioInfo.WriteRecord);
      if (ShieldCapture != null)
        MapEditorModeInfo.WriteRecord(writer, ShieldCapture);
      if (ShieldRush != null)
        MapEditorModeInfo.WriteRecord(writer, ShieldRush);
      if (Tutorial != null)
        MapEditorModeInfo.WriteRecord(writer, Tutorial);
      if (TimeTrial != null)
        MapEditorModeInfo.WriteRecord(writer, TimeTrial);
      if (Items != null)
        writer.WriteList(Items, MapEditorItemList.WriteRecord);
      if (DefaultGeneratorLayers != null)
        writer.WriteList(DefaultGeneratorLayers, MapEditorLayer.WriteRecord);
      if (GeneratorBlocks != null)
        writer.WriteList(GeneratorBlocks, Key.WriteRecord);
      writer.Write(MinSize);
      writer.Write(MaxSize);
      writer.Write(MaxBlocksCount);
      writer.Write(SteamSmallThreshold);
      writer.Write(SteamMediumThreshold);
      if (DefaultGeneratorSizes != null)
        writer.WriteList(DefaultGeneratorSizes, MapEditorDefaultSize.WriteRecord);
      writer.Write(MaxSizeBrush2D);
      writer.Write(MaxSizeBrush3D);
      writer.Write(MaxSizeBrushCustom);
      if (PlayHeroes != null)
        writer.WriteList(PlayHeroes, Key.WriteRecord);
      writer.Write(CopyMaxOffset);
      writer.Write(PasteMaxOffset);
      writer.Write(TreeMaxRadius);
      writer.Write(TreeMaxHeight);
      writer.Write(PrefabsLimitWarning);
      writer.Write(PrefabsLimitRefusing);
      writer.Write(ForceGatesLimitWarning);
      writer.Write(ForceGatesLimitRefusing);
      if (SpecificToolTips != null)
        writer.WriteMap(SpecificToolTips, writer.WriteByteEnum, item => writer.WriteList(item, MapToolTip.WriteRecord));
      writer.Write(CameraMinFov);
      writer.Write(CameraMaxFov);
      writer.Write(CameraDefaultFov);
      writer.Write(CameraMinSpeed);
      writer.Write(CameraMaxSpeed);
      writer.Write(CameraDefaultSpeed);
      writer.Write(CameraMinSensitivity);
      writer.Write(CameraMaxSensitivity);
      writer.Write(CameraDefaultSensitivity);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(43);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      DefaultPlane = bitField[2] ? reader.ReadString() : null;
      DefaultRender = bitField[3] ? reader.ReadString() : null;
      DefaultColors = bitField[4] ? reader.ReadList<Color, List<Color>>(reader.ReadColor) : null;
      PlaneList = bitField[5] ? reader.ReadList<MapEditorPlaneInfo, List<MapEditorPlaneInfo>>(MapEditorPlaneInfo.ReadRecord) : null;
      RenderList = bitField[6] ? reader.ReadList<MapEditorRenderInfo, List<MapEditorRenderInfo>>(MapEditorRenderInfo.ReadRecord) : null;
      AudioList = bitField[7] ? reader.ReadList<MapEditorAudioInfo, List<MapEditorAudioInfo>>(MapEditorAudioInfo.ReadRecord) : null;
      ShieldCapture = bitField[8] ? MapEditorModeInfo.ReadRecord(reader) : null;
      ShieldRush = bitField[9] ? MapEditorModeInfo.ReadRecord(reader) : null;
      Tutorial = bitField[10] ? MapEditorModeInfo.ReadRecord(reader) : null;
      TimeTrial = bitField[11] ? MapEditorModeInfo.ReadRecord(reader) : null;
      Items = bitField[12] ? reader.ReadList<MapEditorItemList, List<MapEditorItemList>>(MapEditorItemList.ReadRecord) : null;
      DefaultGeneratorLayers = bitField[13] ? reader.ReadList<MapEditorLayer, List<MapEditorLayer>>(MapEditorLayer.ReadRecord) : null;
      GeneratorBlocks = bitField[14] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      if (bitField[15])
        MinSize = reader.ReadVector3s();
      if (bitField[16])
        MaxSize = reader.ReadVector3s();
      if (bitField[17])
        MaxBlocksCount = reader.ReadInt32();
      if (bitField[18])
        SteamSmallThreshold = reader.ReadInt32();
      if (bitField[19])
        SteamMediumThreshold = reader.ReadInt32();
      DefaultGeneratorSizes = bitField[20] ? reader.ReadList<MapEditorDefaultSize, List<MapEditorDefaultSize>>(MapEditorDefaultSize.ReadRecord) : null;
      if (bitField[21])
        MaxSizeBrush2D = reader.ReadInt32();
      if (bitField[22])
        MaxSizeBrush3D = reader.ReadInt32();
      if (bitField[23])
        MaxSizeBrushCustom = reader.ReadVector3s();
      PlayHeroes = bitField[24] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      if (bitField[25])
        CopyMaxOffset = reader.ReadVector3s();
      if (bitField[26])
        PasteMaxOffset = reader.ReadVector3s();
      if (bitField[27])
        TreeMaxRadius = reader.ReadInt32();
      if (bitField[28])
        TreeMaxHeight = reader.ReadInt32();
      if (bitField[29])
        PrefabsLimitWarning = reader.ReadInt32();
      if (bitField[30])
        PrefabsLimitRefusing = reader.ReadInt32();
      if (bitField[31])
        ForceGatesLimitWarning = reader.ReadInt32();
      if (bitField[32])
        ForceGatesLimitRefusing = reader.ReadInt32();
      SpecificToolTips = bitField[33] ? reader.ReadMap<MapEditorToolType, List<MapToolTip>, Dictionary<MapEditorToolType, List<MapToolTip>>>(reader.ReadByteEnum<MapEditorToolType>, () => reader.ReadList<MapToolTip, List<MapToolTip>>(MapToolTip.ReadRecord)) : null;
      if (bitField[34])
        CameraMinFov = reader.ReadSingle();
      if (bitField[35])
        CameraMaxFov = reader.ReadSingle();
      if (bitField[36])
        CameraDefaultFov = reader.ReadSingle();
      if (bitField[37])
        CameraMinSpeed = reader.ReadSingle();
      if (bitField[38])
        CameraMaxSpeed = reader.ReadSingle();
      if (bitField[39])
        CameraDefaultSpeed = reader.ReadSingle();
      if (bitField[40])
        CameraMinSensitivity = reader.ReadSingle();
      if (bitField[41])
        CameraMaxSensitivity = reader.ReadSingle();
      if (!bitField[42])
        return;
      CameraDefaultSensitivity = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, CardMapLogic value) => value.Write(writer);

    public static CardMapLogic ReadRecord(BinaryReader reader)
    {
      var cardMapLogic = new CardMapLogic();
      cardMapLogic.Read(reader);
      return cardMapLogic;
    }
}