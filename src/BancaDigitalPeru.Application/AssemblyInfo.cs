using System.Runtime.CompilerServices;

// Permite probar directamente las reglas de validación internas (CommonTransferValidation,
// OwnAccountTransferValidation, ThirdPartyTransferValidation) sin exponerlas a Infrastructure ni a
// Api — mismo criterio que Domain -> Infrastructure (ver Domain/AssemblyInfo.cs).
[assembly: InternalsVisibleTo("BancaDigitalPeru.Application.UnitTests")]
