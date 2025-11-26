using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ServerTypes;

public record ExtraMaps
{
    public List<Key>? Custom { get; set; }

    public List<Key>? Friendly { get; set; }

    public List<Key>? FriendlyNoob { get; set; }

    public List<Key>? Ranked { get; set; }
}