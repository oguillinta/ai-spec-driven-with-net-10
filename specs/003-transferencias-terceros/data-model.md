# Data Model: Transferencias a cuentas de terceros

**Feature**: `003-transferencias-terceros` | **Date**: 2026-09-25

Muestra únicamente el **impacto incremental** sobre el modelo ya existente de `001`/`002`
(research.md §0-§9). No se repite el modelo completo del sistema.

## Modelos reutilizados sin ningún cambio

`Account` (comportamiento `Debit`/`Credit`, invariantes), `AccountId`, `Money`/`CurrencyCode`,
`Transfer`/`Transfer.Create` (invariantes: moneda PEN, cuentas distintas), `TransferId`,
`IdempotencyKey`, `TransferStatus`, `TransferOutcome<T>`, `Customer` (entidad ya existe con
`DisplayName`, ver más abajo), `CustomerId`.

## Modelos que cambian

### `Customer` (Domain) — se le añade comportamiento, no se reemplaza

Ubicación real: `src/BancaDigitalPeru.Domain/Customers/Customer.cs`.

- Se añade una propiedad computada `DisplayNameMasked` (mismo patrón que
  `AccountNumber.Masked`/`CardNumber.Masked` de `001`): aplica el enmascaramiento acordado en
  clarify (2026-09-25, FR-011 de `003`) — primer token completo del `DisplayName` + inicial del
  segundo token + asteriscos (p. ej. `"Juan Pérez García"` → `"Juan P***"`). Si `DisplayName`
  tiene un solo token, se devuelve ese token seguido de asteriscos sin inicial adicional (caso
  defensivo, no se espera en los datos ficticios de referencia tras el ajuste de seed de
  research.md §9).
- Ningún cambio de persistencia: `DisplayNameMasked` es una propiedad calculada en memoria a
  partir de `DisplayName` (ya mapeado por `CustomerConfiguration`), no una columna nueva.

**Persistencia (cambio de datos, no de esquema)**: `UpdateData` en la migración nueva de `003`
sobre `customers.display_name` para `Cliente A`/`Cliente B` (research.md §9); ver también
`Account` más abajo para el cambio de esquema real.

### `Account` (Infrastructure) — nuevo índice, sin cambio de Domain

Ubicación real: `src/BancaDigitalPeru.Infrastructure/Persistence/Configurations/AccountConfiguration.cs`.

- Se añade `builder.HasIndex(a => a.Number).IsUnique().HasDatabaseName("ux_accounts_number")`
  (research.md §3). `Account`/`AccountNumber` (Domain) no cambian: el índice es una decisión de
  Infrastructure sobre una propiedad ya mapeada.

### `IAccountRepository` (Application) — se amplía, no se reemplaza

Ubicación real: `src/BancaDigitalPeru.Application/Abstractions/Persistence/IAccountRepository.cs`.

Se añaden dos métodos nuevos (research.md §3), sin modificar los dos ya existentes
(`GetByCustomerAsync`, `GetByIdForCustomerAsync`):

```text
Task<Account?> GetByNumberAsync(AccountNumber accountNumber, CancellationToken)
    -> resuelve la cuenta destino a partir del número ingresado por el ordenante (FR-009),
       sin restricción de propietario. Usado únicamente por PreviewThirdPartyTransferUseCase.

Task<Account?> GetByIdAsync(AccountId accountId, CancellationToken)
    -> resuelve una cuenta por su identificador interno, sin restricción de propietario. Usado
       únicamente por ConfirmTransferUseCase/GetTransferUseCase para revalidar/consultar la
       cuenta destino de una transferencia (AccountId ya resuelto y firmado en la vista previa,
       o ya persistido en Transfer.DestinationAccountId — nunca un AccountId provisto
       directamente por el cliente HTTP; ver research.md §3, nota de seguridad).
```

### `ITransferRepository` — sin cambios de forma; se aclara la semántica de `CustomerId`

`GetByIdForCustomerAsync(CustomerId, TransferId, ...)` sigue filtrando por "cliente ordenante"
(research.md §8): para una transferencia a terceros, `CustomerId` identifica al mismo cliente que
inició la operación (propietario de `SourceAccountId`), exactamente el mismo significado que ya
tenía en `002`. Ningún método cambia de firma.

### `ICustomerRepository` (Application) — interfaz nueva, mínima

Ubicación: `src/BancaDigitalPeru.Application/Abstractions/Persistence/ICustomerRepository.cs`.

```text
ICustomerRepository
    GetByIdAsync(CustomerId, CancellationToken) -> Customer?
```

Necesaria porque ninguna abstracción existente expone `Customer` (research.md §3): `001`/`002`
solo lo referenciaban como FK de `Account`/`DebitCard`, nunca lo consultaban directamente. Un
único método, coherente con el patrón de un repositorio por Aggregate Root ya establecido
(`IAccountRepository`, `ITransferRepository`, `IDebitCardRepository`).

