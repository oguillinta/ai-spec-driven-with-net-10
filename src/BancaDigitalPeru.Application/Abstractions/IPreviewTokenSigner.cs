using BancaDigitalPeru.Application.Transfers;

namespace BancaDigitalPeru.Application.Abstractions;

/// <summary>
/// Protege y desprotege criptográficamente el contenido de una vista previa de transferencia, sin
/// persistirlo (research.md §4). La referencia resultante es opaca para el cliente HTTP.
/// </summary>
public interface IPreviewTokenSigner
{
    string Protect(TransferPreviewPayload payload);

    /// <summary>Devuelve <c>null</c> si el token no es válido o fue manipulado.</summary>
    TransferPreviewPayload? Unprotect(string token);
}
