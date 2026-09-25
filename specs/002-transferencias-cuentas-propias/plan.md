# Implementation Plan: Transferencias entre cuentas propias

**Branch**: `002-transferencias-cuentas-propias` | **Date**: 2026-09-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-transferencias-cuentas-propias/spec.md`, más un
input de planificación detallado que exige atención explícita a atomicidad, concurrencia,
idempotencia y trazabilidad, por ser la primera operación de escritura financiera del proyecto.

## Summary

Permitir que el cliente actual transfiera dinero entre dos cuentas de ahorro propias mediante un
flujo de dos pasos —vista previa (sin efecto financiero) y confirmación (ejecuta la
transferencia)— construido sobre el mismo stack y la misma base de código de `001`
(.NET 10 / ASP.NET Core 10, Clean Architecture, Contract First). La transferencia es atómica (una
única `SaveChangesAsync`), protegida contra sobregasto concurrente mediante optimistic concurrency
nativo de PostgreSQL (`xmin`), e idempotente mediante un header `Idempotency-Key` obligatorio con
restricción única en base de datos. Reutiliza `Account`, `Money`, `CustomerId`,
`ICurrentCustomerProvider` e `IUnitOfWork` ya existentes; `IUnitOfWork` pasa de estar definida-pero-
sin-uso a ser consumida por primera vez.

## Technical Context

**Language/Version**: C# 13 sobre .NET 10 (`net10.0`) — igual que `001`

**Primary Dependencies**: ASP.NET Core 10 · Entity Framework Core 10 ·
Npgsql.EntityFrameworkCore.PostgreSQL · FluentValidation · Scalar.AspNetCore ·
`Microsoft.AspNetCore.DataProtection.Abstractions` (nueva referencia en Infrastructure — ya forma
parte del framework de ASP.NET Core, sin paquete NuGet de terceros; research.md §4) ·
`Testcontainers.PostgreSql` (ya adoptado en `001`, reutilizado)

**Storage**: PostgreSQL 17/18 vía EF Core 10 / Npgsql (misma base de datos e instancia que `001`)

**Testing**: xUnit; `Testcontainers.PostgreSql` para `IntegrationTests` (incluye pruebas de
concurrencia real con dos `DbContext` simultáneos, research.md §10); dobles de prueba para
`Application.UnitTests`

**Target Platform**: Mismo proceso Api de `001` (no se crea un servicio nuevo)

**Performance Goals**: Sin objetivos cuantitativos propios más allá de la corrección financiera
exigida por la spec (Principio I: no diseñar para requisitos inexistentes)

**Constraints**: Operación de escritura financiera con atomicidad, idempotencia y protección de
concurrencia obligatorias (constitución Principio V); sin autenticación (spec, fuera de alcance);
sin comisiones ni multidivisa; API sin estado en memoria, apta para múltiples instancias salvo la
limitación documentada del key ring de Data Protection (research.md §9)

**Scale/Scope**: Alcance de demostración académica, mismo cliente y cuentas ficticias de `001`; no
se diseña para volumen de producción

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principios (constitution.md v1.0.0)

| Principio | Evaluación | Estado |
|---|---|---|
| I. Simplicidad ante todo | Vista previa sin persistencia (token firmado, no una tabla nueva); `xmin` nativo en vez de una columna `RowVersion` propia; sin reintento automático; sin Redis/locks distribuidos. Toda desviación real se registra en Complexity Tracking. | PASS |
| II. Especificaciones primero y alcance gobernado por ellas | El plan implementa únicamente FR-001…FR-027 de `spec.md` (`Status: Draft`; ver nota de gobernanza) | PASS (ver nota) |
| III. Mercado peruano | PEN fija (RB3), mensajes de error en español de Perú, fecha/hora presentada en zona horaria de Perú (spec, Assumptions) | PASS |
| IV. Clean Architecture y responsabilidades separadas | Dependency Rule idéntica a `001`; `Account.Debit/Credit` como comportamiento de Domain, no lógica anémica en Application (research.md §2) | PASS |
| V. Integridad financiera | `Money`/`decimal` sin pérdida de precisión; atomicidad vía una única Unit of Work (research.md §7); idempotencia real con restricción única persistente (research.md §5); toda transferencia completada es trazable (`Transfer`, RB10) | PASS |
| VI. Seguridad y mínimo privilegio | Filtrado de propiedad server-side (nunca se confía en `customerId` del cliente); 404 genérico e idéntico ante cuenta origen/destino inexistente o ajena (research.md §8, extendiendo FR-022 simétricamente); mensajes de rechazo de negocio sin datos sensibles de terceros | PASS |
| VII. Comportamiento verificable | Todos los endpoints trazan a criterios Dado/Cuando/Entonces de `spec.md` (tabla en quickstart.md) | PASS |
| VIII. Calidad automatizada | Suite de pruebas más estricta que en `001` por el riesgo financiero: Domain/Application/Integration, incluyendo atomicidad, idempotencia y concurrencia reales contra PostgreSQL (ver Testing Strategy) | PASS |
| IX. Decisiones técnicas justificada | Cada decisión técnica (idempotencia, concurrencia, transacción, vista previa) justificada en `research.md` con alternativas descartadas | PASS |

**Nota de gobernanza (Principio II)**: `spec.md` está en `Status: Draft`. Igual que en `001`, el
Principio II exige `Status: Approved` antes de implementar. La spec ya no tiene marcadores
`[NEEDS CLARIFICATION]` (resueltos el 2026-09-24), por lo que solo falta el cambio formal de
estado antes de `/speckit-tasks`/implementación real.

### Gate adicional del input de planificación (sección 27)

- [x] El alcance permanece limitado a `002-transferencias-cuentas-propias`.
- [x] No se implementan capacidades de `003` (destino ajeno se rechaza, no se procesa).
- [x] Se respeta Clean Architecture (ver "Dependency Direction").
- [x] Domain permanece independiente (sin ASP.NET Core/EF Core/FluentValidation/Data Protection).
- [x] Las reglas financieras viven en Domain (`Account.Debit/Credit`) y Application (orquestación, revalidación en confirmación).
- [x] Money utiliza `decimal` con precisión exacta (reutiliza `Money` de `001`, sin cambios).
- [x] El movimiento es atómico (una única `SaveChangesAsync`, research.md §7).
- [x] No existen estados financieros parciales (mismo punto anterior; ver también "Technical Risks" para el escenario de fallo deliberado).
- [x] La concurrencia impide overspending (`xmin` optimistic concurrency, research.md §6).
- [x] La idempotencia impide doble débito (`Idempotency-Key` + índice único, research.md §5).
- [x] La solución funciona con múltiples instancias de Api, salvo la limitación documentada del key ring de Data Protection (research.md §9, Technical Risks).
- [x] Unit of Work tiene responsabilidad real (primera consumidora real desde `001`) y no duplica DbContext.
- [x] El contrato OpenAPI se diseña antes de implementación (`own-account-transfers-v1.yaml`, completo en esta fase).
- [x] Se protege propiedad de las cuentas (server-side, 404 genérico simétrico).
- [x] Las pruebas financieras críticas están contempladas (ver Testing Strategy).
- [x] La solución mantiene simplicidad (research.md, decisiones "alternativa más simple" en cada sección).
- [x] Cualquier complejidad adicional está justificada (ver Complexity Tracking).

**Gate: APROBADO.**

## Project Structure

### Documentation (this feature)

```text
specs/002-transferencias-cuentas-propias/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── openapi/
│       └── own-account-transfers-v1.yaml
└── tasks.md              # Phase 2 (/speckit-tasks — no generado por este comando)
```

### Source Code (repository root — extiende la solución existente, no crea proyectos nuevos)

```text
src/
├── BancaDigitalPeru.Domain/
│   ├── Accounts/
│   │   └── Account.cs                  # MODIFICADO: + Debit(), + Credit(), Balance con private set
│   └── Transfers/                      # NUEVO
│       ├── Transfer.cs
│       ├── TransferId.cs
│       ├── TransferStatus.cs
│       └── IdempotencyKey.cs
│
├── BancaDigitalPeru.Application/
│   ├── Abstractions/
│   │   ├── IPreviewTokenSigner.cs                          # NUEVO
│   │   └── Persistence/
│   │       └── ITransferRepository.cs                      # NUEVO
│   └── Transfers/                                          # NUEVO
│       ├── PreviewOwnAccountTransfer/
│       │   ├── TransferPreviewPayload.cs
│       │   ├── PreviewOwnAccountTransferUseCase.cs
│       │   └── PreviewOwnAccountTransferResult.cs
│       ├── ConfirmOwnAccountTransfer/
│       │   ├── ConfirmOwnAccountTransferUseCase.cs
│       │   └── ConfirmOwnAccountTransferResult.cs
│       └── GetOwnAccountTransfer/
│           ├── GetOwnAccountTransferUseCase.cs
│           └── TransferResultDto.cs
│
├── BancaDigitalPeru.Infrastructure/
│   ├── Persistence/
│   │   ├── BancaDigitalPeruDbContext.cs                    # MODIFICADO: + DbSet<Transfer>
│   │   ├── Configurations/
│   │   │   ├── AccountConfiguration.cs                     # MODIFICADO: + UseXminAsConcurrencyToken()
│   │   │   └── TransferConfiguration.cs                    # NUEVO
│   │   └── Repositories/
│   │       └── TransferRepository.cs                       # NUEVO
│   └── Security/
│       └── DataProtectionPreviewTokenSigner.cs             # NUEVO
│
└── BancaDigitalPeru.Api/
    ├── Controllers/
    │   └── TransfersController.cs                          # NUEVO
    ├── Contracts/
    │   └── Transfers/                                      # NUEVO (records reflejando el contrato)
    ├── Validation/
    │   └── TransferRequestValidators.cs                    # NUEVO
    ├── ErrorHandling/
    │   └── TransferOutcomeMapping.cs                        # NUEVO (mapea outcomes -> ProblemDetails)
    ├── OpenApi/
    │   └── OpenApiEndpoints.cs                             # MODIFICADO: + fuente own-account-transfers-v1
    └── Program.cs                                          # MODIFICADO: + AddDataProtection(), registros nuevos

