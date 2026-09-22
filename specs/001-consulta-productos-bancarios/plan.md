# Implementation Plan: Consulta de productos bancarios del cliente

**Branch**: `001-consulta-productos-bancarios` | **Date**: 2026-09-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-consulta-productos-bancarios/spec.md`, más un
input de planificación detallado que fija stack, arquitectura y estrategias técnicas (ver
secciones de este documento).

## Summary

Exponer una API REST de solo lectura (.NET 10 / ASP.NET Core 10, Clean Architecture, Contract
First con OpenAPI 3.1) que permite al cliente ficticio activo listar y consultar el detalle de
sus cuentas de ahorro y tarjetas de débito (spec `001`, HU1–HU5), sin exponer productos de otros
clientes (FR-015…FR-017, FR-022) y sin operaciones de escritura (FR-021). Persistencia en
PostgreSQL vía EF Core 10, con Domain puro en C# y Value Objects para proteger enmascaramiento de
números (RB7), moneda fija PEN (RB2) y saldo con 2 decimales (RB3). El contrato OpenAPI
(`contracts/openapi/banking-products-v1.yaml`) se diseña y valida antes de cualquier controller.

## Technical Context

**Language/Version**: C# 13 sobre .NET 10 (`net10.0`)

**Primary Dependencies**: ASP.NET Core 10 · Entity Framework Core 10 ·
Npgsql.EntityFrameworkCore.PostgreSQL · FluentValidation (+ `FluentValidation.DependencyInjectionExtensions`) ·
Scalar.AspNetCore (documentación interactiva sobre el contrato OpenAPI estático) ·
Microsoft.AspNetCore.OpenApi (solo para servir/validar el documento, no para generarlo desde
código — ver research.md §7)

**Storage**: PostgreSQL 17 vía EF Core 10 / Npgsql

**Testing**: xUnit; `Testcontainers.PostgreSql` para `IntegrationTests` (justificado en
research.md §8); dobles de prueba (fakes/mocks manuales o `NSubstitute`, a confirmar en tasks) para
`Application.UnitTests`

**Target Platform**: API HTTP autohospedada (Kestrel), contenedor Linux en el entorno de
despliegue académico

**Project Type**: Servicio web (backend REST API, sin frontend en esta feature)

**Performance Goals**: No hay objetivos cuantitativos propios de esta feature más allá de
SC-005 de la spec (estado identificable en <5s de lectura, es un objetivo de UX de un futuro
consumidor, no de la API). No se define un SLA de latencia p95 porque la spec no lo exige
(Principio I: no diseñar para requisitos que no existen).

**Constraints**: Solo lectura (FR-021); sin autenticación (spec, fuera de alcance); todos los
datos ficticios (constitución, contexto académico); respuesta indistinguible ante recurso
inexistente/ajeno (FR-022).

**Scale/Scope**: Alcance de demostración académica: un cliente activo por instancia de
configuración, un puñado de cuentas/tarjetas de referencia (spec §6). No se diseña para escala de
producción (Principio I).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principios (constitution.md v1.0.0)

| Principio | Evaluación | Estado |
|---|---|---|
| I. Simplicidad ante todo | Un solo modelo Domain=Persistencia (research.md §5), sin Generic Repository, sin CQRS/MediatR/Event Sourcing, Unit of Work delgada. Toda desviación de "lo mínimo" está justificada en Complexity Tracking. | PASS |
| II. Especificaciones primero y alcance gobernado por ellas | Este plan implementa únicamente FR-001…FR-022 de `spec.md` (Status: Draft — ver nota de gobernanza abajo). No se añaden endpoints ni reglas fuera de la spec. | PASS (ver nota) |
| III. Mercado peruano | Moneda PEN fija (RB2), mensajes de error orientados al cliente en español de Perú (`ProblemDetails.title/detail`), sin otra localización. | PASS |
| IV. Clean Architecture y responsabilidades separadas | Dependency Rule explícita en "Project Structure"/"Dependency Direction"; Domain sin referencias a ASP.NET Core/EF Core/FluentValidation. | PASS |
| V. Integridad financiera | `Money` VO con 2 decimales exactos (RB3); feature de solo lectura por lo que no hay riesgo de doble aplicación de efectos monetarios (no hay comandos de escritura en `001`); trazabilidad de auditoría no aplica aún porque no hay mutaciones. | PASS |
| VI. Seguridad y mínimo privilegio | Filtrado por `CustomerId` en la propia consulta de repositorio (deny-by-default); 404 genérico indistinguible (FR-022); sin secretos/datos reales; `ICurrentCustomerProvider` resuelto en backend, nunca desde el cliente HTTP. | PASS |
| VII. Comportamiento verificable | Todos los endpoints trazan a criterios Dado/Cuando/Entonces de `spec.md` (tabla en quickstart.md). | PASS |
| VIII. Calidad automatizada | `Domain.UnitTests`, `Application.UnitTests`, `IntegrationTests` obligatorios definidos en "Testing Strategy"; capas internas testeables sin infraestructura externa (Domain/Application). | PASS |
| IX. Decisiones técnicas justificadas | Stack completo justificado en `research.md`; ninguna decisión técnica se vuelve restricción global sin pasar por enmienda constitucional. | PASS |

**Nota de gobernanza (Principio II)**: `spec.md` tiene `Status: Draft`. El Principio II exige
`Status: Approved` (sin `[NEEDS CLARIFICATION]`) antes de implementar. La spec ya no tiene
marcadores de clarificación pendientes (resueltos el 2026-09-20), por lo que solo falta el cambio
formal de estado a `Approved` por quien corresponda antes de iniciar `/speckit-tasks` /
implementación; este plan puede diseñarse sobre ella, pero la implementación debe esperar ese
cambio de estado o una decisión explícita del usuario de proceder igualmente.

### Gate adicional del input de planificación (sección 19)

- [x] La solución mantiene simplicidad (research.md §5, §9; sin patrones no solicitados por la spec).
- [x] No se amplía el alcance de `001` (solo los 4 endpoints de consulta; ver "API First Strategy").
- [x] Se respeta Clean Architecture (ver "Dependency Direction").
- [x] Se cumple la Dependency Rule (ver "Dependency Direction").
- [x] Domain permanece independiente (sin ASP.NET Core/EF Core/FluentValidation/OpenAPI/Scalar).
- [x] Los casos de uso están en Application (ver "Application Use Cases").
- [x] Infrastructure contiene EF Core/PostgreSQL (ver "Persistence Strategy").
- [x] La API sigue Contract First (ver "API First Strategy").
- [x] El contrato OpenAPI existe antes de implementación (`contracts/openapi/banking-products-v1.yaml`, completo en esta fase de plan).
- [x] Scalar es consumidor y no fuente de verdad (research.md §7).
- [x] Propiedad y privacidad están protegidas (FR-015…FR-022, ver "Error Handling").
- [x] Las pruebas automatizadas están contempladas (ver "Testing Strategy").
- [x] Unit of Work no duplica innecesariamente las responsabilidades de DbContext (ver "Unit of Work Strategy").
- [x] Cualquier complejidad adicional está justificada (ver "Complexity Tracking").

**Re-check post-diseño (tras Phase 1)**: repetido al final de este documento en
"Constitution Check — Post-Diseño".

## Project Structure

### Documentation (this feature)

```text
specs/001-consulta-productos-bancarios/
├── plan.md              # Este archivo
├── research.md          # Phase 0 — decisiones técnicas
├── data-model.md         # Phase 1 — entidades, value objects, reglas
├── quickstart.md         # Phase 1 — guía de validación manual
├── contracts/
│   └── openapi/
│       └── banking-products-v1.yaml   # Contrato REST v1, fuente de verdad
└── tasks.md              # Phase 2 (/speckit-tasks — no generado por este comando)
```

### Source Code (repository root)

```text
src/
├── BancaDigitalPeru.Domain/
│   ├── Customers/                 # Customer, CustomerId
│   ├── Accounts/                  # Account, AccountId, AccountNumber, AccountType, AccountStatus
│   ├── DebitCards/                # DebitCard, DebitCardId, CardNumber, CardStatus, CardExpiration
│   └── Common/                    # Money, CurrencyCode
│
├── BancaDigitalPeru.Application/
│   ├── Abstractions/
│   │   ├── Persistence/           # IAccountRepository, IDebitCardRepository, IUnitOfWork
│   │   └── ICurrentCustomerProvider.cs
│   ├── Accounts/
│   │   ├── ListCustomerAccounts/  # Use case + DTOs (AccountSummaryDto)
│   │   └── GetCustomerAccountDetail/
│   ├── DebitCards/
│   │   ├── ListCustomerDebitCards/
│   │   └── GetCustomerDebitCardDetail/
│   └── Common/                    # Result<T>, NotFoundReason (interno, no HTTP)
│
├── BancaDigitalPeru.Infrastructure/
│   ├── Persistence/
│   │   ├── BancaDigitalPeruDbContext.cs
│   │   ├── Configurations/        # AccountConfiguration, DebitCardConfiguration, CustomerConfiguration
│   │   ├── Repositories/          # AccountRepository, DebitCardRepository
│   │   └── UnitOfWork.cs
│   ├── Migrations/                # EF Core migrations + seed de datos ficticios (spec §6)
│   └── CurrentCustomer/
│       └── ConfiguredCurrentCustomerProvider.cs
│
└── BancaDigitalPeru.Api/
    ├── Controllers/                # AccountsController, DebitCardsController (delgados)
    ├── Contracts/                  # Request/Response records que reflejan el OpenAPI (no reusan DTOs de Application 1:1 si el contrato exige forma distinta)
    ├── Validation/                 # FluentValidation validators (formato de accountId/debitCardId)
    ├── ErrorHandling/              # Middleware/ExceptionHandler -> ProblemDetails
    ├── OpenApi/                    # Endpoint que sirve contracts/openapi/banking-products-v1.yaml + configuración de Scalar
    ├── Program.cs                  # Composition Root
    └── appsettings.json            # incluye DemoCustomer:CustomerId

