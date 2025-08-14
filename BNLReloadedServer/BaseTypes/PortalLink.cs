using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class PortalLink
{
    public uint? LinkedPortalUnitId { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(LinkedPortalUnitId.HasValue).Write(writer);
        if (!LinkedPortalUnitId.HasValue)
            return;
        writer.Write(LinkedPortalUnitId.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        LinkedPortalUnitId = bitField[0] ? reader.ReadUInt32() : null;
    }

    public static void WriteRecord(BinaryWriter writer, PortalLink value) => value.Write(writer);

    public static PortalLink ReadRecord(BinaryReader reader)
    {
        var portalLink = new PortalLink();
        portalLink.Read(reader);
        return portalLink;
    }
}