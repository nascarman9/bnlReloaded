using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ProtocolInterfaces;

namespace BNLReloadedServer.BaseTypes;

public class CardSkin : Card, IPrefab, IFpsPrefab, ILootCrateItem
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Skin;

    public string? Prefab { get; set; }

    public string? FpsPrefab { get; set; }

    public string? IconPortrait { get; set; }

    public string? IconPortraitProfile { get; set; }

    public string? Bundle { get; set; }

    public string? FpsBundle { get; set; }

    public LocalizedString? Name { get; set; }

    public Key HeroKey { get; set; }

    public SoundReference? LearningMusic { get; set; }

    public SoundReference? LockinMusic { get; set; }

    public SoundReference? DeathMusicSting { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Prefab != null, FpsPrefab != null, IconPortrait != null,
        IconPortraitProfile != null, Bundle != null, FpsBundle != null, Name != null, true, LearningMusic != null,
        LockinMusic != null, DeathMusicSting != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Prefab != null)
        writer.Write(Prefab);
      if (FpsPrefab != null)
        writer.Write(FpsPrefab);
      if (IconPortrait != null)
        writer.Write(IconPortrait);
      if (IconPortraitProfile != null)
        writer.Write(IconPortraitProfile);
      if (Bundle != null)
        writer.Write(Bundle);
      if (FpsBundle != null)
        writer.Write(FpsBundle);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      Key.WriteRecord(writer, HeroKey);
      if (LearningMusic != null)
        SoundReference.WriteRecord(writer, LearningMusic);
      if (LockinMusic != null)
        SoundReference.WriteRecord(writer, LockinMusic);
      if (DeathMusicSting == null)
        return;
      SoundReference.WriteRecord(writer, DeathMusicSting);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(13);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Prefab = bitField[2] ? reader.ReadString() : null;
      FpsPrefab = bitField[3] ? reader.ReadString() : null;
      IconPortrait = bitField[4] ? reader.ReadString() : null;
      IconPortraitProfile = bitField[5] ? reader.ReadString() : null;
      Bundle = bitField[6] ? reader.ReadString() : null;
      FpsBundle = bitField[7] ? reader.ReadString() : null;
      Name = bitField[8] ? LocalizedString.ReadRecord(reader) : null;
      if (bitField[9])
        HeroKey = Key.ReadRecord(reader);
      LearningMusic = bitField[10] ? SoundReference.ReadRecord(reader) : null;
      LockinMusic = bitField[11] ? SoundReference.ReadRecord(reader) : null;
      DeathMusicSting = bitField[12] ? SoundReference.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardSkin value) => value.Write(writer);

    public static CardSkin ReadRecord(BinaryReader reader)
    {
      var cardSkin = new CardSkin();
      cardSkin.Read(reader);
      return cardSkin;
    }
}