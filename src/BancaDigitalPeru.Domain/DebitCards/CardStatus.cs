namespace BancaDigitalPeru.Domain.DebitCards;

/// <summary>
/// Estado de la tarjeta de débito (spec RF-013). Una tarjeta BLOQUEADA sigue siendo visible para
/// su propietario (RB5); esta spec no define transición de estado.
/// </summary>
public enum CardStatus
{
    Active,
    Blocked
}
