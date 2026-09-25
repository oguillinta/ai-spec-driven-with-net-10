# Implementation Plan: Transferencias a cuentas de terceros

**Branch**: `003-transferencias-terceros` | **Date**: 2026-09-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-transferencias-terceros/spec.md`, más un input
de planificación detallado que exige reutilizar íntegramente las garantías financieras de `002`
(atomicidad, concurrencia, idempotencia, trazabilidad) y documentar explícitamente qué es común
entre ambas features de transferencia y qué es propio de cada una.

## Summary

Permitir que el cliente actual (ordenante) transfiera dinero desde una cuenta propia hacia la
cuenta de **otro** cliente del mismo banco, identificada por su número de cuenta completo, con el
mismo flujo de dos pasos —vista previa (sin efecto financiero) y confirmación— ya establecido en
`002`. La feature reutiliza sin cambios `Account.Debit`/`Credit`, `Transfer`, `Money`,
`IUnitOfWork`, la concurrencia optimista `xmin`, la idempotencia por `Idempotency-Key` y el
mecanismo de vista previa firmada (research.md §0). Lo genuinamente nuevo es: resolver la cuenta
destino por número de cuenta en vez de por identificador interno (research.md §3), un modelo de
privacidad distinto para el destino (revela su existencia y un nombre parcialmente oculto, en vez
de ocultarla — research.md §7), y la unificación deliberada de los pasos de confirmación y
consulta de resultado en casos de uso únicos compartidos con `002` (research.md §5), en vez de
duplicarlos.

## Technical Context

**Language/Version**: C# 13 sobre .NET 10 (`net10.0`) — igual que `001`/`002`

**Primary Dependencies**: ASP.NET Core 10 · Entity Framework Core 10 ·
Npgsql.EntityFrameworkCore.PostgreSQL · FluentValidation · Scalar.AspNetCore ·
`Microsoft.AspNetCore.DataProtection.Abstractions` (ya adoptada en `002`, reutilizada sin cambios) ·
`Testcontainers.PostgreSql` (ya adoptado, reutilizado)

**Storage**: PostgreSQL 17/18 vía EF Core 10 / Npgsql (misma base de datos e instancia que
`001`/`002`)

**Testing**: xUnit; `Testcontainers.PostgreSql` para `IntegrationTests` (incluye el nuevo
escenario de créditos concurrentes al mismo destino, research.md §2); dobles de prueba para
`Application.UnitTests`

**Target Platform**: Mismo proceso Api de `001`/`002` (no se crea un servicio nuevo)

**Performance Goals**: Sin objetivos cuantitativos propios más allá de la corrección financiera
exigida por la spec (Principio I)

**Constraints**: Operación de escritura financiera con atomicidad, idempotencia y protección de
concurrencia obligatorias, reutilizando íntegramente los mecanismos ya adoptados en `002`
(constitución Principio V); sin autenticación (spec, fuera de alcance); sin comisiones ni
multidivisa; API sin estado en memoria, misma limitación conocida del key ring de Data Protection
que `002` (research.md de 002 §9), sin agravarse por esta feature

**Scale/Scope**: Alcance de demostración académica, mismo esquema de clientes y cuentas ficticias
de `001`/`002` con un ajuste menor de datos (research.md §9); no se diseña para volumen de
producción

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principios (constitution.md v1.0.0)

| Principio | Evaluación | Estado |
|---|---|---|
| I. Simplicidad ante todo | Reutilización máxima de `002` (research.md §0): cero cambios a `Transfer`, `IUnitOfWork`, idempotencia, concurrencia; solo 2 métodos nuevos de repositorio, 1 repositorio mínimo nuevo, y un refactor acotado (`CommonTransferValidation`) en vez de duplicar reglas. Toda desviación real se registra en Complexity Tracking. | PASS |
| II. Especificaciones primero y alcance gobernado por ellas | El plan implementa únicamente FR-001…FR-031 de `spec.md` (`Status: Approved`) | PASS |
| III. Mercado peruano | PEN fija (RB4), mensajes de error en español de Perú, fecha/hora en zona horaria de Perú (spec, Assumptions), nombres ficticios de clientes ahora representativos del mercado peruano (research.md §9) | PASS |
| IV. Clean Architecture y responsabilidades separadas | Dependency Rule idéntica a `001`/`002`; nuevas reglas de negocio residen en Application (`ThirdPartyTransferValidation`, `CommonTransferValidation`) y Domain (`Customer.DisplayNameMasked`), nunca en Infrastructure/Api (research.md §5/§6) | PASS |
| V. Integridad financiera | Atomicidad, idempotencia y trazabilidad 100% reutilizadas de `002` sin modificación (research.md §0/§1); concurrencia verificada explícitamente también para créditos concurrentes al destino, no solo débitos del origen (research.md §2) | PASS |
| VI. Seguridad y mínimo privilegio | Filtrado de propiedad server-side para el origen (sin cambios); resolución de destino por número sin exponer datos financieros (research.md §3); `GetByIdAsync`/`GetByNumberAsync` nunca invocados con input directo del cliente salvo el número de cuenta ya validado en formato (research.md §3, nota de seguridad); data minimization por construcción para el destinatario (data-model.md, "Cómo se mantiene la privacidad") | PASS |
| VII. Comportamiento verificable | Todos los endpoints trazan a criterios Dado/Cuando/Entonces de `spec.md` (tabla en quickstart.md) | PASS |
| VIII. Calidad automatizada | Suite de pruebas reutiliza helpers de `002`; se añade cobertura específica solo donde hay comportamiento nuevo (destino por número, privacidad del destinatario, créditos concurrentes) — ver Testing Strategy | PASS |
| IX. Decisiones técnicas justificada | Cada decisión (unificación de Confirm/Get, separación de Preview, nuevo índice, nuevo repositorio) justificada en `research.md` con alternativas descartadas | PASS |

**Nota de gobernanza (Principio II)**: `spec.md` fue clarificada el 2026-09-25 (3 preguntas
resueltas, ver `## Clarifications` en spec.md) y aprobada (`Status: Approved`) tras el hallazgo D1
de `/speckit-analyze`, que además corrigió una omisión menor en SC-002 (F1: "cuenta destino
BLOQUEADA" no aparecía en su lista de motivos de rechazo, aunque FR-008 y su cobertura de pruebas
ya la contemplaban) — mismo patrón de gobernanza ya aplicado en `002`.