tests/
├── BancaDigitalPeru.Domain.UnitTests/
├── BancaDigitalPeru.Application.UnitTests/
└── BancaDigitalPeru.IntegrationTests/
```

**Structure Decision**: Opción "Single project" adaptada a 4 proyectos de Clean Architecture (no
es el layout CLI/lib genérico de la plantilla ni el de app web con frontend separado, porque esta
feature no tiene frontend). Un único `.sln` en la raíz referencia los 4 proyectos `src/` y los 3
`tests/`, tal como exige la sección 2 del input de planificación.

## Dependency Direction

```text
BancaDigitalPeru.Infrastructure ──> BancaDigitalPeru.Application ──> BancaDigitalPeru.Domain
BancaDigitalPeru.Api ──────────────> BancaDigitalPeru.Application
BancaDigitalPeru.Api ──────────────> BancaDigitalPeru.Infrastructure   (solo Composition Root, Program.cs)
```

- `Domain` no referencia ningún otro proyecto ni paquete NuGet (C# puro).
- `Application` referencia solo `Domain`.
- `Infrastructure` referencia `Application` y `Domain`; contiene EF Core, Npgsql, la
  implementación de `ICurrentCustomerProvider` y de `IUnitOfWork`.
- `Api` referencia `Application` (para invocar casos de uso) e `Infrastructure` (únicamente en
  `Program.cs`, para registrar servicios vía DI) — ningún controller referencia tipos de
  `Infrastructure` directamente.

## Domain Model

Ver [data-model.md](./data-model.md) para el detalle completo de entidades, Value Objects e
invariantes (`Customer`, `Account`, `DebitCard`, `Money`, `AccountNumber`, `CardNumber`,
`CardExpiration`, y los enums `AccountType`, `AccountStatus`, `CardStatus`, `CurrencyCode`). No se
introducen conceptos de transferencias, Domain Events, agregados adicionales ni CQRS: la única
frontera de consistencia real en esta feature es la invariante RB6 (tarjeta-cuenta del mismo
cliente), protegida en el constructor/factory de `DebitCard`.

## Application Use Cases

Cuatro casos de uso, cada uno con su propio namespace/carpeta (feature folders), sin un mediador
genérico (no MediatR, sección 17):

| Caso de uso | Entrada | Salida | Requisitos |
|---|---|---|---|
| `ListCustomerAccountsUseCase` | — (usa `ICurrentCustomerProvider`) | `IReadOnlyList<AccountSummaryDto>` | FR-001, FR-002, FR-005…FR-007, FR-015 |
| `GetCustomerAccountDetailUseCase` | `AccountId` | `Result<AccountDetailDto>` (`Found` \| `NotFound`) | FR-003, FR-004, FR-016, FR-022 |
| `ListCustomerDebitCardsUseCase` | — (usa `ICurrentCustomerProvider`) | `IReadOnlyList<DebitCardSummaryDto>` | FR-008, FR-009, FR-012…FR-015 |
| `GetCustomerDebitCardDetailUseCase` | `DebitCardId` | `Result<DebitCardDetailDto>` (`Found` \| `NotFound`) | FR-010, FR-011, FR-017, FR-022 |

Cada caso de uso resuelve el `CustomerId` actual vía `ICurrentCustomerProvider` (nunca recibe un
`customerId` como parámetro de entrada — sección 9). `Result<T>` es un tipo mínimo propio de
Application (no se agrega una librería de Result/Railway-oriented programming: dos casos —
`Found`/`NotFound` — no justifican una dependencia nueva, Principio I).

## API First Strategy

Secuencia seguida en este plan: `spec.md` → diseño del contrato OpenAPI
(`contracts/openapi/banking-products-v1.yaml`, completado en esta misma fase) → gate de validación
(sección siguiente) → `/speckit-tasks` → implementación de controllers → verificación de
conformidad en `IntegrationTests`. Los controllers de `Api` se generarán a partir del contrato ya
existente; el contrato no se generará a partir de los controllers (research.md §7).

### Gate de validación del contrato (antes de generar tareas de controllers)

- [x] El contrato OpenAPI está definido — `contracts/openapi/banking-products-v1.yaml`.
- [x] Todas las operaciones corresponden a requisitos de la spec — `GET /accounts` (FR-001),
      `GET /accounts/{accountId}` (FR-003), `GET /debit-cards` (FR-008),
      `GET /debit-cards/{debitCardId}` (FR-010).
- [x] No existen endpoints fuera del alcance de la feature — 4 operaciones, todas `GET`, ninguna
      de transferencias/autenticación/creación/bloqueo (sección 8 del input de planificación).
- [x] Los schemas no exponen modelos de persistencia — `AccountSummary`/`AccountDetail`/
      `DebitCardSummary`/`DebitCardDetail` son contratos HTTP propios, sin campos de EF Core.
- [x] Los errores HTTP están definidos — `400` (formato inválido), `404` (genérico, FR-022),
      `500` (inesperado), todos `application/problem+json`.
- [x] El contrato es consistente con los criterios de aceptación — trazabilidad en quickstart.md.
- [x] La implementación todavía no ha comenzado — este plan no crea código de `Api`/`Infrastructure`.

**Gate: APROBADO.** Las tareas de controllers pueden generarse en `/speckit-tasks`.

## OpenAPI Contract

Ver [`contracts/openapi/banking-products-v1.yaml`](./contracts/openapi/banking-products-v1.yaml)
(OpenAPI 3.1.0). Resumen:

- **Recursos**: `/accounts`, `/accounts/{accountId}`, `/debit-cards`, `/debit-cards/{debitCardId}`.
- **Versionado**: prefijo `/api/v1`; un cambio incompatible se publicaría como `banking-products-v2.yaml` bajo `/api/v2`, nunca modificando v1 in place.
- **Identificadores**: `accountId`/`debitCardId` son `uuid` internos (research.md §1), no el número real de cuenta/tarjeta.
- **Errores**: `ProblemDetails` (RFC 9457) en español de Perú; `404` idéntico para "no existe" y "es de otro cliente" (FR-022).
- **Ejemplos**: incluidos por operación, alineados con los datos de referencia de `spec.md` §6 (Cliente A/Cliente B).

## Persistence Strategy

EF Core 10 + Npgsql, exclusivamente en `Infrastructure` (ver research.md §4, §5 para el
razonamiento completo):

- Un único `BancaDigitalPeruDbContext` con `DbSet<Customer>`, `DbSet<Account>`, `DbSet<DebitCard>`.
- Configuración vía Fluent API (`IEntityTypeConfiguration<T>`), sin Data Annotations en Domain.
- Value Objects mapeados con `OwnsOne` (`Money`, `CardExpiration`) y `HasConversion` (`AccountNumber`, `CardNumber`, enums como `string`).
- Migraciones EF Core versionadas en `Infrastructure/Migrations`, incluyendo el seed de los datos ficticios de referencia (Cliente A, Cliente B — spec §6), para que estén disponibles cada vez que la base de datos se recree (FR-020, CA12).
- Un único modelo compartido Domain = Persistencia (sin DTOs de persistencia separados) — decisión justificada en research.md §5, a revisar si `002`/`003` introducen invariantes de escritura más complejas.

## Repository Strategy

Interfaces en `Application/Abstractions/Persistence`, expresadas en términos de capacidad de
negocio, no de acceso genérico a datos:

```text
IAccountRepository
    GetByCustomerAsync(CustomerId, CancellationToken) -> IReadOnlyList<Account>
    GetByIdForCustomerAsync(CustomerId, AccountId, CancellationToken) -> Account?

