# Data Model: Transferencias entre cuentas propias

**Feature**: `002-transferencias-cuentas-propias` | **Date**: 2026-09-24

Deriva de `spec.md` (Key Entities, Functional Requirements) y de `research.md`. Describe el
modelo de **Domain** (C# puro). El mapeo a PostgreSQL se resume al final de cada entidad.

## Cambios sobre el modelo existente de `001`

### `Account` (modificada, no reemplazada)

Ubicación real: `src/BancaDigitalPeru.Domain/Accounts/Account.cs`.

- `Balance` pasa de `{ get; }` a `{ get; private set; }`.
- Se añaden dos métodos de comportamiento (research.md §2):
  - `Debit(Money amount)`: exige `amount.Currency == Balance.Currency`, `amount.Amount > 0`,
    `Status == AccountStatus.Active` (RF-005/FR-023), y `amount.Amount <= Balance.Amount`
    (RF-009/FR-019). Reasigna `Balance = Money.Create(Balance.Amount - amount.Amount, Balance.Currency)`.
  - `Credit(Money amount)`: exige `amount.Currency == Balance.Currency`, `amount.Amount > 0`, y
    `Status == AccountStatus.Active` (FR-006, clarificación 2026-09-24: una cuenta `Blocked` no
    puede ser destino). Reasigna `Balance = Money.Create(Balance.Amount + amount.Amount, Balance.Currency)`.
- Estas verificaciones son una segunda línea de defensa: Application ya las comprueba antes de
  invocar `Debit`/`Credit` (ver "Application Use Case" en plan.md), por lo que en operación normal
  nunca deberían dispararse.
- **Persistencia (cambio)**: se añade concurrency token nativo de PostgreSQL (`xmin`) vía
  `UseXminAsConcurrencyToken()` en `AccountConfiguration` (research.md §6). No se añade ninguna
  columna nueva.

### `IAccountRepository` (extendida, no reemplazada)

Ubicación real: `src/BancaDigitalPeru.Application/Abstractions/Persistence/IAccountRepository.cs`.

- Se añade `Task<Account?> GetByIdForCustomerAsync(CustomerId, AccountId, CancellationToken)` —
  ya existe desde `001`, se reutiliza tal cual para localizar tanto la cuenta origen como la
  destino (dos llamadas, o una variante que acepte ambos ids en una sola consulta — decisión de
  implementación, no de este documento).

## Nuevas entidades y Value Objects

### `TransferId` (Value Object)

Envoltorio de `Guid`, mismo patrón que `AccountId`/`DebitCardId` de `001`. Invariante: no vacío.

### `IdempotencyKey` (Value Object)

Envoltorio de `string`. Invariantes: no vacío, longitud entre 1 y 255 caracteres (research.md §5).
No se exige un formato específico (el cliente puede usar cualquier token opaco, típicamente un
UUID); el valor se persiste tal cual para la comparación exacta de duplicados.

### `TransferStatus` (enum)

Único miembro: `Completed` (research.md §3, data-model deliberadamente cerrado — spec: las
transferencias rechazadas no se persisten como `Transfer`).

### `Transfer` (Entity, Aggregate Root)

| Campo | Tipo | Notas |
|---|---|---|
| `Id` | `TransferId` | Clave primaria, generada por el backend al confirmar (FR-016) |
| `CustomerId` | `CustomerId` | Propietario — ambas cuentas pertenecen al mismo cliente por diseño (RF-001/RF-002); se denormaliza aquí para permitir un filtro de propiedad en una sola consulta al consultar el resultado (US4), mismo patrón que `DebitCard.CustomerId` en `001` |
| `SourceAccountId` | `AccountId` | Cuenta debitada |
| `DestinationAccountId` | `AccountId` | Cuenta acreditada |
| `Amount` | `Money` | Importe transferido, PEN, 2 decimales (RB5/FR-013/FR-014) |
| `Status` | `TransferStatus` | Siempre `Completed` en esta versión |
| `IdempotencyKey` | `IdempotencyKey` | Único a nivel de base de datos (research.md §5) |
| `CompletedAtUtc` | `DateTimeOffset` | Fecha/hora autoritativa generada en backend (FR-017), en UTC; se presenta al cliente convertida a America/Lima (spec, Assumptions) |

**Invariantes de construcción** (factory `Transfer.Create(...)`, sin constructor público): `Amount.Currency == PEN`; `SourceAccountId != DestinationAccountId` (RB2/FR-003, defensa en profundidad — Application ya lo valida antes).

**Persistencia**: tabla `transfers` (`id uuid PK`, `customer_id uuid FK -> customers.id`,
`source_account_id uuid FK -> accounts.id`, `destination_account_id uuid FK -> accounts.id`,
`amount_amount numeric(18,2)`, `amount_currency text`, `status text`, `idempotency_key text`,
`completed_at_utc timestamptz`). Índices: `(customer_id)` para el listado futuro si se agregara, y
un **índice único** en `(idempotency_key)` (research.md §5). No se persiste el número completo de
cuenta ni ningún otro dato ya capturado por `AccountId` (sección 13 del input: no duplicar
información resoluble mediante los identificadores de negocio).

## Value Object de proceso (no persistido)

### `TransferPreviewPayload` (no es una entidad de Domain; vive en Application/Infrastructure)

Representa el contenido codificado dentro de la referencia de vista previa (research.md §4):
`SourceAccountId`, `DestinationAccountId`, `Amount`, `IssuedAtUtc`. No tiene tabla ni identidad
propia — se serializa y se protege criptográficamente (Data Protection) al generar la vista
previa, y se desprotege y deserializa al confirmar. No es un Aggregate ni una Entity de Domain: es
un contrato de datos interno entre los dos pasos del flujo.

## Relaciones

```text
Customer (1) ──── (0..N) Account            [ya existente, 001]
Customer (1) ──── (0..N) Transfer            [nuevo; CustomerId denormalizado]
Account  (1) ──── (0..N) Transfer            [como origen, vía SourceAccountId]
Account  (1) ──── (0..N) Transfer            [como destino, vía DestinationAccountId]
```

## Reglas de negocio reflejadas en el modelo

| Regla de la spec | Dónde se refleja |
|---|---|
| RF-001/RF-002 (RB1) — ambas cuentas del cliente actual | Validado en Application antes de invocar Domain; `Transfer.CustomerId` registra el propietario común |
| RF-003 (RB2) — cuentas distintas | Invariante de construcción de `Transfer.Create`; validado también en Application antes de mutar `Account` |
| RF-004/RF-005/FR-006 (RB3) — moneda PEN, estados ACTIVA | `Account.Debit`/`Credit` (invariantes), `CurrencyCode` de `001` reutilizado sin cambios |
| RF-009/RF-019 (RB4/RB7) — importe válido y saldo suficiente | `Account.Debit` (invariante); revalidado en cada confirmación (FR-012) |
| RF-013/RF-014 (RB5) — conservación del dinero | `Debit`/`Credit` operan sobre el mismo `Money.Amount`, una sola `SaveChangesAsync` |
| RF-015 (RB6) — atomicidad | Una única `IUnitOfWork.SaveChangesAsync()` (research.md §7) |
| RF-016/RF-017/RB10 — identificador y fecha/hora | `TransferId`/`CompletedAtUtc` generados en backend al confirmar |
| FR-022 — indistinguibilidad origen/destino ajeno o inexistente | Manejo de errores en Application/Api (research.md §8), no en el modelo de datos |
| RF-021 (RB9) — no duplicar el movimiento | `IdempotencyKey` con índice único + verificación previa (research.md §5) |
| RF-026/RF-027 — persistencia observable | `Transfer` persistido en PostgreSQL real, sobrevive reinicios |
