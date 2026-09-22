---

description: "Task list template for feature implementation"
---

# Tasks: Consulta de productos bancarios del cliente

**Input**: Design documents from `/specs/001-consulta-productos-bancarios/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/openapi/banking-products-v1.yaml](./contracts/openapi/banking-products-v1.yaml),
[quickstart.md](./quickstart.md)

**Tests**: Obligatorias — el Principio VIII de la constitución exige pruebas automatizadas para
reglas de negocio críticas y casos de uso; no son opcionales en este proyecto. Cada historia de
usuario incluye tareas de prueba explícitas.

**Organization**: Las tareas están agrupadas por historia de usuario (US1–US5, ver `spec.md`) para
permitir implementación y prueba independiente de cada una.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivo distinto, sin dependencias pendientes)
- **[Story]**: Historia de usuario a la que pertenece la tarea (US1…US5)
- Cada tarea incluye la ruta de archivo exacta

## Path Conventions

Layout de Clean Architecture de `plan.md` (Project Structure):

```text
src/BancaDigitalPeru.Domain/
src/BancaDigitalPeru.Application/
src/BancaDigitalPeru.Infrastructure/
src/BancaDigitalPeru.Api/
tests/BancaDigitalPeru.Domain.UnitTests/
tests/BancaDigitalPeru.Application.UnitTests/
tests/BancaDigitalPeru.IntegrationTests/
```

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Inicialización de la solución .NET 10 y su estructura de proyectos.

- [X] T001 Crear `BancaDigitalPeru.sln` en la raíz del repositorio y los 7 proyectos (`src/BancaDigitalPeru.Domain`, `src/BancaDigitalPeru.Application`, `src/BancaDigitalPeru.Infrastructure`, `src/BancaDigitalPeru.Api`, `tests/BancaDigitalPeru.Domain.UnitTests`, `tests/BancaDigitalPeru.Application.UnitTests`, `tests/BancaDigitalPeru.IntegrationTests`), con las referencias de proyecto que implementan la Dependency Rule de `plan.md` (`Infrastructure → Application → Domain`; `Api → Application`; `Api → Infrastructure` solo para Composition Root; cada proyecto de test referencia el proyecto que ejercita)
- [X] T002 [P] Agregar los paquetes NuGet de `plan.md` (Technical Context) a cada proyecto: `Microsoft.EntityFrameworkCore` + `Npgsql.EntityFrameworkCore.PostgreSQL` en `src/BancaDigitalPeru.Infrastructure`; `FluentValidation` + `FluentValidation.DependencyInjectionExtensions` + `Scalar.AspNetCore` + `Microsoft.AspNetCore.OpenApi` en `src/BancaDigitalPeru.Api`; `xunit` + `Microsoft.NET.Test.Sdk` en los 3 proyectos de `tests/`; `Testcontainers.PostgreSql` + `Microsoft.AspNetCore.Mvc.Testing` en `tests/BancaDigitalPeru.IntegrationTests`
- [X] T003 [P] Crear `Directory.Build.props` en la raíz habilitando `Nullable=enable`, `ImplicitUsings=enable` y analyzers/warnings estándar (Principio "Calidad del código" de `plan.md`), aplicable a los 7 proyectos
- [X] T004 [P] Crear `src/BancaDigitalPeru.Api/appsettings.json` y `appsettings.Development.json` con `ConnectionStrings:Default` (PostgreSQL) y `DemoCustomer:CustomerId` (placeholder GUID), conforme a research.md §2
- [X] T005 [P] Crear `docker-compose.yml` en la raíz del repositorio con un servicio PostgreSQL 17 para desarrollo local, alineado con `quickstart.md` §1

**Checkpoint**: Solución compilable (proyectos vacíos con referencias correctas).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infraestructura común que TODAS las historias de usuario necesitan (contexto del
cliente actual, persistencia base, manejo de errores, documentación del contrato).

**⚠️ CRITICAL**: Ninguna historia de usuario puede empezar hasta que esta fase esté completa.

- [X] T006 [P] Crear value object `Money` en `src/BancaDigitalPeru.Domain/Common/Money.cs` (`Amount: decimal` con exactamente 2 decimales, `Currency: CurrencyCode`, invariante `Amount >= 0`, igualdad por valor — spec RB3, data-model.md)
- [X] T007 [P] Crear enum `CurrencyCode` en `src/BancaDigitalPeru.Domain/Common/CurrencyCode.cs` con único miembro `PEN` (spec RB2/FR-005, research.md §9)
- [X] T008 [P] Crear value object `CustomerId` en `src/BancaDigitalPeru.Domain/Customers/CustomerId.cs` (envoltorio de `Guid`, invariante no-vacío, igualdad por valor)
- [X] T009 Crear entidad `Customer` en `src/BancaDigitalPeru.Domain/Customers/Customer.cs` (`Id: CustomerId`, `DisplayName: string`) (depende de T008)
- [X] T010 [P] Crear interfaz `ICurrentCustomerProvider` en `src/BancaDigitalPeru.Application/Abstractions/ICurrentCustomerProvider.cs` (`Task<CustomerId> GetCurrentCustomerIdAsync(CancellationToken)`) — spec §9, research.md §2
- [X] T011 [P] Crear interfaz `IUnitOfWork` en `src/BancaDigitalPeru.Application/Abstractions/Persistence/IUnitOfWork.cs` (`Task<int> SaveChangesAsync(CancellationToken)`, sin `BeginTransaction`/`Commit`/`Rollback`) — plan.md "Unit of Work Strategy"
- [X] T012 [P] Crear tipo `Result<T>` en `src/BancaDigitalPeru.Application/Common/Result.cs` (estados `Found`/`NotFound`, sin dependencias externas) — plan.md "Application Use Cases"
- [X] T013 Crear `BancaDigitalPeruDbContext` en `src/BancaDigitalPeru.Infrastructure/Persistence/BancaDigitalPeruDbContext.cs` con `DbSet<Customer>` (depende de T009)
- [X] T014 Crear `CustomerConfiguration` (Fluent API) en `src/BancaDigitalPeru.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs` (tabla `customers`: `id uuid PK`, `display_name text`) (depende de T013)
- [X] T015 Implementar `ConfiguredCurrentCustomerProvider` en `src/BancaDigitalPeru.Infrastructure/CurrentCustomer/ConfiguredCurrentCustomerProvider.cs`, leyendo `DemoCustomer:CustomerId` vía `IOptions<DemoCustomerOptions>` (depende de T010)
- [X] T016 Implementar `UnitOfWork` en `src/BancaDigitalPeru.Infrastructure/Persistence/UnitOfWork.cs` delegando en `BancaDigitalPeruDbContext.SaveChangesAsync` (depende de T011, T013)
- [X] T017 [P] Crear manejador global de errores basado en `ProblemDetails` en `src/BancaDigitalPeru.Api/ErrorHandling/` implementando la tabla 400/404/500 de `plan.md` "Error Handling" (mensajes en español de Perú, sin stack traces ni detalles de EF Core/PostgreSQL — Principio VI)
- [X] T018 [P] Crear endpoint que sirve el archivo estático `contracts/openapi/banking-products-v1.yaml` y configurar Scalar (`Scalar.AspNetCore`) para consumirlo en `src/BancaDigitalPeru.Api/OpenApi/`, habilitado al menos en `Development` — research.md §7
- [X] T019 Configurar el Composition Root en `src/BancaDigitalPeru.Api/Program.cs` con los métodos de extensión `AddApplication()`, `AddInfrastructure(configuration)`, `AddApiServices()` (registrando `DbContext` Npgsql, `ICurrentCustomerProvider`, `IUnitOfWork`, manejo de errores y OpenAPI/Scalar) (depende de T013, T015, T016, T017, T018)
- [X] T020 Crear la migración inicial de EF Core para la tabla `customers` y sembrar Cliente A y Cliente B (`DisplayName` ficticios) en `src/BancaDigitalPeru.Infrastructure/Migrations/` (depende de T014, T019) — spec §6

**Checkpoint**: Solución arranca, expone `/scalar/v1` con el contrato v1, conecta a PostgreSQL y siembra los dos clientes de referencia. Las historias de usuario pueden empezar.

---

## Phase 3: User Story 1 - Listar mis cuentas (Priority: P1) 🎯 MVP

**Goal**: El cliente actual puede obtener la lista completa de sus cuentas de ahorro (activas y
bloqueadas) con tipo, número enmascarado, saldo disponible, moneda y estado.

**Independent Test**: Con el Cliente A sembrado con 2 cuentas (una ACTIVA, una BLOQUEADA),
`GET /api/v1/accounts` devuelve exactamente esas 2, cada una con los 5 atributos requeridos, sin
necesidad de que existan tarjetas ni otras funcionalidades.

### Domain

- [X] T021 [P] [US1] Crear value object `AccountId` en `src/BancaDigitalPeru.Domain/Accounts/AccountId.cs` (envoltorio de `Guid`, invariante no-vacío)
- [X] T022 [P] [US1] Crear enum `AccountType` en `src/BancaDigitalPeru.Domain/Accounts/AccountType.cs` con único miembro `Savings` (spec RF-005)
- [X] T023 [P] [US1] Crear enum `AccountStatus` en `src/BancaDigitalPeru.Domain/Accounts/AccountStatus.cs` con miembros `Active`, `Blocked` (spec RF-006)
- [X] T024 [P] [US1] Crear value object `AccountNumber` en `src/BancaDigitalPeru.Domain/Accounts/AccountNumber.cs` (almacena el número completo, expone `Masked` con solo los últimos 4 dígitos visibles — spec RB7/FR-002/FR-004, clarificación 2026-09-20)
- [X] T025 [US1] Crear entidad `Account` en `src/BancaDigitalPeru.Domain/Accounts/Account.cs` (`Id: AccountId`, `CustomerId: CustomerId`, `Type: AccountType`, `Number: AccountNumber`, `Balance: Money`, `Status: AccountStatus`; invariantes `CustomerId` no vacío y `Balance.Currency == PEN` — spec RB2) (depende de T006, T007, T008, T021, T022, T023, T024)
- [X] T026 [P] [US1] Prueba unitaria de `Money` (2 decimales exactos, no negativo) en `tests/BancaDigitalPeru.Domain.UnitTests/Common/MoneyTests.cs`
- [X] T027 [P] [US1] Prueba unitaria de `AccountNumber` (enmascaramiento, solo últimos 4 dígitos visibles) en `tests/BancaDigitalPeru.Domain.UnitTests/Accounts/AccountNumberTests.cs`

### Application

- [X] T028 [US1] Crear interfaz `IAccountRepository` en `src/BancaDigitalPeru.Application/Abstractions/Persistence/IAccountRepository.cs` con `GetByCustomerAsync(CustomerId, CancellationToken) -> IReadOnlyList<Account>` y `GetByIdForCustomerAsync(CustomerId, AccountId, CancellationToken) -> Account?` (ambos métodos definidos ahora para evitar reabrir este archivo en US2 — plan.md "Repository Strategy") (depende de T025)
- [X] T029 [P] [US1] Crear `AccountSummaryDto` en `src/BancaDigitalPeru.Application/Accounts/ListCustomerAccounts/AccountSummaryDto.cs` (`AccountId`, `AccountType`, `MaskedNumber`, `Balance {Amount, Currency}`, `Status` — data-model.md) (depende de T025)
- [X] T030 [US1] Implementar `ListCustomerAccountsUseCase` en `src/BancaDigitalPeru.Application/Accounts/ListCustomerAccounts/ListCustomerAccountsUseCase.cs`, resolviendo el cliente actual vía `ICurrentCustomerProvider` y mapeando `Account` → `AccountSummaryDto` (depende de T010, T028, T029)
- [X] T031 [US1] Prueba unitaria de `ListCustomerAccountsUseCase` en `tests/BancaDigitalPeru.Application.UnitTests/Accounts/ListCustomerAccountsUseCaseTests.cs`, con doble de prueba de `IAccountRepository`: cuentas propias listadas completas, cliente sin cuentas devuelve colección vacía (CL1), cuenta BLOQUEADA sigue apareciendo (RB4) (depende de T030)

### Infrastructure

- [X] T032 [US1] Crear `AccountConfiguration` (Fluent API) en `src/BancaDigitalPeru.Infrastructure/Persistence/Configurations/AccountConfiguration.cs`: `AccountNumber` vía `HasConversion`, `Money` vía `OwnsOne` (`balance_amount numeric(18,2)`, `balance_currency text`), `AccountType`/`AccountStatus` vía `HasConversion<string>()`, FK `customer_id -> customers.id`, índice `(customer_id)` (depende de T025, T013)
- [X] T033 [US1] Implementar `AccountRepository` en `src/BancaDigitalPeru.Infrastructure/Persistence/Repositories/AccountRepository.cs` implementando `IAccountRepository` con `AsNoTracking()` en ambos métodos, filtrando `GetByIdForCustomerAsync` por `customer_id` y `id` en la misma consulta (research.md §6) (depende de T028, T032)
- [X] T034 [US1] Crear migración EF Core agregando la tabla `accounts` y sembrar las 2 cuentas del Cliente A (una ACTIVA S/ 2,500.00, una BLOQUEADA S/ 800.00) y al menos 1 cuenta del Cliente B en `src/BancaDigitalPeru.Infrastructure/Migrations/` (spec §6) (depende de T032, T020)
- [X] T035 [US1] Registrar `IAccountRepository -> AccountRepository` en `AddInfrastructure()` (`src/BancaDigitalPeru.Api/Program.cs`) (depende de T033, T019)

### Api

- [X] T036 [P] [US1] Crear `AccountSummaryResponse` en `src/BancaDigitalPeru.Api/Contracts/Accounts/AccountSummaryResponse.cs`, reflejando el schema `AccountSummary` del contrato OpenAPI (depende de T029)
- [X] T037 [US1] Crear `AccountsController` con la acción `GET /api/v1/accounts` en `src/BancaDigitalPeru.Api/Controllers/AccountsController.cs`, delegando en `ListCustomerAccountsUseCase` y mapeando a `AccountSummaryResponse` (depende de T030, T036)

### Pruebas de integración

- [X] T038 [US1] Prueba de integración de `AccountRepository` contra PostgreSQL real (Testcontainers) en `tests/BancaDigitalPeru.IntegrationTests/Persistence/AccountRepositoryTests.cs`: `GetByCustomerAsync` devuelve solo las cuentas del Cliente A, incluida la BLOQUEADA (depende de T034)
- [X] T039 [US1] Prueba de integración end-to-end de `GET /api/v1/accounts` en `tests/BancaDigitalPeru.IntegrationTests/Api/AccountsEndpointTests.cs`: devuelve las 2 cuentas del Cliente A con la forma exacta de `AccountSummary` (depende de T037)

**Checkpoint**: US1 completamente funcional y verificable de forma independiente (CA1, CA3, CA11, CL1).

---

## Phase 4: User Story 2 - Consultar el detalle de una cuenta (Priority: P2)

**Goal**: El cliente actual puede consultar el detalle de una cuenta propia por su identificador.

**Independent Test**: Con una cuenta conocida del Cliente A, `GET /api/v1/accounts/{accountId}`
devuelve su detalle completo, de forma independiente de si se navegó desde el listado.

### Application

- [X] T040 [P] [US2] Crear `AccountDetailDto` en `src/BancaDigitalPeru.Application/Accounts/GetCustomerAccountDetail/AccountDetailDto.cs` (mismos campos que `AccountSummaryDto` — data-model.md) (depende de T025)
- [X] T041 [US2] Implementar `GetCustomerAccountDetailUseCase` en `src/BancaDigitalPeru.Application/Accounts/GetCustomerAccountDetail/GetCustomerAccountDetailUseCase.cs`, usando `IAccountRepository.GetByIdForCustomerAsync` y devolviendo `Result<AccountDetailDto>` (`Found`/`NotFound`) (depende de T010, T012, T028, T040)
- [X] T042 [US2] Prueba unitaria de `GetCustomerAccountDetailUseCase` en `tests/BancaDigitalPeru.Application.UnitTests/Accounts/GetCustomerAccountDetailUseCaseTests.cs`: cuenta propia → `Found`; cuenta inexistente → `NotFound`; cuenta de otro cliente → `NotFound` (mismo resultado que inexistente, FR-022) (depende de T041)

### Api

- [X] T043 [US2] Crear validador FluentValidation del formato de `accountId` (debe ser un GUID válido) en `src/BancaDigitalPeru.Api/Validation/AccountIdRouteValidator.cs`
- [X] T044 [US2] Agregar la acción `GET /api/v1/accounts/{accountId}` a `AccountsController` (`src/BancaDigitalPeru.Api/Controllers/AccountsController.cs`), devolviendo `AccountSummaryResponse` en `Found` (reutilizado, mismo shape que el detalle) y delegando el `NotFound` al manejador global de errores (404 genérico) (depende de T037, T041, T043)

### Pruebas de integración

- [X] T045 [US2] Prueba de integración de `GET /api/v1/accounts/{accountId}` en `tests/BancaDigitalPeru.IntegrationTests/Api/AccountsEndpointTests.cs`: camino feliz con una cuenta del Cliente A, y `404` con `ProblemDetails` genérico al consultar una cuenta del Cliente B (depende de T044)

**Checkpoint**: US1 y US2 funcionan juntas e independientemente (CA2, CA4 a nivel de cuentas).

---

## Phase 5: User Story 3 - Listar mis tarjetas de débito (Priority: P2)

**Goal**: El cliente actual puede obtener la lista completa de sus tarjetas de débito (activas y
bloqueadas) con número enmascarado, últimos 4 dígitos, cuenta asociada, estado y vencimiento.

**Independent Test**: Con el Cliente A sembrado con 2 tarjetas (una ACTIVA, una BLOQUEADA),
`GET /api/v1/debit-cards` devuelve exactamente esas 2, cada una con los 5 atributos requeridos.

### Domain

- [X] T046 [P] [US3] Crear value object `DebitCardId` en `src/BancaDigitalPeru.Domain/DebitCards/DebitCardId.cs` (envoltorio de `Guid`, invariante no-vacío)
- [X] T047 [P] [US3] Crear enum `CardStatus` en `src/BancaDigitalPeru.Domain/DebitCards/CardStatus.cs` con miembros `Active`, `Blocked` (spec RF-013)
- [X] T048 [P] [US3] Crear value object `CardNumber` en `src/BancaDigitalPeru.Domain/DebitCards/CardNumber.cs` (almacena el número completo, expone `Masked` y `Last4Digits` — spec RB7/FR-019, criterio CA9)
- [X] T049 [P] [US3] Crear value object `CardExpiration` en `src/BancaDigitalPeru.Domain/DebitCards/CardExpiration.cs` (`Month` 1–12, `Year`; invariante de rango de mes)
- [X] T050 [US3] Crear entidad `DebitCard` en `src/BancaDigitalPeru.Domain/DebitCards/DebitCard.cs` (`Id: DebitCardId`, `AccountId: AccountId`, `CustomerId: CustomerId`, `Number: CardNumber`, `Expiration: CardExpiration`, `Status: CardStatus`) con un factory que exige el `CustomerId` de la cuenta asociada y lanza si no coincide con el `CustomerId` de la tarjeta (invariante RB6) (depende de T008, T021, T046, T047, T048, T049)
- [X] T051 [P] [US3] Prueba unitaria de `CardNumber` (enmascaramiento, últimos 4 dígitos) en `tests/BancaDigitalPeru.Domain.UnitTests/DebitCards/CardNumberTests.cs`
- [X] T052 [P] [US3] Prueba unitaria de `CardExpiration` (rango de mes 1–12) en `tests/BancaDigitalPeru.Domain.UnitTests/DebitCards/CardExpirationTests.cs`
- [X] T053 [P] [US3] Prueba unitaria del invariante RB6 de `DebitCard` (falla si el `CustomerId` de la tarjeta no coincide con el de la cuenta asociada) en `tests/BancaDigitalPeru.Domain.UnitTests/DebitCards/DebitCardTests.cs` (depende de T050)

### Application

- [X] T054 [US3] Crear interfaz `IDebitCardRepository` en `src/BancaDigitalPeru.Application/Abstractions/Persistence/IDebitCardRepository.cs` con `GetByCustomerAsync` y `GetByIdForCustomerAsync` (mismo patrón que `IAccountRepository`, definidos ambos ahora para evitar reabrir el archivo en US4) (depende de T050)
- [X] T055 [P] [US3] Crear `DebitCardSummaryDto` en `src/BancaDigitalPeru.Application/DebitCards/ListCustomerDebitCards/DebitCardSummaryDto.cs` (`DebitCardId`, `MaskedNumber`, `Last4Digits`, `AccountId`, `Status`, `ExpirationMonth`, `ExpirationYear` — data-model.md) (depende de T050)
- [X] T056 [US3] Implementar `ListCustomerDebitCardsUseCase` en `src/BancaDigitalPeru.Application/DebitCards/ListCustomerDebitCards/ListCustomerDebitCardsUseCase.cs` (depende de T010, T054, T055)
- [X] T057 [US3] Prueba unitaria de `ListCustomerDebitCardsUseCase` en `tests/BancaDigitalPeru.Application.UnitTests/DebitCards/ListCustomerDebitCardsUseCaseTests.cs`: tarjetas propias listadas completas, cliente sin tarjetas devuelve colección vacía (CL2), tarjeta BLOQUEADA sigue apareciendo (RB5) (depende de T056)

### Infrastructure

- [X] T058 [US3] Crear `DebitCardConfiguration` (Fluent API) en `src/BancaDigitalPeru.Infrastructure/Persistence/Configurations/DebitCardConfiguration.cs`: `CardNumber` vía `HasConversion`, `CardExpiration` vía `OwnsOne` (`expiration_month int`, `expiration_year int`), `CardStatus` vía `HasConversion<string>()`, FKs `account_id -> accounts.id` y `customer_id -> customers.id`, índice `(customer_id)` (depende de T050, T032)
- [X] T059 [US3] Implementar `DebitCardRepository` en `src/BancaDigitalPeru.Infrastructure/Persistence/Repositories/DebitCardRepository.cs` implementando `IDebitCardRepository` con `AsNoTracking()`, filtrando por `customer_id` y `id` en la misma consulta de detalle (depende de T054, T058)
- [X] T060 [US3] Crear migración EF Core agregando la tabla `debit_cards` y sembrar las 2 tarjetas del Cliente A (una ACTIVA asociada a su primera cuenta, una BLOQUEADA asociada a su segunda cuenta) y al menos 1 tarjeta del Cliente B en `src/BancaDigitalPeru.Infrastructure/Migrations/` (spec §6) (depende de T058, T034)
- [X] T061 [US3] Registrar `IDebitCardRepository -> DebitCardRepository` en `AddInfrastructure()` (`src/BancaDigitalPeru.Api/Program.cs`) (depende de T059)

### Api

- [X] T062 [P] [US3] Crear `DebitCardSummaryResponse` en `src/BancaDigitalPeru.Api/Contracts/DebitCards/DebitCardSummaryResponse.cs`, reflejando el schema `DebitCardSummary` del contrato OpenAPI (depende de T055)
- [X] T063 [US3] Crear `DebitCardsController` con la acción `GET /api/v1/debit-cards` en `src/BancaDigitalPeru.Api/Controllers/DebitCardsController.cs`, delegando en `ListCustomerDebitCardsUseCase` (depende de T056, T062)

### Pruebas de integración

- [X] T064 [US3] Prueba de integración de `DebitCardRepository` contra PostgreSQL real en `tests/BancaDigitalPeru.IntegrationTests/Persistence/DebitCardRepositoryTests.cs`: `GetByCustomerAsync` devuelve solo las tarjetas del Cliente A, incluida la BLOQUEADA (depende de T060)
- [X] T065 [US3] Prueba de integración end-to-end de `GET /api/v1/debit-cards` en `tests/BancaDigitalPeru.IntegrationTests/Api/DebitCardsEndpointTests.cs`: devuelve las 2 tarjetas del Cliente A con la forma exacta de `DebitCardSummary` (depende de T063)

**Checkpoint**: US1, US2 y US3 funcionan juntas e independientemente (CA5, CA7, CA9, CA10, CL2).

---

## Phase 6: User Story 4 - Consultar el detalle de una tarjeta (Priority: P3)

**Goal**: El cliente actual puede consultar el detalle de una tarjeta de débito propia por su
identificador, incluyendo la cuenta asociada.

**Independent Test**: Con una tarjeta conocida del Cliente A, `GET /api/v1/debit-cards/{debitCardId}`
devuelve su detalle completo, incluyendo que la cuenta asociada pertenece también al Cliente A.

### Application

- [X] T066 [P] [US4] Crear `DebitCardDetailDto` en `src/BancaDigitalPeru.Application/DebitCards/GetCustomerDebitCardDetail/DebitCardDetailDto.cs` (mismos campos que `DebitCardSummaryDto` — data-model.md) (depende de T050)
- [X] T067 [US4] Implementar `GetCustomerDebitCardDetailUseCase` en `src/BancaDigitalPeru.Application/DebitCards/GetCustomerDebitCardDetail/GetCustomerDebitCardDetailUseCase.cs`, usando `IDebitCardRepository.GetByIdForCustomerAsync` y devolviendo `Result<DebitCardDetailDto>` (depende de T010, T012, T054, T066)
- [X] T068 [US4] Prueba unitaria de `GetCustomerDebitCardDetailUseCase` en `tests/BancaDigitalPeru.Application.UnitTests/DebitCards/GetCustomerDebitCardDetailUseCaseTests.cs`: tarjeta propia → `Found` con el `AccountId` de una cuenta del mismo cliente; tarjeta inexistente → `NotFound`; tarjeta de otro cliente → `NotFound` (mismo resultado que inexistente, FR-022) (depende de T067)

### Api

- [X] T069 [US4] Crear validador FluentValidation del formato de `debitCardId` (debe ser un GUID válido) en `src/BancaDigitalPeru.Api/Validation/DebitCardIdRouteValidator.cs`
- [X] T070 [US4] Agregar la acción `GET /api/v1/debit-cards/{debitCardId}` a `DebitCardsController` (`src/BancaDigitalPeru.Api/Controllers/DebitCardsController.cs`), devolviendo `DebitCardSummaryResponse` en `Found` y delegando el `NotFound` al manejador global de errores (depende de T063, T067, T069)

### Pruebas de integración

- [X] T071 [US4] Prueba de integración de `GET /api/v1/debit-cards/{debitCardId}` en `tests/BancaDigitalPeru.IntegrationTests/Api/DebitCardsEndpointTests.cs`: camino feliz con una tarjeta del Cliente A, y `404` con `ProblemDetails` genérico al consultar una tarjeta del Cliente B (depende de T070)

**Checkpoint**: Todas las consultas de producto (US1–US4) funcionan de forma independiente (CA6, CA8 a nivel de tarjetas, CA10).

---

## Phase 7: User Story 5 - Proteger mis productos (Priority: P1)

**Goal**: Verificar de forma explícita y transversal (cuentas + tarjetas) que un cliente nunca
puede ver ni distinguir productos de otro cliente, ni siquiera conociendo su identificador. El
mecanismo de protección ya queda implementado incrementalmente por el filtrado de propietario en
cada repositorio (US1–US4, ver research.md §6); esta fase añade la verificación end-to-end que
demuestra esa propiedad de seguridad de forma independiente y transversal a ambos tipos de
producto.

**Independent Test**: Con el Cliente A y el Cliente B ya sembrados (Foundational) y los 4
endpoints ya implementados (US1–US4), se puede verificar de forma aislada que ninguna respuesta
del Cliente A revela información ni existencia de productos del Cliente B.

**Nota de dependencia**: A diferencia de las demás historias, US5 depende de que US1–US4 ya estén
implementadas, porque sus criterios de aceptación ejercitan directamente los 4 endpoints de
consulta ya construidos (ver "User Story Dependencies" más abajo).

### Application

- [X] T072 [US5] Prueba unitaria adicional en `tests/BancaDigitalPeru.Application.UnitTests/Accounts/GetCustomerAccountDetailUseCaseTests.cs`: el resultado `NotFound` para "cuenta inexistente" y para "cuenta de otro cliente" son estructuralmente idénticos (mismo tipo/valor, sin distinción de motivo) — FR-022 (depende de T042)
- [X] T073 [US5] Prueba unitaria adicional en `tests/BancaDigitalPeru.Application.UnitTests/DebitCards/GetCustomerDebitCardDetailUseCaseTests.cs`: el resultado `NotFound` para "tarjeta inexistente" y para "tarjeta de otro cliente" son estructuralmente idénticos — FR-022 (depende de T068)

### Pruebas de integración

- [X] T074 [US5] Prueba de integración en `tests/BancaDigitalPeru.IntegrationTests/Api/AccountsEndpointTests.cs`: el cuerpo `ProblemDetails` de `GET /accounts/{idDeUnaCuentaDelClienteB}` es idéntico (mismo `title`/`status`/`detail`) al de `GET /accounts/{idAleatorioInexistente}` (FR-022, SC-002) (depende de T045)
- [X] T075 [US5] Prueba de integración en `tests/BancaDigitalPeru.IntegrationTests/Api/DebitCardsEndpointTests.cs`: el cuerpo `ProblemDetails` de `GET /debit-cards/{idDeUnaTarjetaDelClienteB}` es idéntico al de `GET /debit-cards/{idAleatorioInexistente}` (depende de T071)
- [X] T076 [US5] Prueba de integración en `tests/BancaDigitalPeru.IntegrationTests/Api/CrossCustomerIsolationTests.cs`: `GET /api/v1/accounts` y `GET /api/v1/debit-cards` como Cliente A no incluyen ningún identificador sembrado del Cliente B (FR-015 de extremo a extremo) (depende de T039, T065)

**Checkpoint**: Las 5 historias de usuario están implementadas y su propiedad de seguridad
transversal queda verificada de forma explícita (CA4, CA8, FR-022, SC-002).

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Verificaciones transversales que no pertenecen a una sola historia de usuario.

- [X] T077 [P] Prueba de arquitectura verificando que el ensamblado `BancaDigitalPeru.Domain` no referencia ASP.NET Core, EF Core, FluentValidation, OpenAPI ni Scalar (Principio IV / Dependency Rule) en `tests/BancaDigitalPeru.Domain.UnitTests/ArchitectureTests.cs`
- [X] T078 [P] Cubrir el caso límite CL3 (cuenta con saldo S/ 0.00 sigue visible normalmente) en `tests/BancaDigitalPeru.Application.UnitTests/Accounts/ListCustomerAccountsUseCaseTests.cs`
- [X] T079 [P] Cubrir el caso límite CL4 (todas las cuentas/tarjetas bloqueadas siguen apareciendo con su estado) en `tests/BancaDigitalPeru.Application.UnitTests/Accounts/ListCustomerAccountsUseCaseTests.cs` y `tests/BancaDigitalPeru.Application.UnitTests/DebitCards/ListCustomerDebitCardsUseCaseTests.cs`
- [X] T080 [P] Cubrir el caso límite CL7 (tarjeta asociada a una cuenta bloqueada sigue siendo visible, sin comportamiento adicional) en `tests/BancaDigitalPeru.Application.UnitTests/DebitCards/ListCustomerDebitCardsUseCaseTests.cs`
- [X] T081 Prueba de conformidad OpenAPI en `tests/BancaDigitalPeru.IntegrationTests/Api/OpenApiConformanceTests.cs`, validando que las respuestas reales de los 4 endpoints cumplen los schemas de `contracts/openapi/banking-products-v1.yaml` (depende de T039, T045, T065, T071)
- [X] T082 Prueba de integración verificando que un error inesperado (500) nunca expone stack trace, excepciones de EF Core/Npgsql ni nombres de tabla en `tests/BancaDigitalPeru.IntegrationTests/Api/ErrorHandlingTests.cs` (depende de T017)
- [X] T083 Ejecutar manualmente los escenarios de `quickstart.md` de extremo a extremo y registrar los resultados como evidencia de aceptación de la feature (depende de T074, T075, T076, T081)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias — puede iniciar de inmediato.
- **Foundational (Phase 2)**: depende de Setup — BLOQUEA todas las historias de usuario.
- **User Stories (Phase 3–7)**: todas dependen de Foundational.
  - US1 (P1), US2 (P2), US3 (P2), US4 (P3) pueden implementarse en el orden dado o con la
    superposición que permitan sus dependencias internas (ver abajo).
  - **US5 (P1) es la excepción**: aunque es prioridad P1, sus tareas dependen de que US1–US4 ya
    existan, porque ejercita directamente sus 4 endpoints (ver "User Story Dependencies").
- **Polish (Phase 8)**: depende de las historias de usuario que se hayan completado.

### User Story Dependencies

- **US1 (Listar cuentas, P1)**: sin dependencia de otras historias. Primera historia implementable tras Foundational.
- **US2 (Detalle de cuenta, P2)**: reutiliza `IAccountRepository`/`AccountsController` creados en US1 (T028, T037); no puede implementarse antes de US1.
- **US3 (Listar tarjetas, P2)**: reutiliza `AccountId` de US1 (T021) para el FK de `DebitCard`; sin dependencia funcional de US2.
- **US4 (Detalle de tarjeta, P3)**: reutiliza `IDebitCardRepository`/`DebitCardsController` creados en US3 (T054, T063); no puede implementarse antes de US3.
- **US5 (Proteger mis productos, P1)**: depende de US1, US2, US3 y US4 completas — sus pruebas ejercitan los 4 endpoints ya construidos. El *mecanismo* de protección (filtrado por propietario) ya está vigente desde US1/US3; US5 solo añade su verificación end-to-end explícita.

### Within Each User Story

- Domain (value objects → entidad) antes de Application (interfaces/DTOs → caso de uso) antes de
  Infrastructure (configuración EF Core → repositorio → migración) antes de Api (contrato →
  controller) antes de pruebas de integración.
- Las pruebas unitarias de Domain/Application se escriben junto con la clase que verifican
  (mismo bloque de tareas) y deben pasar antes de dar la sub-tarea por completa.

### Parallel Opportunities

- Todas las tareas `[P]` de Setup (T002–T005) en paralelo.
- Todas las tareas `[P]` de Foundational (T006–T008, T010–T012, T017–T018) en paralelo entre sí.
- Dentro de US1: T021–T024 (value objects) en paralelo; T026–T027 (pruebas) en paralelo entre sí.
- Dentro de US3: T046–T049 (value objects) en paralelo; T051–T053 (pruebas) en paralelo entre sí.
- US1 y US3 comparten Foundational pero, salvo por la dependencia de `AccountId` (T021) para
  `DebitCard` (T050), podrían avanzar en paralelo con dos desarrolladores.

---

## Parallel Example: User Story 1

```bash
# Lanzar juntos los value objects de US1 (T021–T024):
Task: "Crear value object AccountId en src/BancaDigitalPeru.Domain/Accounts/AccountId.cs"
Task: "Crear enum AccountType en src/BancaDigitalPeru.Domain/Accounts/AccountType.cs"
Task: "Crear enum AccountStatus en src/BancaDigitalPeru.Domain/Accounts/AccountStatus.cs"
Task: "Crear value object AccountNumber en src/BancaDigitalPeru.Domain/Accounts/AccountNumber.cs"

