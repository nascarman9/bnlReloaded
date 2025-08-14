namespace BNLReloadedServer.ProtocolInterfaces;

public interface IInternalDevice
{
    float? BuildTime { get; set; }

    float? Cooldown { get; set; }

    float? BaseCost { get; set; }

    float? CostIncPerUnit { get; set; }

    int Level { get; set; }
}