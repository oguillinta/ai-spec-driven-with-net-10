namespace BancaDigitalPeru.Domain.Accounts;

/// <summary>
/// Estado de la cuenta (spec RF-006). Una cuenta BLOQUEADA sigue siendo visible para su
/// propietario (RB4); esta spec no define transición de estado.
/// </summary>
public enum AccountStatus
{
    Active,
    Blocked
}