src/BancaDigitalPeru.Infrastructure/Migrations/
    └── <timestamp>_AddTransfersAndAccountConcurrencyToken.cs   # NUEVA migración

tests/
├── BancaDigitalPeru.Domain.UnitTests/Accounts/AccountDebitCreditTests.cs   # NUEVO
├── BancaDigitalPeru.Application.UnitTests/Transfers/                       # NUEVO
└── BancaDigitalPeru.IntegrationTests/
    ├── Persistence/TransferRepositoryTests.cs                             # NUEVO
    └── Api/TransfersEndpointTests.cs                                      # NUEVO (incluye atomicidad, idempotencia, concurrencia)
```

**Structure Decision**: Se extiende la solución `BancaDigitalPeru.slnx` existente; no se crean
proyectos nuevos. Todos los archivos nuevos siguen el mismo layout por feature-folder ya usado en
`001` (`Accounts/`, `DebitCards/` → ahora también `Transfers/`).

## Dependency Direction

Idéntica a `001` (sin cambios):

```text
BancaDigitalPeru.Infrastructure ──> BancaDigitalPeru.Application ──> BancaDigitalPeru.Domain
BancaDigitalPeru.Api ──────────────> BancaDigitalPeru.Application
BancaDigitalPeru.Api ──────────────> BancaDigitalPeru.Infrastructure   (solo Composition Root)
```

`Infrastructure` añade una referencia a `Microsoft.AspNetCore.DataProtection.Abstractions`
(paquete liviano, sin dependencia de hosting completo de ASP.NET Core); `Api` registra la
implementación concreta del key ring (`AddDataProtection()`) en `Program.cs`, como corresponde a
un detalle de hosting (Composition Root).

## Domain Design

Ver [data-model.md](./data-model.md) para el detalle completo. Resumen de comportamiento (no solo
estructura, evitando el modelo anémico prohibido por la sección 4 del input):

- **`Account.Debit(Money amount)`**: invariantes — misma moneda, importe > 0, `Status == Active`
  (RF-005/FR-023), `amount <= Balance` (RF-009/FR-019/RB7). Reasigna `Balance` internamente.
- **`Account.Credit(Money amount)`**: invariantes — misma moneda, importe > 0, `Status == Active`
  (FR-006, clarificación: destino BLOQUEADA se rechaza igual que origen). Reasigna `Balance`.
- **`Transfer.Create(...)`**: invariantes — moneda PEN, `SourceAccountId != DestinationAccountId`
  (defensa en profundidad; Application ya lo valida antes).

Estas verificaciones de Domain son una **segunda línea de defensa**: Application valida todo antes
de invocar `Debit`/`Credit` (ver "Application Use Case"), por lo que en operación normal las
excepciones de Domain nunca deberían dispararse — si lo hicieran, sería un error de programación,
no una respuesta de negocio legítima, y quedarían cubiertas por el manejo genérico de errores
(500), no por un outcome específico.

## Aggregate Boundaries

`Account` y `Transfer` son Aggregate Roots independientes (research.md §1). Ninguna transferencia
requiere que ambas cuentas compartan una raíz común: la única regla multi-cuenta ("distintas y del
mismo cliente") es responsabilidad del **caso de uso** de Application, no de un agregado
compuesto. Tabla de responsabilidades:

| Invariante | Protegida por |
|---|---|
| Cuenta origen ACTIVA y con saldo suficiente al debitar | `Account.Debit` (Domain) |
| Cuenta destino ACTIVA al acreditar | `Account.Credit` (Domain) |
| Cuentas distintas, ambas del cliente actual | Caso de uso (Application), antes de tocar Domain |
| Moneda PEN, importe > 0 y con 2 decimales | `Money.Create` (Domain, reutilizado de `001`) + `Account.Debit/Credit` |
| Débito + crédito + registro de `Transfer` atómicos | `IUnitOfWork.SaveChangesAsync()` (una sola llamada) |
| No duplicar el movimiento ante solicitud repetida | Índice único de `IdempotencyKey` + verificación previa (Application/Infrastructure) |
| No permitir overspending concurrente | `xmin` concurrency token (Infrastructure/EF Core) |

## Application Use Case

Tres casos de uso (feature folders), sin mediador genérico (igual que `001`):

| Caso de uso | Entrada | Salida | Efecto |
|---|---|---|---|
| `PreviewOwnAccountTransferUseCase` | `SourceAccountId`, `DestinationAccountId`, `Money` | Referencia firmada + datos identificados, o rechazo | Ninguno (solo lectura + validación) |
| `ConfirmOwnAccountTransferUseCase` | Referencia de vista previa (decodificada a `TransferPreviewPayload`), `IdempotencyKey` | `TransferResultDto` (nuevo o replay), o rechazo | Débito + crédito + `Transfer` persistidos atómicamente, o ninguno |
| `GetOwnAccountTransferUseCase` | `TransferId` | `TransferResultDto` o `NotFound` | Ninguno |

`ConfirmOwnAccountTransferUseCase.ExecuteAsync` sigue exactamente los 13 pasos de la sección 6 del
input de planificación:

1. Resolver `CustomerId` actual vía `ICurrentCustomerProvider` (reutilizado de `001`).
2. Cargar cuenta origen vía `IAccountRepository.GetByIdForCustomerAsync` (reutilizado de `001`).
3. Cargar cuenta destino vía el mismo método (con el `CustomerId` actual — así, si la cuenta
   destino no pertenece al cliente actual, la búsqueda ya devuelve `null` sin distinguirlo de "no
   existe", satisfaciendo FR-022 en el punto de origen del dato, no como un parche posterior).
4. Si origen o destino es `null` → outcome `AccountNotEligible` (404 genérico, research.md §8).
5. Si `SourceAccountId == DestinationAccountId` → outcome `SameAccount` (422).
6. Si origen o destino no está `Active` → outcome `AccountNotEligible422` (422; ver Error Handling).
7. Si `Amount <= 0` → outcome `InvalidAmount` (422; el formato ya lo filtró FluentValidation antes).
8. Si `Amount > origen.Balance` → outcome `InsufficientFunds` (422).
9. Buscar `Transfer` existente por `IdempotencyKey` vía `ITransferRepository`; si existe y coincide
   → devolver su resultado (200, replay); si existe y no coincide → outcome `IdempotencyConflict`
   (409); si no existe, continuar.
10. `sourceAccount.Debit(amount)` (Domain).
11. `destinationAccount.Credit(amount)` (Domain).
12. `Transfer.Create(...)` y añadir vía `ITransferRepository.Add(transfer)`.
13. `IUnitOfWork.SaveChangesAsync()`; capturar `DbUpdateConcurrencyException` → outcome
    `ConcurrencyConflict` (409); capturar violación de unicidad de `IdempotencyKey` → recargar y
    devolver el `Transfer` ganador (200, replay); éxito → outcome `Created` (201) con
    `TransferResultDto`.

El caso de uso no usa `DbContext`, `DbSet` ni tipos de EF Core directamente (solo las
abstracciones `IAccountRepository`, `ITransferRepository`, `IUnitOfWork`), ni contiene lógica HTTP.

## API First Strategy

Secuencia: `spec.md` → contrato OpenAPI (`own-account-transfers-v1.yaml`, completado en esta fase)
→ gate de validación → `/speckit-tasks` → implementación de `TransfersController` → verificación
de conformidad en `IntegrationTests`. Los controllers no preceden al contrato.

### Decisión: contrato separado para transfers (no se amplía banking-products-v1.yaml)

**Elegido**: un segundo documento OpenAPI, `own-account-transfers-v1.yaml`, servido en su propia
ruta estática (`/openapi/transfers-v1.yaml`) y añadido como segunda fuente en la misma instancia
de Scalar ya configurada en `001` (Scalar.AspNetCore soporta múltiples fuentes de documento; el
método fluido exacto se confirma al codificar en `/speckit-tasks`).

**Justificación**: "transfers" es una capacidad de negocio distinta de "banking-products" (lectura
vs. escritura financiera, con su propio perfil de riesgo y su propio ciclo de versionado — un
cambio incompatible en transfers no debería forzar una nueva versión del contrato de consulta de
productos). Cumple la sección 7 del input ("preferir una API organizada por capacidades de negocio
y no por proyectos internos de Clean Architecture") interpretando "capacidad de negocio" como el
criterio de partición correcto entre los dos contratos, no dentro de uno solo.

### Gate de validación del contrato

- [x] El contrato OpenAPI está definido — `own-account-transfers-v1.yaml`.
- [x] Todas las operaciones corresponden a requisitos de la spec — `POST /transfer-previews`
      (FR-010), `POST /transfers` (FR-011/FR-012), `GET /transfers/{id}` (FR-018, User Story 4).
- [x] No existen endpoints fuera del alcance — 3 operaciones, ninguna de `003-third-party-transfers`.
- [x] Los schemas no exponen modelos de persistencia — sin campos de EF Core, `previewReference`
      es opaco por diseño.
- [x] Los errores HTTP están definidos — 400/404/409/422/500, todos `application/problem+json`.
- [x] El contrato es consistente con los criterios de aceptación — trazabilidad en quickstart.md.
- [x] La implementación todavía no ha comenzado.

**Gate: APROBADO.**

## OpenAPI Contract

Ver [`contracts/openapi/own-account-transfers-v1.yaml`](./contracts/openapi/own-account-transfers-v1.yaml).
Resumen: `POST /transfer-previews` (vista previa, sin efecto), `POST /transfers` (confirmación,
requiere header `Idempotency-Key`, responde 201 nuevo o 200 replay), `GET /transfers/{transferId}`
(consulta). `sourceAccountId`/`destinationAccountId` son los mismos GUID internos ya usados por el
contrato de `001`; el request de confirmación **no** acepta `customerId` (sección 8 del input).

## Money Strategy

Reutiliza `Money`/`CurrencyCode` de `001` sin cambios: `decimal`, nunca `float`/`double`,
invariante de 2 decimales ya impuesta en `Money.Create`. `AccountConfiguration` ya mapea `Money`
como owned type (`numeric(18,2)`); `TransferConfiguration` reutiliza el mismo patrón para
`Transfer.Amount`. El redondeo respeta exactamente RB4/RB5 de la spec (sin redondeo implícito:
`Money.Create` rechaza importes con más de 2 decimales en vez de redondearlos silenciosamente —
mismo comportamiento que `001`, ahora relevante también para CL2 de esta spec).

## Persistence Strategy

Un único modelo compartido Domain = Persistencia, igual criterio que `001` (research.md de `001`
§5): `Transfer` se mapea directamente vía Fluent API (`TransferConfiguration`), sin DTO de
persistencia separado. Nueva migración `AddTransfersAndAccountConcurrencyToken` añade la tabla
`transfers` y aplica `UseXminAsConcurrencyToken()` a `Account` (sin nueva columna, ya que `xmin` es
una columna de sistema de PostgreSQL). Constraint defensiva adicional en base de datos: índice
único en `transfers.idempotency_key` (segunda línea de protección junto a la verificación
explícita en Application — research.md §5).

## Repository Strategy

`ITransferRepository` (nueva, mismo patrón que `IAccountRepository`/`IDebitCardRepository` de
`001`):

```text
ITransferRepository
    Add(Transfer transfer)                                                    -> void (tracking, sin SaveChanges)
    GetByIdForCustomerAsync(CustomerId, TransferId, CancellationToken)         -> Transfer?
    GetByIdempotencyKeyAsync(IdempotencyKey, CancellationToken)                -> Transfer?
