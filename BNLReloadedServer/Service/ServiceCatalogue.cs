using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;

namespace BNLReloadedServer.Service;

public class ServiceCatalogue(ISender sender) : IServiceCatalogue
{
    private enum ServiceCatalogueId : byte
    {
        MessageReplicate = 0,
        MessageUpdateCard = 1,
        MessageRemoveCard = 2
    }
    
    private static BinaryWriter CreateWriter()
    {
        var memStream = new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServiceCatalogue);
        return writer;
    }
    
    public void SendReplicate(ICollection<Card> cards)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceCatalogueId.MessageReplicate);
        writer.WriteList(cards, Card.WriteVariant);
        sender.SendToSession(writer);
    }

    public void SendUpdateCard(Card card)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceCatalogueId.MessageUpdateCard);
        Card.WriteVariant(writer, card);
        sender.SendToSession(writer);
    }

    public void SendRemoveCard(string cardId)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceCatalogueId.MessageRemoveCard);
        writer.Write(cardId);
        sender.SendToSession(writer);
    }
    
    public void Receive(BinaryReader reader)
    {
        var serviceCatalogueId = reader.ReadByte();
        Console.WriteLine($"ServiceCatalogueId: {serviceCatalogueId}");
        Console.WriteLine($"Catalogue service received unsupported serviceId: {serviceCatalogueId}");
    }
}