using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Service;

public interface IServiceCatalogue : IService
{
    public void SendReplicate(ICollection<Card> cards);
    public void SendUpdateCard(Card card);
    public void SendRemoveCard(string cardId);
}