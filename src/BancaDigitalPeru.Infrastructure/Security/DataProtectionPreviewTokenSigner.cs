using System.Security.Cryptography;
using System.Text.Json;
using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Application.Transfers;
using Microsoft.AspNetCore.DataProtection;

namespace BancaDigitalPeru.Infrastructure.Security;

/// <summary>
/// Protege/desprotege la referencia de vista previa con <see cref="IDataProtectionProvider"/>
/// (research.md §4): un token firmado y sin estado, nunca persistido. Un token manipulado, con
/// firma inválida, o firmado con una clave distinta (rotación de key ring) hace que
/// <see cref="Unprotect"/> devuelva <c>null</c> en vez de propagar la excepción de Data Protection.
/// </summary>
public sealed class DataProtectionPreviewTokenSigner : IPreviewTokenSigner
{
    private const string Purpose = "BancaDigitalPeru.Transfers.TransferPreview.v1";

    private readonly IDataProtector _protector;

    public DataProtectionPreviewTokenSigner(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public string Protect(TransferPreviewPayload payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return _protector.Protect(json);
    }

    public TransferPreviewPayload? Unprotect(string token)
    {
        try
        {
            var json = _protector.Unprotect(token);
            return JsonSerializer.Deserialize<TransferPreviewPayload>(json);
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