### Gate adicional del input de planificación (sección 29)

- [x] El alcance está limitado a `003-transferencias-terceros`.
- [x] No se introducen interbank transfers ni CCI (fuera de alcance, spec §9).
- [x] Se respeta Clean Architecture (ver "Dependency Direction").
- [x] Se reutiliza el diseño financiero de `002` (research.md §0, tabla completa).
- [x] No se duplica innecesariamente `Transfer` (research.md §8: sin cambios de modelo).
- [x] No se duplica Unit of Work (research.md §0: mismo `IUnitOfWork`/`UnitOfWork`).
- [x] No se duplica la estrategia de idempotencia (research.md §1).
- [x] No se duplica la estrategia de concurrencia (research.md §2).
- [x] La cuenta origen pertenece al cliente actual (`GetByIdForCustomerAsync`, sin cambios).
- [x] La cuenta destino pertenece a otro cliente (`ThirdPartyTransferValidation`, research.md §6).
- [x] Se preserva la privacidad del destinatario (research.md §3/§6/§7; data minimization por construcción).
- [x] Money conserva exactitud (reutiliza `Money` de `002` sin cambios).
- [x] La operación es atómica (misma `SaveChangesAsync` única, research.md §0).
- [x] La concurrencia evita overspending y lost updates (research.md §2, incluye el escenario nuevo de créditos concurrentes).
- [x] La idempotencia evita doble efecto (research.md §1).
- [x] El contrato OpenAPI existe antes de implementación (`third-party-transfers-v1.yaml` completo; `own-account-transfers-v1.yaml` actualizado aditivamente, research.md §10).
- [x] Las pruebas financieras críticas están contempladas (ver Testing Strategy).
- [x] La solución sigue siendo simple (research.md, "alternativa más simple descartada" en cada sección).
- [x] Toda complejidad adicional está justificada (ver Complexity Tracking).

**Gate: APROBADO.**

## Impact Analysis (respecto a `001` y `002`)

**Sobre `001`**: Ningún impacto. `001` no se toca (ni `Account`/`AccountNumber`/`DebitCard`
cambian de forma, ni sus contratos ni sus casos de uso).

**Sobre `002`**: Impacto real y acotado, íntegramente aditivo/refactor sin cambio de
comportamiento observable para `002`:

