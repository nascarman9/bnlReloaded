using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class DemeritData
{
    public LocalizedString? Title { get; set; }

    public int Demerits { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(Title != null, true).Write(writer);
        if (Title != null)
            LocalizedString.WriteRecord(writer, Title);
        writer.Write(Demerits);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        Title = bitField[0] ? LocalizedString.ReadRecord(reader) : null;
        if (!bitField[1])
            return;
        Demerits = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, DemeritData value) => value.Write(writer);

    public static DemeritData ReadRecord(BinaryReader reader)
    {
        var demeritData = new DemeritData();
        demeritData.Read(reader);
        return demeritData;
    }
}