# Lanzar juntas las pruebas de Domain de US1 (T026–T027), una vez creadas las clases anteriores:
Task: "Prueba unitaria de Money en tests/BancaDigitalPeru.Domain.UnitTests/Common/MoneyTests.cs"
Task: "Prueba unitaria de AccountNumber en tests/BancaDigitalPeru.Domain.UnitTests/Accounts/AccountNumberTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 + User Story 5)

1. Completar Phase 1: Setup.
2. Completar Phase 2: Foundational (CRÍTICO — bloquea todas las historias).
3. Completar Phase 3: US1 (Listar mis cuentas).
4. **PARAR y VALIDAR**: probar US1 de forma independiente (quickstart.md, pasos 1–2).
5. Dado que US5 depende de US1–US4, el MVP mínimamente demostrable de "cuentas protegidas" es
   US1 + US2 (para poder ejercitar la denegación a nivel de detalle) — un MVP de solo-listado
   (US1) ya cumple FR-015 (solo se listan productos propios) aunque no verifique aún FR-022 a
   nivel de detalle.

### Incremental Delivery

1. Setup + Foundational → base lista.
2. US1 → listado de cuentas → Demo.
3. US2 → detalle de cuenta → Demo.
4. US3 → listado de tarjetas → Demo.
5. US4 → detalle de tarjeta → Demo.
6. US5 → verificación transversal de seguridad → Demo/cierre de la feature `001`.
7. Polish → auditoría de arquitectura, casos límite restantes, conformidad OpenAPI, validación
   manual de `quickstart.md`.

### Parallel Team Strategy

1. El equipo completa Setup + Foundational en conjunto.
2. Una vez lista Foundational:
   - Desarrollador A: US1 → luego US2 (mismo controller/repositorio).
   - Desarrollador B: US3 → luego US4 (mismo controller/repositorio).
3. US5 se asigna a quien termine primero, una vez que US1–US4 estén todas completas.

---

## Notes

- `[P]` = archivos distintos, sin dependencias pendientes entre sí.
- La etiqueta `[Story]` mapea cada tarea a su historia de usuario para trazabilidad con `spec.md`.
- Las pruebas no son opcionales en este proyecto (Principio VIII de la constitución).
- US5 es la única historia con dependencia explícita de otras historias (US1–US4); todas las
  demás son independientes entre sí más allá de la reutilización de archivos ya creados
  (controllers, repositorios) descrita en "User Story Dependencies".
- Confirmar que cada prueba fallaría antes de la implementación correspondiente y pasa después.
- Hacer commit tras cada tarea o grupo lógico de tareas.
- Detenerse en cualquier checkpoint para validar una historia de forma independiente.