| Archivo/tipo | Cambio | ¿Rompe algo de `002`? |
|---|---|---|
| `own-account-transfers-v1.yaml` | Campo opcional nuevo en `TransferResult`; notas de reutilización en 3 descripciones | No — aditivo, cubierto por el "al menos" de FR-018 de `002` |
| `ConfirmOwnAccountTransferUseCase` → `ConfirmTransferUseCase` | Renombrado + generalizado (rama own-account preservada exactamente) | No debería — requiere reejecutar la suite de `Application.UnitTests`/`IntegrationTests` de `002` contra la clase renombrada como parte de las tareas de esta feature |
| `GetOwnAccountTransferUseCase` → `GetTransferUseCase` | Ídem | Ídem |
| `OwnAccountTransferValidation` | Refactor: delega en `CommonTransferValidation` nueva | No — mismas comprobaciones, mismo orden, mismo resultado |
| `TransferPreviewPayload`/`TransferPreviewResult`/`TransferResultDto` | Reubicados fuera de `PreviewOwnAccountTransfer/`; los dos últimos ganan un campo opcional | No — mismos campos existentes sin cambios, solo namespace/ubicación de archivo y un campo nuevo que `002` deja en `null` |
| `TransferRejectionReason` | 2 valores nuevos agregados al final del enum | No — valores existentes sin cambios |
| `DataProtectionPreviewTokenSigner` | `Purpose` renombrado (research.md §4) | No — cambio interno, no observable por HTTP |
| `AccountConfiguration` | Nuevo índice único en `number` | No — aditivo, no afecta ninguna consulta existente |
| `TransfersController`/`TransferOutcomeMapping` | Extendidos con la acción y los casos nuevos | No — acciones/casos existentes sin cambios |

Esta tabla es la entrada principal para las tareas de "migración" que deberá generar
`/speckit-tasks`: son refactors controlados de código ya implementado y probado de `002`, no
reimplementaciones. Cada uno DEBE mantener verde la suite de pruebas existente de `002` antes de
añadir cobertura nueva (Principio VIII).

## Project Structure

### Documentation (this feature)

```text
specs/003-transferencias-terceros/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── openapi/
│       └── third-party-transfers-v1.yaml
└── tasks.md              # Phase 2 (/speckit-tasks — no generado por este comando)
```

### Source Code (repository root — extiende la solución existente, no crea proyectos nuevos)

```text
src/
├── BancaDigitalPeru.Domain/
│   └── Customers/
│       └── Customer.cs                                     # MODIFICADO: + DisplayNameMasked
│
├── BancaDigitalPeru.Application/
│   ├── Abstractions/
│   │   └── Persistence/
│   │       ├── IAccountRepository.cs                       # MODIFICADO: + GetByNumberAsync, + GetByIdAsync
│   │       └── ICustomerRepository.cs                      # NUEVO
│   └── Transfers/
│       ├── TransferPreviewPayload.cs                       # REUBICADO desde PreviewOwnAccountTransfer/
│       ├── TransferPreviewResult.cs                        # REUBICADO + campo opcional nuevo
│       ├── TransferResultDto.cs                             # MODIFICADO: + campo opcional
│       ├── TransferRejectionReason.cs                       # MODIFICADO: + 2 valores
│       ├── CommonTransferValidation.cs                      # NUEVO (extraído)
│       ├── OwnAccountTransferValidation.cs                   # MODIFICADO: delega en CommonTransferValidation
│       ├── ThirdPartyTransferValidation.cs                   # NUEVO
│       ├── PreviewOwnAccountTransfer/
│       │   └── PreviewOwnAccountTransferUseCase.cs           # MODIFICADO: imports (tipos reubicados)
│       ├── PreviewThirdPartyTransfer/                        # NUEVO
│       │   └── PreviewThirdPartyTransferUseCase.cs
│       ├── ConfirmTransfer/                                   # RENOMBRADO desde ConfirmOwnAccountTransfer/
│       │   └── ConfirmTransferUseCase.cs                      # RENOMBRADO + generalizado
│       └── GetTransfer/                                       # RENOMBRADO desde GetOwnAccountTransfer/
│           └── GetTransferUseCase.cs                          # RENOMBRADO + generalizado
│
├── BancaDigitalPeru.Infrastructure/
│   ├── Persistence/
│   │   ├── Configurations/
│   │   │   └── AccountConfiguration.cs                       # MODIFICADO: + índice único en number
│   │   └── Repositories/
│   │       ├── AccountRepository.cs                          # MODIFICADO: + 2 métodos
│   │       └── CustomerRepository.cs                          # NUEVO
│   └── Security/
│       └── DataProtectionPreviewTokenSigner.cs                # MODIFICADO: Purpose renombrado
│
└── BancaDigitalPeru.Api/
    ├── Controllers/
    │   └── TransfersController.cs                             # MODIFICADO: + acción de vista previa de terceros
    ├── Contracts/
    │   └── Transfers/
    │       └── ThirdPartyTransferContracts.cs                  # NUEVO
    ├── Validation/
    │   └── ThirdPartyTransferRequestValidators.cs               # NUEVO
    ├── ErrorHandling/
    │   └── TransferOutcomeMapping.cs                            # MODIFICADO: + 2 casos
    └── OpenApi/
        └── OpenApiEndpoints.cs                                 # MODIFICADO: + fuente third-party-transfers-v1

src/BancaDigitalPeru.Infrastructure/Migrations/
    └── <timestamp>_AddThirdPartyTransferSupport.cs             # NUEVA migración (research.md §9)

tests/
├── BancaDigitalPeru.Domain.UnitTests/Customers/CustomerDisplayNameMaskedTests.cs   # NUEVO
├── BancaDigitalPeru.Application.UnitTests/Transfers/                              # renombrados + nuevos
└── BancaDigitalPeru.IntegrationTests/Api/
    ├── TransfersEndpointTests.cs                     # referencias renombradas donde aplique
    └── ThirdPartyTransferConcurrencyTests.cs          # NUEVO (créditos concurrentes al destino)
```

