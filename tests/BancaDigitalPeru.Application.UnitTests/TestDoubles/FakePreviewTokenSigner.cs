using System.Text.Json;
using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Application.Transfers.PreviewOwnAccountTransfer;

namespace BancaDigitalPeru.Application.UnitTests.TestDoubles;

/// <summary>Doble de prueba: serializa el payload sin criptografía real, suficiente para probar Application en aislamiento.</summary>
public sealed class FakePreviewTokenSigner : IPreviewTokenSigner
{
    public string Protect(TransferPreviewPayload payload) => JsonSerializer.Serialize(payload);

    public TransferPreviewPayload? Unprotect(string token)
    {
        try
        {
            return JsonSerializer.Deserialize<TransferPreviewPayload>(token);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