IDebitCardRepository
    GetByCustomerAsync(CustomerId, CancellationToken) -> IReadOnlyList<DebitCard>
    GetByIdForCustomerAsync(CustomerId, DebitCardId, CancellationToken) -> DebitCard?
```

`GetByIdForCustomerAsync` filtra por `CustomerId` **y** por el identificador del recurso en la
misma consulta EF Core (`WHERE customer_id = @customerId AND id = @id`), de modo que "no existe" y
"pertenece a otro cliente" ya llegan indistinguibles a Application como un `null` (research.md
§6). No se expone `DbSet`, `IQueryable`, `DbContext` ni expresiones específicas de EF Core desde
`Application`. No se crea `GenericRepository<T>`: las cuatro operaciones anteriores son toda la
superficie que esta feature necesita, y una abstracción genérica no aportaría semántica de negocio
adicional (Principio I).

## Unit of Work Strategy

`Application` define:

```text
IUnitOfWork
    SaveChangesAsync(CancellationToken) -> Task<int>
```

`Infrastructure` la implementa delegando directamente en `BancaDigitalPeruDbContext.SaveChangesAsync`
— sin una segunda capa transaccional paralela, reconociendo que el propio `DbContext` ya es
conceptualmente una Unit of Work (sección 12 del input de planificación). No se agregan
`BeginTransaction()`/`Commit()`/`Rollback()` a la abstracción: ningún caso de uso de esta feature
los necesita.

**Para `001` (feature de solo lectura), ninguno de los cuatro casos de uso invoca
`IUnitOfWork`** — los repositorios de solo lectura no mutan estado, así que no hay cambios que
confirmar. La interfaz y su implementación sí se registran en DI desde ahora para que `002`/`003`
(transferencias, con escritura real) puedan consumirla sin cambios en el Composition Root. Esta
decisión (definir la abstracción ahora sin usarla todavía) se registra en Complexity Tracking.

## Dependency Injection Strategy

`Api/Program.cs` es el único Composition Root. Se organiza en métodos de extensión por capa para
mantenerlo legible:

```text
services.AddDomain()          // no-op hoy: Domain no registra nada (es C# puro)
services.AddApplication()     // casos de uso, validators de Application si los hubiera
services.AddInfrastructure(configuration)
    // DbContext (Npgsql), IAccountRepository, IDebitCardRepository, IUnitOfWork,
    // ICurrentCustomerProvider (lee DemoCustomer:CustomerId)
