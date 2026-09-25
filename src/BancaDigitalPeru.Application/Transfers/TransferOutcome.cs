namespace BancaDigitalPeru.Application.Transfers;

/// <summary>
/// Resultado de un caso de uso de transferencia: éxito con un payload, o rechazo con una razón
/// específica de <see cref="TransferRejectionReason"/> (plan.md "Application Use Case"). A
/// diferencia de <see cref="Common.Result{T}"/> (binario, reutilizado por consultas de solo
/// lectura), esta feature necesita distinguir entre varios motivos de rechazo para mapear cada uno
/// a su propio código HTTP (research.md §8).
/// </summary>
public sealed class TransferOutcome<T>
{
    public bool IsSuccess { get; }

    public T? Value { get; }

    /// <summary>
    /// Cuando <see cref="IsSuccess"/> es <c>true</c>: indica si el resultado es un replay
    /// idempotente de una confirmación ya aplicada anteriormente (200), en vez de una ejecución
    /// nueva (201) — solo relevante para <c>ConfirmOwnAccountTransferUseCase</c>.
    /// </summary>
    public bool IsReplay { get; }

    public TransferRejectionReason? RejectionReason { get; }

    private TransferOutcome(bool isSuccess, T? value, bool isReplay, TransferRejectionReason? rejectionReason)
    {
        IsSuccess = isSuccess;
        Value = value;
        IsReplay = isReplay;
        RejectionReason = rejectionReason;
    }

    public static TransferOutcome<T> Succeeded(T value, bool isReplay = false) => new(true, value, isReplay, null);

    public static TransferOutcome<T> Rejected(TransferRejectionReason reason) => new(false, default, false, reason);
}
