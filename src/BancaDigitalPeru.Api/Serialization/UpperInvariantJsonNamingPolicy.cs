using System.Text.Json;

namespace BancaDigitalPeru.Api.Serialization;

/// <summary>
/// Convierte el nombre de cada miembro de enum a MAYÚSCULAS al serializar/deserializar JSON, para
/// que coincida con los valores declarados en los contratos OpenAPI (p. ej. "ACTIVE"/"BLOCKED",
/// "SAVINGS", "PEN"), que no coinciden con el PascalCase de los enums de Domain (`Active`,
/// `Blocked`, `Savings`). No se anota el enum de Domain con atributos de System.Text.Json (Clean
/// Architecture, Domain no depende de frameworks de serialización): la conversión vive
/// íntegramente en Api, junto con el <see cref="System.Text.Json.Serialization.JsonStringEnumConverter"/>
/// que la usa.
/// </summary>
public sealed class UpperInvariantJsonNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) => name.ToUpperInvariant();
}
