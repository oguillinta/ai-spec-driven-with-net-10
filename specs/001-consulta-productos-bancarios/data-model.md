# Data Model: Consulta de productos bancarios del cliente

**Feature**: `001-consulta-productos-bancarios` | **Date**: 2026-09-21

Deriva de `spec.md` (Key Entities, Functional Requirements) y de las decisiones de `research.md`.
Este documento describe el modelo de **Domain** (C# puro). El mapeo a PostgreSQL vive en
Infrastructure y se resume al final de cada entidad; no introduce columnas ni tablas fuera de lo
necesario para representar lo descrito aquí.

## Value Objects

### CustomerId / AccountId / DebitCardId

- Envoltorio inmutable de `Guid`. Igualdad por valor.
- Un `Guid.Empty` no es válido (invariante de construcción).

### AccountNumber

- Almacena el número completo de cuenta (string de dígitos, longitud fija definida por Domain).
- Expone `Masked` (`string`): `"****" + últimos 4 dígitos`.
- El número completo **nunca** se expone fuera de Domain/Infrastructure; Application y Api solo
  reciben `Masked` (ver research.md §3, spec FR-018/FR-002/FR-004).
- Invariante: longitud y formato numérico validados en construcción.

### CardNumber

- Igual forma que `AccountNumber`: almacena el número completo, expone `Masked` con los últimos 4
  dígitos visibles (spec RB7 / FR-019, criterio CA9).

### Money

- Campos: `Amount` (`decimal`, 2 posiciones decimales exactas — RB3), `Currency` (`CurrencyCode`).
- Invariante: `Amount` no negativo (un saldo disponible no es negativo en el alcance de esta
  spec); si algún dato ficticio de referencia lo requiriera en el futuro, se revisará en la spec
  correspondiente, no aquí.
- Igualdad por valor (`Amount` + `Currency`).

### CurrencyCode (enum)

- `PEN` — único miembro soportado en esta versión (RB2/FR-005). Documentado como cerrado
  intencionalmente.

### AccountType (enum)

- `Savings` (cuenta de ahorro) — único miembro soportado en esta versión (RF-005).

### AccountStatus (enum)

- `Active` (ACTIVA), `Blocked` (BLOQUEADA) — RF-006.

### CardStatus (enum)

- `Active` (ACTIVA), `Blocked` (BLOQUEADA) — RF-013.

### CardExpiration

- Campos: `Month` (1–12), `Year`.
- Invariante: `Month` en rango 1–12.
- Formato de presentación `MM/yy` delegado a Application/Api, no a Domain.

## Entities

### Customer

Representa al propietario ficticio de productos bancarios. Su gestión (alta, identidad,
autenticación) está fuera del alcance de esta feature (spec, Assumptions); existe únicamente para
dar soporte referencial a `Account`/`DebitCard` y a los datos ficticios de referencia (Cliente A,
Cliente B).

| Campo | Tipo | Notas |
|---|---|---|
| `Id` | `CustomerId` | Clave primaria |
| `DisplayName` | `string` | Nombre ficticio, solo para datos de referencia/seed; no se expone por ningún endpoint de esta feature |

**Persistencia**: tabla `customers` (`id uuid PK`, `display_name text`).

### Account

| Campo | Tipo | Notas |
|---|---|---|
| `Id` | `AccountId` | Clave primaria |
| `CustomerId` | `CustomerId` | Propietario (FK a `customers`) — RB1 |
| `Type` | `AccountType` | Siempre `Savings` en esta versión — RF-005 |
| `Number` | `AccountNumber` | Número completo enmascarado al salir de Domain |
| `Balance` | `Money` | Saldo disponible, PEN, 2 decimales — RB3 |
| `Status` | `AccountStatus` | `Active` \| `Blocked` — RF-006/RF-007 |

**Invariantes**: `CustomerId` no vacío; `Balance.Currency == PEN` (RB2); una cuenta `Blocked`
sigue siendo un dato válido y consultable (RB4), Domain no impone reglas adicionales sobre ese
estado.

**Persistencia**: tabla `accounts` (`id uuid PK`, `customer_id uuid FK -> customers.id`,
`type text`, `number text`, `balance_amount numeric(18,2)`, `balance_currency text`,
`status text`). Índice `(customer_id)` para el listado (RF-001) e índice único implícito en
`(id, customer_id)` para la consulta de detalle con verificación de propiedad en una sola query
(research.md §6).

### DebitCard

| Campo | Tipo | Notas |
|---|---|---|
| `Id` | `DebitCardId` | Clave primaria |
| `AccountId` | `AccountId` | Cuenta asociada (FK a `accounts`) — RF-012/RB6 |
| `CustomerId` | `CustomerId` | Propietario, redundante respecto de `Account.CustomerId` por diseño (ver Rationale) |
| `Number` | `CardNumber` | Número completo, enmascarado al salir de Domain |
| `Expiration` | `CardExpiration` | Mes/año de vencimiento |
| `Status` | `CardStatus` | `Active` \| `Blocked` — RF-013/RF-014 |

**Invariante (RB6)**: en el momento de construcción/reconstrucción, `CustomerId` DEBE coincidir
con el `CustomerId` de la cuenta referenciada por `AccountId`. Esta feature es de solo lectura y
no crea tarjetas, por lo que la invariante se verifica en los datos ficticios de seed/migraciones,
no en un flujo HTTP.

**Rationale del campo `CustomerId` redundante**: permite que el repositorio de tarjetas filtre
por propietario (`WHERE customer_id = @current AND id = @cardId`) sin necesidad de un `JOIN`
contra `accounts` en cada consulta de detalle o listado, manteniendo el mismo patrón de "filtrado
por propietario en una sola consulta" usado en `Account` (research.md §6, FR-022). El precio es
mantener ambos valores sincronizados, lo cual es trivial porque no existen operaciones de
escritura en esta spec que puedan desincronizarlos.

**Persistencia**: tabla `debit_cards` (`id uuid PK`, `account_id uuid FK -> accounts.id`,
`customer_id uuid FK -> customers.id`, `number text`, `expiration_month int`,
`expiration_year int`, `status text`). Índice `(customer_id)` para el listado (RF-008).

## Relaciones

```text
Customer (1) ──── (0..N) Account
Customer (1) ──── (0..N) DebitCard        [propiedad directa, para consulta eficiente]
Account  (1) ──── (0..N) DebitCard        [RF-012: cada tarjeta referencia exactamente 1 cuenta]
```

No existen relaciones ni entidades de transferencias, autenticación ni otros productos: quedan
fuera de esta spec (ver Assumptions de `spec.md`).

## Reglas de negocio reflejadas en el modelo

| Regla de la spec | Dónde se refleja |
|---|---|
| RB1 — Propiedad de productos | `CustomerId` en `Account` y `DebitCard`; filtrado obligatorio en repositorios (ver `contracts/` y Application) |
| RB2 — Moneda única PEN | `CurrencyCode` enum de un solo miembro |
| RB3 — Saldo con 2 decimales | Invariante de construcción de `Money` |
| RB4/RB5 — Producto bloqueado sigue visible | `AccountStatus`/`CardStatus` no ocultan el registro; no hay filtro por estado en las consultas de listado |
| RB6 — Asociación tarjeta-cuenta del mismo cliente | Invariante de construcción de `DebitCard` |
| RB7 — Protección de números financieros | `AccountNumber`/`CardNumber` solo exponen `Masked` |
| FR-022 — Respuesta indistinguible ante producto inexistente/ajeno | Métodos de repositorio con filtro combinado por `CustomerId` + identificador (ver research.md §6) |

## Application DTOs (salida de los casos de uso)

Estos tipos viven en Application, no en Domain, y son los que finalmente serializa Api. Se listan
aquí porque derivan directamente de las entidades/VOs anteriores:

- `AccountSummaryDto { AccountId, AccountType, MaskedNumber, Balance (Amount, Currency), Status }`
- `AccountDetailDto` — mismos campos que `AccountSummaryDto` (spec FR-002 y FR-004 piden los
  mismos atributos en listado y detalle; no hay campos adicionales en el detalle para esta
  feature).
- `DebitCardSummaryDto { DebitCardId, MaskedNumber, Last4Digits, AccountId, Status, ExpirationMonth, ExpirationYear }`
- `DebitCardDetailDto` — mismos campos que `DebitCardSummaryDto` (spec FR-009/FR-011).

`Last4Digits` se expone como campo independiente además de `MaskedNumber` porque la spec (RF-009/
RF-011) los pide como dos datos separados; `Masked.Substring(Masked.Length - 4)` sería redundante
de mantener en dos lugares, así que `CardNumber` expone ambas propiedades (`Masked`,
`Last4Digits`) directamente.
