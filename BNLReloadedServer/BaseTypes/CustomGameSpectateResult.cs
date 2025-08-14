namespace BNLReloadedServer.BaseTypes;

public enum CustomGameSpectateResult
{
    Accepted = 1,
    WrongPassword = 2,
    TooManySpectators = 3,
    GameNotStartedYet = 4,
    GameInLobbyYet = 5,
    NoSuchGame = 6
}