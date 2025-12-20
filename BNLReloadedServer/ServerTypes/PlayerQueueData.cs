using Moserware.Skills;

namespace BNLReloadedServer.ServerTypes;

public record PlayerQueueData(
    uint PlayerId,
    Guid PlayerGuid,
    Rating Rating,
    DateTimeOffset JoinTime,
    ulong? SquadId);