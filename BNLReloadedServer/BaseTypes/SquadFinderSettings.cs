using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SquadFinderSettings
{
    public List<Key>? GameModes { get; set; }

    public List<Locale>? Locales { get; set; }

    public List<Key>? Heroes { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(GameModes != null, Locales != null, Heroes != null).Write(writer);
        if (GameModes != null)
            writer.WriteList(GameModes, Key.WriteRecord);
        if (Locales != null)
            writer.WriteList(Locales, writer.WriteByteEnum);
        if (Heroes == null)
            return;
        writer.WriteList(Heroes, Key.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        GameModes = bitField[0] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
        Locales = bitField[1] ? reader.ReadList<Locale, List<Locale>>(reader.ReadByteEnum<Locale>) : null;
        Heroes = bitField[2] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, SquadFinderSettings value)
    {
        value.Write(writer);
    }

    public static SquadFinderSettings ReadRecord(BinaryReader reader)
    {
        var squadFinderSettings = new SquadFinderSettings();
        squadFinderSettings.Read(reader);
        return squadFinderSettings;
    }
}