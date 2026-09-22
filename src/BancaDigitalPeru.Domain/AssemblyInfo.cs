using System.Runtime.CompilerServices;

// Permite que Infrastructure mapee el valor completo de los value objects de número (AccountNumber,
// CardNumber) vía su propiedad interna, sin reflexión y sin exponerlo a Application ni a Api
// (spec FR-018): solo Infrastructure recibe visibilidad de los miembros "internal" de Domain.
[assembly: InternalsVisibleTo("BancaDigitalPeru.Infrastructure")]
