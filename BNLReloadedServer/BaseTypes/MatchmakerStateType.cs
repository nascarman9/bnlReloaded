namespace BNLReloadedServer.BaseTypes;

public enum MatchmakerStateType
{
    None = 1,
    InQueue = 2,
    Confirming = 3,
    ConfirmingBackfilling = 4,
    Aborting = 5
}