**Structure Decision**: Se extiende `BancaDigitalPeru.slnx` existente; no se crean proyectos
nuevos. Los archivos renombrados/reubicados mantienen exactamente el mismo layout por
feature-folder ya usado (`Transfers/<UseCase>/`), ahora con los conceptos verdaderamente
compartidos (`ConfirmTransfer/`, `GetTransfer/`, tipos sueltos bajo `Transfers/`) al mismo nivel
que los conceptos propios de cada tipo de transferencia
(`PreviewOwnAccountTransfer/`, `PreviewThirdPartyTransfer/`).

## Dependency Direction

Idéntica a `001`/`002` (sin cambios):

```text
BancaDigitalPeru.Infrastructure ──> BancaDigitalPeru.Application ──> BancaDigitalPeru.Domain
BancaDigitalPeru.Api ──────────────> BancaDigitalPeru.Application
BancaDigitalPeru.Api ──────────────> BancaDigitalPeru.Infrastructure   (solo Composition Root)
```

Ninguna dependencia nueva entre proyectos; `ICustomerRepository` sigue el mismo patrón que
`IAccountRepository`/`ITransferRepository` (definida en Application, implementada en
Infrastructure).

## Domain Reuse Strategy

Ver [data-model.md](./data-model.md) para el detalle completo. Resumen:

- **Reutilizado sin ningún cambio**: `Account.Debit`/`Credit`, `Transfer.Create`, `Money`,
  `AccountId`, `TransferId`, `IdempotencyKey`, `TransferStatus` (research.md §0/§8).
- **Único cambio de Domain**: `Customer.DisplayNameMasked` (propiedad computada, mismo patrón que
  `AccountNumber.Masked`/`CardNumber.Masked`), necesaria para FR-011 (data-model.md).
- **Invariantes financieras generales** (residen en Domain, sin cambios): importe válido, saldo
  suficiente, precisión monetaria, cuenta elegible para débito, conservación del dinero —
  `Account.Debit`/`Credit`/`Money.Create`.
- **Reglas específicas de terceros** (residen en Application, no en Domain, porque requieren
  coordinar dos Aggregate Roots y el contexto del cliente actual — igual criterio que `002`,
  research.md de 002 §1): cuenta origen del cliente actual, cuenta destino de otro cliente, no
  revelar información financiera del destinatario — `ThirdPartyTransferValidation` +
  `GetTransferUseCase`/`ConfirmTransferUseCase` (research.md §5/§6).

## Aggregate Boundaries