### `TransferPreviewPayload`, `TransferPreviewResult`, `TransferResultDto` — se reubican y/o extienden

Ubicación nueva (compartida, fuera de la carpeta `PreviewOwnAccountTransfer/` donde vivían en
`002` — research.md §4-§5): `src/BancaDigitalPeru.Application/Transfers/`.

- **`TransferPreviewPayload`**: se reubica, **sin cambio de campos**
  (`SourceAccountId`, `DestinationAccountId`, `Amount`, `Currency`, `IssuedAtUtc`).
- **`TransferPreviewResult`**: se reubica y se amplía con un campo opcional nuevo,
  `DestinationCustomerDisplayNameMasked` (`string?`), poblado únicamente por
  `PreviewThirdPartyTransferUseCase`; `PreviewOwnAccountTransferUseCase` sigue dejándolo en
  `null` sin cambiar su propio comportamiento observable.
- **`TransferResultDto`**: se amplía con el mismo campo opcional,
  `DestinationCustomerDisplayNameMasked` (`string?`), poblado por `ConfirmTransferUseCase`/
  `GetTransferUseCase` únicamente cuando `destinationAccount.CustomerId != transfer.CustomerId`
  (research.md §5/§6) — es decir, únicamente para transferencias a terceros.

### Casos de uso de Application — se renombran/generalizan dos, se crean dos nuevos

Ver research.md §5 para la justificación completa de qué se unifica y qué se mantiene separado.

| Antes (`002`) | Ahora | Naturaleza |
|---|---|---|
| `PreviewOwnAccountTransferUseCase` | Sin cambios | — |
| `ConfirmOwnAccountTransferUseCase` | `ConfirmTransferUseCase` | Renombrado + generalizado (misma carpeta física renombrada `ConfirmTransfer/`) |
| `GetOwnAccountTransferUseCase` | `GetTransferUseCase` | Renombrado + generalizado (misma carpeta física renombrada `GetTransfer/`) |
| — | `PreviewThirdPartyTransferUseCase` | Nuevo |
| `OwnAccountTransferValidation` | Refactorizada para delegar en `CommonTransferValidation` | Sin cambio de comportamiento observable |
| — | `CommonTransferValidation` | Nuevo (extraído de `OwnAccountTransferValidation`) |
| — | `ThirdPartyTransferValidation` | Nuevo |

### `TransferRejectionReason` — se amplía, sin cambiar valores existentes

Se agregan dos miembros nuevos (research.md §7): `DestinationAccountNotFound`,
`DestinationIsOwnAccount`. Los siete valores existentes (`InvalidPreviewReference`,
`AccountNotEligible`, `SameAccount`, `AccountBlocked`, `InvalidAmount`, `InsufficientFunds`,
`IdempotencyConflict`, `ConcurrencyConflict`) no cambian de significado; `AccountBlocked` se
reutiliza tal cual también para "cuenta destino BLOQUEADA" (research.md §7).

## Nuevas relaciones o constraints

```text
accounts.number  -- UNIQUE (nuevo, research.md §3/§9)
```

Ninguna relación nueva a nivel de FK: `Transfer.DestinationAccountId` ya apuntaba a `accounts.id`
desde `002` sin restricción de que esa cuenta perteneciera al mismo cliente que
`Transfer.CustomerId` — el FK ya era estructuralmente compatible con un destino ajeno, la única
regla que lo impedía vivía en Application (`OwnAccountTransferValidation`), no en el esquema.

## Cómo se localiza la cuenta destino (resumen)

```text
Vista previa (input del cliente):  AccountNumber (string)
                                        │
                                        ▼  IAccountRepository.GetByNumberAsync
                                    AccountId (interno, resuelto una vez)
                                        │
                                        ▼  firmado dentro de TransferPreviewPayload
                            previewReference (opaco, entregado al cliente)
                                        │
Confirmación (input del cliente):  previewReference
                                        │
                                        ▼  IPreviewTokenSigner.Unprotect
                            TransferPreviewPayload.DestinationAccountId
                                        │
                                        ▼  IAccountRepository.GetByIdAsync (revalidación, FR-012)
                                    Account (estado vigente)
```

## Cómo se mantiene la privacidad del destinatario (resumen)

```text
Nunca se carga:  saldo/otros productos del destinatario (no se consulta DebitCards ni
                 se proyecta Balance de la cuenta destino en ninguna respuesta)
Se carga y se enmascara:  Customer.DisplayName -> Customer.DisplayNameMasked (Domain, único lugar)
Se decide una sola vez:  "¿incluir el nombre enmascarado?" -> destinationAccount.CustomerId !=
                         customerId actual (Application, dentro del caso de uso compartido,
                         nunca en el controller — research.md §6)
```
