namespace BancaDigitalPeru.Domain.Transfers;

/// <summary>
/// Estado de una transferencia persistida. Único miembro, intencionalmente cerrado
/// (research.md §3): una transferencia rechazada nunca se persiste como <see cref="Transfer"/>.
/// </summary>
public enum TransferStatus
{
    Completed
}