Sin cambios respecto de `002` (research.md de 002 §1, reutilizado íntegramente): `Account` y
`Transfer` siguen siendo Aggregate Roots independientes. Ninguna transferencia (propia o a
terceros) requiere que las cuentas compartan una raíz común; la única regla multi-cuenta ("origen
del cliente actual, destino de otro cliente, ambas ACTIVAS") sigue siendo responsabilidad del caso
de uso de Application, nunca de un agregado compuesto (sección 6 del input de planificación, "NO
agrupar artificialmente Customer + Source Account + Destination Account + Transfer").

| Invariante | Protegida por |
|---|---|
| Cuenta origen ACTIVA y con saldo suficiente al debitar | `Account.Debit` (Domain) — sin cambios |
| Cuenta destino ACTIVA al acreditar | `Account.Credit` (Domain) — sin cambios |
| Cuenta origen pertenece al cliente actual | `IAccountRepository.GetByIdForCustomerAsync` — sin cambios |
| Cuenta destino pertenece a un cliente **distinto** (terceros) / **igual** (propias) | `ThirdPartyTransferValidation` / `OwnAccountTransferValidation` (Application) |
| Débito + crédito + registro de `Transfer` atómicos | `IUnitOfWork.SaveChangesAsync()` — sin cambios |
| No duplicar el movimiento ante solicitud repetida | Índice único de `IdempotencyKey` — sin cambios |
| No permitir overspending concurrente en origen ni lost updates en destino | `xmin` concurrency token — sin cambios de mecanismo, cobertura de prueba ampliada (research.md §2) |
| Número de cuenta único (nuevo) | Índice único `ux_accounts_number` (Infrastructure) |

## Third-Party Transfer Use Case

Ver research.md §5 para la tabla completa de qué se separa y qué se unifica, con su
justificación. Resumen del flujo de `PreviewThirdPartyTransferUseCase.ExecuteAsync` (los 16 pasos
de la sección 7 del input, adaptados):

1. Resolver `CustomerId` actual vía `ICurrentCustomerProvider` (reutilizado).
2. Cargar cuenta origen vía `IAccountRepository.GetByIdForCustomerAsync` (reutilizado) — ya
   verifica pertenencia al cliente actual en la misma consulta.
3. `CommonTransferValidation`: origen existe, ACTIVA, importe > 0, saldo suficiente (nuevo,
   extraído — research.md §6).
4. Resolver cuenta destino vía `IAccountRepository.GetByNumberAsync` (nuevo — research.md §3), sin
   restricción de propietario.
5. Si no existe → outcome `DestinationAccountNotFound` (404, nuevo — research.md §7).
6. Si `destination.CustomerId == customerId` → outcome `DestinationIsOwnAccount` (422, nuevo).
7. Si `destination.Status != Active` → outcome `AccountBlocked` (422, reutilizado).
8. Resolver `Customer` del destino vía `ICustomerRepository.GetByIdAsync` (nuevo) y calcular
   `DisplayNameMasked` (Domain, nuevo).
9. Construir `TransferPreviewPayload` (reutilizado sin cambios) y protegerlo vía
   `IPreviewTokenSigner.Protect` (reutilizado).
10. Devolver `TransferPreviewResult` (reutilizado, campo `DestinationCustomerDisplayNameMasked`
    poblado).

`ConfirmTransferUseCase.ExecuteAsync` (renombrado y generalizado desde `002`) sigue exactamente
los mismos 13 pasos que `002` ya documentaba, con un único paso adicional insertado entre la
carga de la cuenta destino y las validaciones:

1-2. (sin cambios) Desproteger referencia; resolver `CustomerId` actual.

3. Cargar cuenta origen vía `GetByIdForCustomerAsync` (sin cambios).

4. Cargar cuenta destino vía `GetByIdAsync` (nuevo — antes usaba `GetByIdForCustomerAsync`, que
   solo sabe buscar "una cuenta del cliente actual"; ahora la cuenta destino puede pertenecer a
   otro cliente).

5. **Nuevo**: `var isThirdParty = destination is not null && destination.CustomerId !=
   customerId;` — la clasificación dinámica de research.md §5.

6. Si `isThirdParty` → aplicar `ThirdPartyTransferValidation` (nuevo); si no →
   `OwnAccountTransferValidation` (existente, sin cambios de comportamiento).

7-13. (sin cambios) Idempotencia, `Debit`/`Credit`, `Transfer.Create`, `SaveChangesAsync`, manejo
   de `ConcurrencyConflictException`/`UniqueConstraintViolationException`.

14. **Nuevo**: si `isThirdParty`, resolver y agregar `DestinationCustomerDisplayNameMasked` al
    `TransferResultDto` (vía `ICustomerRepository`); si no, dejarlo en `null`.

Ninguno de los dos casos de uso usa `DbContext`, `DbSet` ni tipos de EF Core directamente, ni
contiene lógica HTTP (sección 7 del input).

## Destination Resolution

Ver data-model.md, "Cómo se localiza la cuenta destino" y research.md §3. Resumen: el número de
cuenta se resuelve a un `AccountId` interno **una sola vez**, en la vista previa; la confirmación
nunca vuelve a recibir ni a resolver un número de cuenta — solo revalida el `AccountId` ya
resuelto y firmado. `AccountId` y `AccountNumber` permanecen conceptos distintos en Domain (sin
fusionarse), exactamente como pide la sección 9 del input.

## Recipient Privacy Strategy

Ver research.md §6/§7 y data-model.md, "Cómo se mantiene la privacidad del destinatario". Tres
reglas, cada una implementada en un único lugar (sección 24 del input: "no duplicar algoritmos de
masking en controllers, use cases y persistence adapters"):

1. **Minimización por construcción**: ningún caso de uso de esta feature consulta `DebitCards` ni
   proyecta `Balance` de la cuenta destino en ninguna respuesta — no hay nada que redactar porque
   nunca se carga.
2. **Enmascaramiento en un único lugar**: `Customer.DisplayNameMasked` (Domain). Ni
   `TransfersController`, ni `PreviewThirdPartyTransferUseCase`, ni `ConfirmTransferUseCase`
   implementan su propia lógica de enmascaramiento — todos consumen esta única propiedad.
3. **Decisión de "cuándo mostrarlo" en un único lugar**: dentro de los casos de uso compartidos
   (`ConfirmTransferUseCase`/`GetTransferUseCase`), comparando `destinationAccount.CustomerId`
   contra el `CustomerId` del cliente actual/ordenante — nunca en el controller ni en el mapeo de
   contratos Api.

## API First Strategy

Secuencia: `spec.md` → contratos OpenAPI (uno nuevo, uno actualizado aditivamente — completados en
esta fase) → gate de validación → `/speckit-tasks` → implementación → verificación de conformidad
en `IntegrationTests`. Ver research.md §10 para la justificación completa de la organización
elegida.

### Gate de validación del contrato

- [x] El contrato de la vista previa de terceros está definido — `third-party-transfers-v1.yaml`.
- [x] La confirmación y consulta reutilizan `own-account-transfers-v1.yaml`, actualizado de forma
      aditiva (campo opcional nuevo, compatible con FR-018 "al menos" de `002`).
- [x] Todas las operaciones corresponden a requisitos de la spec — `POST
      /third-party-transfer-previews` (FR-010), confirmación y consulta compartidas (FR-016…FR-021).
- [x] No existen endpoints fuera del alcance — ninguno de transferencias interbancarias, CCI, etc.
- [x] Los schemas no exponen modelos de persistencia ni información no permitida del destinatario
      (RB10/FR-007/FR-012).
- [x] Los errores HTTP están definidos — 400/404/409/422/500, todos `application/problem+json`.
- [x] El contrato es consistente con los criterios de aceptación — trazabilidad en quickstart.md.
- [x] La implementación todavía no ha comenzado.

**Gate: APROBADO.**

## OpenAPI Contract

Ver [`contracts/openapi/third-party-transfers-v1.yaml`](./contracts/openapi/third-party-transfers-v1.yaml)
(vista previa, único endpoint nuevo) y
[`own-account-transfers-v1.yaml` actualizado](../002-transferencias-cuentas-propias/contracts/openapi/own-account-transfers-v1.yaml)
(confirmación y consulta, compartidas — research.md §10). El request de vista previa de terceros
usa `destinationAccountNumber` (string), nunca `customerId` como prueba de identidad ni datos del
destinatario que el servidor pueda resolver por su cuenta (sección 12 del input).

## Money Strategy

Reutiliza `Money`/`CurrencyCode` de `001`/`002` sin ningún cambio (research.md §0). `decimal`
exacto, PEN fija, sin redondeo implícito.

## Persistence Strategy

Dos cambios aditivos, ambos sobre `accounts`, ninguno sobre `transfers` (research.md §9): índice
único `ux_accounts_number`, y `UpdateData` del `display_name` de Cliente A/B en la migración nueva
de esta feature (nunca editando `InitialCreate`, ya aplicada). `TransferConfiguration` no cambia.

## Repository Strategy

`IAccountRepository` se amplía con `GetByNumberAsync`/`GetByIdAsync` (research.md §3,
data-model.md). `ICustomerRepository` nueva, mínima, un solo método (data-model.md). Ninguna
interfaz nueva por "tipo" de transferencia — `ITransferRepository` sigue siendo la única
abstracción para `Transfer`, reutilizada sin cambios (sección 19 del input: evitar
`IThirdPartyAccountRepository`). No se expone `IQueryable`/`DbContext`/`DbSet` desde Application.
No se introduce `GenericRepository<T>`.

## Unit of Work Strategy

Sin cambios respecto de `002` (research.md §0): `IUnitOfWork.SaveChangesAsync()` sigue siendo el
único punto de confirmación, ahora invocado también por transferencias a terceros a través del
mismo `ConfirmTransferUseCase` unificado. Ningún método nuevo en la interfaz.

## Transaction Strategy

Sin cambios (research.md §0): débito + crédito + inserción de `Transfer` viajan en una única
`SaveChangesAsync`, sin importar si la cuenta destino pertenece al cliente actual o a un tercero.

## Concurrency Strategy

Ver research.md §2. El mecanismo `xmin` ya existente protege tanto el overspending del ordenante
(escenario de `002`, sección 16 del input) como los créditos concurrentes al mismo destino
(escenario nuevo de `003`, mismo mecanismo, misma sección del input), porque el concurrency check
de EF Core opera por fila de `accounts` actualizada, sin distinguir el rol de esa cuenta en la
operación. No se requiere ningún ajuste de diseño; se requiere una prueba de integración nueva
para verificarlo explícitamente (Testing Strategy).

## Idempotency Strategy

Sin cambios (research.md §1): mismo header `Idempotency-Key` obligatorio en la confirmación,
mismo índice único, mismo comportamiento ante clave repetida con mismos/distintos datos. El
mecanismo es agnóstico al tipo de transferencia porque opera sobre `Transfer.IdempotencyKey`
(opaco), no sobre las cuentas involucradas.

## Validation Strategy

FluentValidation continúa validando exclusivamente forma/sintaxis (sección 22 del input):
`sourceAccountId` debe ser GUID válido; `destinationAccountNumber` debe contener solo dígitos con
al menos 4 caracteres (mismo invariante que el constructor de `AccountNumber`, sin duplicar ni
divergir — research.md §3); `amount` debe tener como máximo 2 decimales (no se valida aquí que sea
`> 0`, igual criterio que `002`). Las reglas de propiedad, elegibilidad, existencia del destino y
saldo permanecen en Application/Domain sin excepción.

## Error Handling Strategy

Ver research.md §7 para la tabla completa de outcomes → HTTP nuevos. Principio rector, heredado de
`002` pero con una distinción nueva: el origen conserva el criterio de privacidad estricta
(inexistente y ajeno son indistinguibles, 404 genérico); el destino, en esta feature, **no**
recibe ese mismo tratamiento — "no existe" es una respuesta específica y reveladora (404 distinto),
porque verificar la validez del destino es precisamente el propósito de la funcionalidad (HU3).
"Destino es una cuenta propia" y "cuenta BLOQUEADA" (origen o destino) son 422 con `detail`
específico, porque no exponen nada que el cliente no supiera ya. Ningún outcome expone stack
traces, excepciones de EF Core/Npgsql, SQL ni nombres de tabla (mismo `GlobalExceptionHandler`,
sin cambios).

## Testing Strategy

Framework: xUnit, reutilizando `PostgresContainerFixture`/`BankingApiFactory` de `001`/`002` sin
cambios. Sección 25 del input: reutilizar tests/helpers existentes, no copiar la suite completa.

- **`Domain.UnitTests`**: cobertura nueva únicamente para `Customer.DisplayNameMasked` (casos:
  nombre con dos o más tokens, nombre de un solo token). `Account.Debit`/`Credit`/`Money` no se
  vuelven a probar — ya están cubiertos por `002` y no cambian de comportamiento.
- **`Application.UnitTests`**:
  - Regresión: la suite existente de `ConfirmOwnAccountTransferUseCaseTests`/
    `GetOwnAccountTransferUseCaseTests` se retarget a `ConfirmTransferUseCase`/
    `GetTransferUseCase` renombrados, sin perder ningún caso ya cubierto (debe seguir en verde).
  - Nueva: `PreviewThirdPartyTransferUseCase` — vista previa exitosa, origen inexistente/ajena/
    bloqueada, destino inexistente, destino es cuenta propia, destino bloqueada, importe inválido,
    saldo insuficiente, privacidad (el resultado nunca incluye saldo del destino).
  - Nueva: `ConfirmTransferUseCase` — casos de terceros equivalentes a los ya cubiertos para
    cuentas propias (éxito, cada rechazo, replay idempotente, conflicto de idempotencia, conflicto
    de concurrencia), verificando además que `DestinationCustomerDisplayNameMasked` se puebla solo
    cuando corresponde.
  - Nueva: `CommonTransferValidation`/`ThirdPartyTransferValidation` — pruebas unitarias directas
    de la función extraída, para no depender únicamente de las pruebas de los casos de uso.
- **`IntegrationTests`** (PostgreSQL real vía Testcontainers):
  - Transferencia exitosa a tercero (debit/credit/persistencia correctos).
  - Atomicidad (mismo patrón de `002`: forzar fallo antes de `SaveChangesAsync`).
  - Idempotencia (secuencial y concurrente, mismo patrón de `002`).
  - Concurrencia sobre origen (overspending, mismo patrón de `002`, ahora también alcanzable desde
    el flujo de terceros).
  - **Nueva**: concurrencia sobre destino — dos ordenantes (potencialmente de clientes distintos)
    acreditando la misma cuenta destino simultáneamente; verificar que ambos créditos se aplican
    sin pérdida (research.md §2). Sin este escenario, la garantía de `xmin` para créditos
    concurrentes quedaría sin verificar, aunque el mecanismo ya la provea.
  - Privacy contract: la respuesta HTTP de vista previa/confirmación/consulta nunca contiene saldo
    ni identificadores internos del destinatario.
  - Conformidad de contrato: `TransfersController` (extendido) se ajusta a
    `third-party-transfers-v1.yaml` y al `own-account-transfers-v1.yaml` actualizado.
  - Regresión: toda la suite de `IntegrationTests` de `002` (renombrada donde corresponda) debe
    seguir en verde.

## Scalability Strategy

Sin cambios respecto de `002` (research.md §0): la Api permanece stateless, sin locks en memoria,
sin caché de sesión. La única limitación conocida (key ring de Data Protection no compartido entre
instancias) ya estaba documentada en `002` y no se agrava por esta feature — el mismo mecanismo de
firma se reutiliza tal cual (research.md §4).

## Observability

Igual que `002` (sin cambios): `TransferId`, correlation id, outcome, categoría de error,
timestamps. Nunca se registra el número completo de cuenta destino, el nombre completo (sin
enmascarar) del destinatario, ni su saldo (sección 27 del input, explícita sobre no registrar
información personal adicional).

## Technical Risks

| Riesgo | Impacto | Mitigación |
|---|---|---|
| El refactor de `ConfirmOwnAccountTransferUseCase`/`GetOwnAccountTransferUseCase` (renombrado + generalización) podría introducir una regresión silenciosa en el comportamiento ya probado de `002` | Medio | La suite de pruebas existente de `002` se retarget (no se borra) a los nombres nuevos como primer paso de implementación, y debe seguir en verde antes de añadir cualquier caso nuevo de terceros (Impact Analysis, Testing Strategy) |
| El escenario de créditos concurrentes al mismo destino nunca fue ejercitado en `002` (solo se probó overspending del origen) | Medio | Nueva prueba de integración dedicada (Testing Strategy); si `xmin` no se comportara como predice research.md §2, se descubriría aquí, antes de producción académica |
| El número de cuenta ingresado por el cliente podría no coincidir exactamente con el formato almacenado (espacios, ceros a la izquierda) | Bajo | `AccountNumber`/`GetByNumberAsync` operan sobre el valor exacto ya validado por FluentValidation (solo dígitos); no se implementa normalización adicional sin necesidad concreta (Principio I) — si se detecta un caso real durante pruebas manuales, se documentará como hallazgo, no se anticipa aquí |
| `Testcontainers.PostgreSql` requiere Docker disponible (mismo riesgo ya documentado en `001`/`002`) | Medio | Ya aceptado como limitación conocida del entorno de desarrollo actual |

## Constitution Check — Post-Diseño

Tras completar Phase 1 (data-model.md, contratos OpenAPI, quickstart.md), ningún diseño
introducido contradice los principios I–IX evaluados arriba. El único artefacto ya aprobado que se
modifica (`own-account-transfers-v1.yaml`) se modifica de forma aditiva y compatible con su propio
requisito ("al menos"), no de forma que reinterprete silenciosamente los requisitos de `002`
(Principio II). El nuevo modelo de datos no añade ninguna entidad, relación o campo no respaldado
por un requisito funcional o regla de negocio de `spec.md`. **Constitution Check: PASS.**

## Complexity Tracking

> Se documentan aquí únicamente las decisiones que introducen una pieza de diseño que podría
> parecer no trivial para el alcance de esta feature y que por tanto requieren justificación
> explícita (Principio I).

| Decisión | Por qué es necesaria ahora | Alternativa más simple descartada |
|---|---|---|
| Renombrar/generalizar `ConfirmOwnAccountTransferUseCase`/`GetOwnAccountTransferUseCase` (tocar código ya implementado de `002`) en vez de crear clases nuevas para terceros | El *request* de confirmación y de consulta es idéntico entre ambos tipos de transferencia (research.md §5); crear clases nuevas duplicaría íntegramente esa lógica, exactamente el antipatrón que la sección 4 del input prohíbe explícitamente (`OwnTransferRepository`/`ThirdPartyTransferRepository`) | Dejar `002` intacto y crear `ConfirmThirdPartyTransferUseCase`/`GetThirdPartyTransferUseCase` como clases separadas — descartada porque duplicaría carga de cuentas, manejo de idempotencia/concurrencia y construcción del DTO de resultado sin ninguna diferencia real de lógica, solo por evitar tocar código existente |
| Dos métodos nuevos sin restricción de propietario en `IAccountRepository` (`GetByNumberAsync`, `GetByIdAsync`) | Es la única forma de resolver/revalidar una cuenta que pertenece a **otro** cliente sin conocer de antemano su `CustomerId` (research.md §3); ambos se invocan exclusivamente con datos ya validados (número de cuenta con formato correcto, o `AccountId` ya resuelto y firmado por el propio backend) | Exponer `IQueryable<Account>` para que Application filtre libremente — descartada explícitamente por la sección 19 del input y por el patrón ya establecido de repositorios con métodos de intención explícita |
| Índice único nuevo en `accounts.number` | Esta feature introduce la primera consulta real por ese campo; sin el índice, la búsqueda del destino degradaría con el volumen de cuentas, y la unicidad de un número de cuenta es una invariante de negocio real que merece reforzarse a nivel de base de datos (mismo criterio que `idempotency_key` en `002`) | Confiar únicamente en que la aplicación nunca inserte números duplicados, sin índice único — descartada por el mismo razonamiento que ya se usó en `002` para `idempotency_key`: es una segunda línea de defensa barata contra un error real de programación o de datos |

Ninguna otra fila aplica: no hay CQRS, MediatR, Domain Events, colas de mensajería, locks
distribuidos, sagas, microservicio nuevo, DbContext nuevo, base de datos separada, ni ningún otro
elemento de la lista prohibida (sección 28 del input) en este plan.