services.AddApiServices()     // FluentValidation validators de Api, ErrorHandling, OpenApi/Scalar
```

Todas las dependencias se declaran por constructor injection. `Domain` no conoce el contenedor.
`Application` no usa Service Locator ni resuelve `IServiceProvider` directamente en los casos de
uso.

## Validation Strategy

FluentValidation valida exclusivamente forma/sintaxis de la entrada HTTP antes de invocar el caso
de uso (p. ej. `accountId`/`debitCardId` deben ser un `Guid` bien formado) — un fallo de
validación produce `400` vía `ProblemDetails`. Las reglas de negocio (propiedad del producto,
existencia, estados ACTIVA/BLOQUEADA) permanecen en Domain/Application, nunca en un validator.
Los validators son clases independientes, testeables sin levantar la Api (`Api` puede tener sus
propios `AbstractValidator<T>` sobre los contratos de request; no se necesitan validators en
Application porque los casos de uso no reciben más que un identificador ya validado por Api).

## Error Handling

Estrategia HTTP basada en `ProblemDetails` (RFC 9457), implementada como un manejador global de
excepciones/errores en `Api/ErrorHandling` (no lógica de errores dispersa en cada controller):

| Situación | HTTP | Cuerpo |
|---|---|---|
| Formato de identificador inválido (falla FluentValidation) | 400 | `ProblemDetails` genérico "Solicitud inválida" |
| Recurso inexistente | 404 | `ProblemDetails` genérico "Recurso no encontrado" |
| Recurso perteneciente a otro cliente | 404 | **Mismo** cuerpo que el caso anterior — ver decisión y justificación en research.md §6 (FR-022, SC-002, Principio VI) |
| Error inesperado (excepción no controlada) | 500 | `ProblemDetails` genérico "Error inesperado", sin stack trace, sin excepciones de EF Core/Npgsql, sin nombres de tabla |

## Testing Strategy

Framework: xUnit en los 3 proyectos de `tests/`.

- **`Domain.UnitTests`**: `Money` (invariante 2 decimales, no negativo), `AccountNumber`/
  `CardNumber` (enmascaramiento correcto, últimos 4 dígitos), `CardExpiration` (rango de mes),
  invariante RB6 en `DebitCard` (falla si `CustomerId` no coincide con el de la cuenta). Sin
  ASP.NET Core ni PostgreSQL.
- **`Application.UnitTests`**: los 4 casos de uso, con dobles de prueba para
  `IAccountRepository`/`IDebitCardRepository`/`ICurrentCustomerProvider` — cubriendo listar
  cuentas/tarjetas propias, consultar cuenta/tarjeta propia, cliente sin productos (CL1/CL2),
  producto inexistente, producto de otro cliente (mismo resultado `NotFound` que "inexistente"),
  productos bloqueados visibles (RB4/RB5). `IUnitOfWork` sustituible por un test double en
  cualquier caso de uso que llegue a necesitarlo en el futuro.
- **`IntegrationTests`**: `BancaDigitalPeruDbContext` contra PostgreSQL real vía
  `Testcontainers.PostgreSql` (research.md §8) — mappings EF Core, migraciones + seed,
  repositorios, endpoints REST end-to-end, y conformidad de las respuestas reales contra
  `banking-products-v1.yaml`. No duplican los casos ya cubiertos por `Application.UnitTests`;
  se enfocan en lo que solo puede fallar con infraestructura real (mapeo, constraints, serialización HTTP).

## Scalability and Maintainability

API sin estado respecto del procesamiento HTTP (el único "estado" por request es el
`CustomerId` resuelto vía `ICurrentCustomerProvider`, no persistido en memoria entre requests).
`async`/`await` en toda operación de E/S, con `CancellationToken` propagado desde el controller
hasta el repositorio. Las cuatro consultas de lectura usan `AsNoTracking()` (no hay necesidad de
tracking en operaciones de solo lectura). No se implementa paginación (la spec no la exige y el
volumen de datos de demostración no la justifica — Principio I). No se introducen microservicios,
CQRS, MediatR, Event Sourcing, colas de mensajería, caché distribuida ni orquestación de
contenedores: nada de eso resuelve una necesidad presente en esta feature.

## Technical Risks

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Mapear Value Objects inmutables (constructores privados) con EF Core 10 puede requerir configuración fina (constructor binding / backing fields) | Medio — podría retrasar `Infrastructure` | Documentar el patrón exacto en la primera tarea de `AccountConfiguration`/`DebitCardConfiguration`; cubrir con `IntegrationTests` tempranos que materialicen y recuperen una entidad completa |
| `Testcontainers.PostgreSql` requiere Docker disponible en CI/entorno de desarrollo | Medio — bloquea `IntegrationTests` si Docker no está disponible | Documentar el prerrequisito en `quickstart.md` (ya incluido); `Domain.UnitTests`/`Application.UnitTests` no dependen de Docker, por lo que el resto de la suite sigue corriendo |
| Desincronización entre el `CustomerId` redundante en `DebitCard` y el de su `Account` | Bajo en `001` (no hay escritura), pero riesgo real si `002`/`003` introducen edición de tarjetas sin pasar por el mismo invariante | Mantener la verificación RB6 centralizada en el constructor/factory de `DebitCard`; revisar este diseño explícitamente al planificar cualquier feature de escritura |
| El cambio de `Status: Draft` a `Approved` en `spec.md` no está automatizado | Bajo | Señalado explícitamente en el Constitution Check; requiere una acción humana/de gobernanza antes de `/speckit-tasks` en implementación real |

## Constitution Check — Post-Diseño

Tras completar Phase 1 (data-model.md, contrato OpenAPI, quickstart.md), se repite la evaluación:
ningún diseño introducido en Phase 1 contradice los principios I–IX evaluados arriba. En
particular, el diseño del contrato OpenAPI (sección "API First Strategy") no expone modelos de
persistencia ni añade endpoints fuera de la spec, y el modelo de datos (`data-model.md`) no
introduce ninguna entidad, relación o campo que no esté ya respaldado por un requisito funcional o
regla de negocio de `spec.md`. **Constitution Check: PASS.**

## Complexity Tracking

> Se documentan aquí únicamente las decisiones que, sin ser violaciones de la constitución,
> introducen una pieza de diseño que podría parecer innecesaria para una feature puramente de
> lectura y que por tanto requieren justificación explícita (Principio I).

| Decisión | Por qué es necesaria ahora | Alternativa más simple descartada |
|---|---|---|
| Definir `IUnitOfWork` (Application) + `UnitOfWork` (Infrastructure) aunque ningún caso de uso de `001` la invoque | Mandato explícito del input de planificación (sección 12), para dejar el Composition Root y el contrato de Infrastructure estables antes de que `002`/`003` (transferencias, con escritura real) lo necesiten; introducirla después obligaría a retocar el registro de DI ya usado por features en producción | Omitir `IUnitOfWork` por completo en `001` y añadirla recién en `002` — descartada porque el propio input de planificación la exige desde ahora y porque el costo de definir hoy una interfaz de un solo método (`SaveChangesAsync`) es mínimo frente al de una migración de DI más adelante |
| `CustomerId` redundante en `DebitCard` además de en `Account` | Permite que `GetByIdForCustomerAsync` de tarjetas filtre por propietario sin `JOIN`, replicando el mismo patrón "una sola consulta filtra existencia + propiedad" usado en `Account` (research.md §6), central para FR-022 | Derivar siempre el propietario vía `JOIN` con `Account` — descartada por ahora porque duplica el patrón de consulta en dos formas distintas (una con filtro directo, otra con join) sin necesidad real, dado que no hay escritura que pueda desincronizar el campo redundante en esta feature |

Ninguna otra fila aplica: no hay violaciones de Clean Architecture, de la Dependency Rule, ni
patrones no solicitados (CQRS, microservicios, Event Sourcing, etc.) en este plan.
