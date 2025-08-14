using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ProtocolInterfaces;

namespace BNLReloadedServer.BaseTypes;

public class CardEffect : Card, IUnlockable
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Effect;

    public string? PrefabUnit { get; set; }

    public string? PrefabPlayer { get; set; }

    public SoundLoopReference? SoundForPlayer { get; set; }

    public SoundLoopReference? SoundForUnit { get; set; }

    public EffectGuiInfo? GuiInfo { get; set; }

    public bool Positive { get; set; }

    public EffectInterrupt? Interrupt { get; set; }

    public float? Scores { get; set; }

    public float? Duration { get; set; }

    public List<EffectLabel>? Labels { get; set; }

    public ConstEffect? Effect { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, PrefabUnit != null, PrefabPlayer != null, SoundForPlayer != null,
        SoundForUnit != null, GuiInfo != null, true, Interrupt != null, Scores.HasValue, Duration.HasValue,
        Labels != null, Effect != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (PrefabUnit != null)
        writer.Write(PrefabUnit);
      if (PrefabPlayer != null)
        writer.Write(PrefabPlayer);
      if (SoundForPlayer != null)
        SoundLoopReference.WriteRecord(writer, SoundForPlayer);
      if (SoundForUnit != null)
        SoundLoopReference.WriteRecord(writer, SoundForUnit);
      if (GuiInfo != null)
        EffectGuiInfo.WriteRecord(writer, GuiInfo);
      writer.Write(Positive);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Scores.HasValue)
        writer.Write(Scores.Value);
      if (Duration.HasValue)
        writer.Write(Duration.Value);
      if (Labels != null)
        writer.WriteList(Labels, writer.WriteByteEnum);
      if (Effect != null)
        ConstEffect.WriteVariant(writer, Effect);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(13);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      PrefabUnit = bitField[2] ? reader.ReadString() : null;
      PrefabPlayer = bitField[3] ? reader.ReadString() : null;
      SoundForPlayer = bitField[4] ? SoundLoopReference.ReadRecord(reader) : null;
      SoundForUnit = bitField[5] ? SoundLoopReference.ReadRecord(reader) : null;
      GuiInfo = bitField[6] ? EffectGuiInfo.ReadRecord(reader) : null;
      if (bitField[7])
        Positive = reader.ReadBoolean();
      Interrupt = bitField[8] ? EffectInterrupt.ReadRecord(reader) : null;
      Scores = bitField[9] ? reader.ReadSingle() : null;
      Duration = bitField[10] ? reader.ReadSingle() : null;
      Labels = bitField[11] ? reader.ReadList<EffectLabel, List<EffectLabel>>(reader.ReadByteEnum<EffectLabel>) : null;
      Effect = bitField[12] ? ConstEffect.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardEffect value) => value.Write(writer);

    public static CardEffect ReadRecord(BinaryReader reader)
    {
      var cardEffect = new CardEffect();
      cardEffect.Read(reader);
      return cardEffect;
    }
}