```

`IAccountRepository` no cambia de forma (ya expone `GetByIdForCustomerAsync`, suficiente para
cargar origen y destino). No se expone `DbSet`/`IQueryable`/`DbContext` desde Application. No se
introduce `GenericRepository<T>`.

## Unit of Work Strategy

`IUnitOfWork` (definida en `001`, `SaveChangesAsync(CancellationToken)`) se consume por primera
vez: `ConfirmOwnAccountTransferUseCase` es su primer y único consumidor. No se añaden métodos
nuevos a la interfaz (no se expone `BeginTransaction`/`Commit`/`Rollback`/`DbTransaction` ni a
Domain ni a Application, sección 10 del input) porque una sola llamada a `SaveChangesAsync` ya
basta para la atomicidad requerida (research.md §7).

## Transaction Strategy

Ver research.md §7. Débito + crédito + inserción de `Transfer` viajan en una única llamada a
`SaveChangesAsync`, que EF Core envuelve automáticamente en una única transacción de base de
datos. No se distribuye la operación entre varias Unit of Work. El escenario de fallo deliberado
exigido por la sección 11 del input se cubre en `IntegrationTests` (Testing Strategy):
forzar una excepción justo antes de que se complete `SaveChangesAsync` y verificar que ambos
saldos permanecen en su valor anterior y que ningún `Transfer` fue insertado.

## Concurrency Strategy

Ver research.md §6. `Account` usa `xmin` como concurrency token optimista (nativo de PostgreSQL,
sin columna nueva). Un conflicto de concurrencia produce `DbUpdateConcurrencyException`, mapeado a
`409 Conflict` sin reintento automático (research.md §6 justifica por qué no reintentar). El
escenario de la sección 12 del input (dos solicitudes de S/ 80.00 contra S/ 100.00) se cubre con
una prueba de integración que ejecuta dos confirmaciones concurrentes con dos `DbContext`
independientes contra el mismo Postgres.

## Idempotency Strategy

Ver research.md §5. Header `Idempotency-Key` obligatorio solo en `POST /transfers` (la vista
previa es de solo lectura y no lo necesita). Verificación previa por `ITransferRepository.
GetByIdempotencyKeyAsync` + índice único persistente en PostgreSQL como respaldo ante condiciones
de carrera entre solicitudes concurrentes con la misma clave. Comportamiento ante clave repetida
con los mismos datos: 200 con el resultado ya existente. Ante clave repetida con datos distintos:
409 `idempotency-conflict`. La garantía sobrevive reinicios de la aplicación porque vive en
PostgreSQL, no en memoria.

## Validation Strategy

FluentValidation valida exclusivamente forma/sintaxis, nunca reglas de negocio (sección 18 del
input): `sourceAccountId`/`destinationAccountId` deben ser GUID válidos; `amount` debe ser un
número con como máximo 2 decimales (no se valida aquí que sea `> 0`: esa es una invariante de
negocio que Domain/Application deben proteger igualmente, sección 18); `Idempotency-Key` debe
tener entre 1 y 255 caracteres; `previewReference` debe estar presente (su decodificación válida
se comprueba en Application/Infrastructure, no en el validator). Un fallo de validación produce
`400`. Las reglas de propiedad, elegibilidad, saldo e invariantes de negocio permanecen en
Application/Domain sin excepción.

## Error Handling Strategy

Ver research.md §8 para la tabla completa de outcomes → HTTP. Principio rector: los rechazos que
podrían revelar información sobre cuentas ajenas (origen o destino inexistente/ajena) usan un
único `ProblemDetails` genérico e indistinguible (404); los rechazos que son reglas de negocio
ordinarias sobre las propias cuentas del cliente (misma cuenta, cuenta bloqueada, importe
inválido, saldo insuficiente) usan `422` con un `detail` específico, porque no exponen nada que el
cliente no supiera ya sobre sus propios productos. Conflictos técnicos (idempotencia,
concurrencia) usan `409`. Ningún outcome expone stack traces, excepciones de EF Core/Npgsql, SQL
ni nombres de tabla (mismo `GlobalExceptionHandler` de `001`, sin cambios, para el camino 500).

## Testing Strategy

Framework: xUnit, reutilizando la infraestructura de `001` (`PostgresContainerFixture`,
`BankingApiFactory`).

- **`Domain.UnitTests`**: `Account.Debit`/`Credit` — débito válido, crédito válido, saldo
  insuficiente, importe inválido (≤0 o distinta moneda), cuenta no `Active` para débito y para
  crédito, preservación de invariantes tras la operación (`Balance` refleja exactamente el
  importe). Sin EF Core ni PostgreSQL.
- **`Application.UnitTests`**: `ConfirmOwnAccountTransferUseCase` — transferencia exitosa, cuenta
  origen inexistente, cuenta destino inexistente, cuenta origen ajena, cuenta destino ajena, misma
  cuenta como origen y destino, saldo insuficiente, cuenta origen/destino no elegible (bloqueada),
  importe inválido, replay idempotente (mismos datos), conflicto de idempotencia (datos
  distintos), `IUnitOfWork` no confirmada ante error simulado del repositorio. `PreviewOwnAccount
  TransferUseCase` — vista previa válida, mismos rechazos que la confirmación (validación
  compartida). Dobles de prueba para `IAccountRepository`, `ITransferRepository`, `IUnitOfWork`,
  `IPreviewTokenSigner`.
- **`IntegrationTests`** (PostgreSQL real vía Testcontainers, ya adoptado en `001`):
  - **Atomicidad**: forzar un error antes de que complete `SaveChangesAsync` (p. ej. un
    `ITransferRepository` de prueba que lance tras `Add`) y verificar que ambos saldos y la
    ausencia del `Transfer` quedan como antes del intento.
  - **Idempotencia**: enviar la misma confirmación dos veces (mismo `Idempotency-Key`, mismos
    datos) y verificar un único débito/crédito.
  - **Idempotencia concurrente**: disparar dos confirmaciones simultáneas con la misma
    `Idempotency-Key` (`Task.WhenAll`) y verificar que solo una aplica el movimiento.
  - **Concurrencia de saldo**: dos confirmaciones simultáneas, cuentas distintas de la misma
    cuenta origen, importes individualmente válidos pero conjuntamente superiores al saldo;
    verificar que nunca se produce overspending y que exactamente una tiene éxito (o ninguna, si
    la revalidación tras el conflicto la rechaza).
  - **Persistencia**: mappings, `xmin`, índice único de `IdempotencyKey`, migraciones.
  - **API**: conformidad de `TransfersController` con `own-account-transfers-v1.yaml`.

## Scalability Strategy

La Api permanece stateless: ningún lock en memoria, ningún diccionario estático, ningún caché de
sesión para idempotencia o vistas previas (secciones 24 del input). `async`/`await` y
`CancellationToken` en toda la cadena. La única limitación conocida para múltiples instancias es el
key ring de Data Protection (research.md §9, Technical Risks) — no bloqueante para el alcance
académico de esta feature.

## Observability

Los logs del caso de uso de confirmación pueden incluir `TransferId`, un identificador de
correlación, el outcome (éxito/tipo de rechazo) y la categoría de error. Nunca se registran
números completos de cuenta, saldos, secretos, ni el cuerpo completo de la solicitud. La
`Idempotency-Key` se registra tal cual (es un token opaco generado por el cliente, no un dato
financiero sensible); si una política de seguridad futura lo exigiera, se podría hashear antes de
loguearla, pero no hay ese requisito en la spec actual (documentado aquí para no perder la
consideración, sin implementarlo sin necesidad concreta — Principio I).

## Technical Risks

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Key ring de Data Protection no compartido entre instancias de Api | Medio si se despliega con múltiples réplicas: una vista previa firmada por una instancia podría no ser decodificable por otra | Documentado como limitación conocida (research.md §9); para el alcance académico de una sola instancia no aplica; si se necesitara, `PersistKeysToDbContext`/volumen compartido resolvería sin rediseño |
| Cambiar `Account.Balance` de solo-lectura a `private set` podría tentar a introducir mutaciones fuera de `Debit`/`Credit` en el futuro | Bajo | Cubierto por `Domain.UnitTests` que verifican que el saldo solo cambia a través de esos dos métodos; revisión de código humana sigue siendo la salvaguarda principal |
| Mapear `xmin` con Npgsql/EF Core 10 puede requerir la sintaxis exacta de configuración (nombre del método de extensión puede variar entre versiones del proveedor) | Medio — podría retrasar la tarea de `AccountConfiguration` | Documentar el patrón exacto verificado en la primera tarea de implementación; cubrir con una prueba de integración temprana que provoque un conflicto real y confirme la excepción esperada |
| Confirmar con una `previewReference` cuya firma es válida pero cuyos datos ya no reflejan la realidad (p. ej. la cuenta fue eliminada, fuera de alcance de esta spec) | Bajo | La revalidación completa en el paso de confirmación (FR-012) ya cubre cualquier cambio de estado real de las cuentas |
| `Testcontainers.PostgreSql` requiere Docker disponible en CI/entorno de desarrollo (mismo riesgo que en `001`) | Medio | Ya documentado en `001`; `Domain.UnitTests`/`Application.UnitTests` no dependen de Docker |

## Constitution Check — Post-Diseño

Tras completar Phase 1 (data-model.md, contrato OpenAPI, quickstart.md), ningún diseño introducido
contradice los principios I–IX evaluados arriba. El modelo de datos no añade ninguna entidad,
relación o campo no respaldado por un requisito funcional o regla de negocio de `spec.md`. El
contrato OpenAPI no expone modelos de persistencia ni añade endpoints fuera de la spec.
**Constitution Check: PASS.**

## Complexity Tracking

> Se documentan aquí únicamente las decisiones que introducen una pieza de diseño que podría
> parecer no trivial para el alcance de esta feature y que por tanto requieren justificación
> explícita (Principio I).

| Decisión | Por qué es necesaria ahora | Alternativa más simple descartada |
|---|---|---|
| Referencia de vista previa firmada con `Microsoft.AspNetCore.DataProtection` en vez de simplemente reenviar los mismos 3 campos a la confirmación | La clarificación del 2026-09-24 fijó explícitamente que la spec requiere un flujo de dos pasos con una "referencia de vista previa" como parte del contrato observable (FR-010); un token firmado sin persistencia es la forma más simple de cumplir ese requisito ya fijado, sin añadir una tabla nueva | Omitir el concepto de "referencia" y que el cliente reenvíe los mismos 3 campos crudos a la confirmación — descartada porque contradice literalmente lo que la spec ya clarificó y aprobó (FR-010), no porque sea técnicamente inferior |
| `xmin` como concurrency token (en vez de omitir el control de concurrencia) | La sección 12 del input exige explícitamente proteger el escenario de doble débito concurrente sobre el mismo saldo; sin un mecanismo de concurrencia, dos confirmaciones simultáneas podrían dejar el saldo en negativo, violando RB7 | Confiar únicamente en la revalidación de saldo dentro de la misma transacción sin ningún token de concurrencia — descartada porque dos transacciones que leen el mismo saldo "viejo" antes de que ninguna confirme pueden ambas pasar esa revalidación y ambas seguir adelante (condición de carrera clásica que `xmin` cierra al nivel de la fila) |
| `ITransferRepository` como abstracción nueva (en vez de reutilizar `IAccountRepository` para todo) | `Transfer` es un agregado con su propio ciclo de vida y su propia consulta por `IdempotencyKey`, semánticamente distinta de cualquier operación sobre `Account`; mezclar ambas responsabilidades en una sola interfaz violaría la cohesión que ya se mantuvo en `001` (una interfaz por agregado) | Añadir métodos de `Transfer` dentro de `IAccountRepository` — descartada por mezclar responsabilidades de dos agregados distintos en una sola abstracción, contradiciendo el patrón ya establecido |

Ninguna otra fila aplica: no hay CQRS, MediatR, Domain Events, colas de mensajería, locks
distribuidos, sagas ni ningún otro elemento de la lista prohibida (sección 26 del input) en este
plan.
