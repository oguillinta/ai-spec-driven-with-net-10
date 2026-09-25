---

description: "Task list template for feature implementation"
---

# Tasks: Transferencias a cuentas de terceros

**Input**: Design documents from `/specs/003-transferencias-terceros/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md),
[contracts/openapi/third-party-transfers-v1.yaml](./contracts/openapi/third-party-transfers-v1.yaml),
[own-account-transfers-v1.yaml actualizado](../002-transferencias-cuentas-propias/contracts/openapi/own-account-transfers-v1.yaml),
[quickstart.md](./quickstart.md)

**Tests**: Obligatorias — el Principio VIII de la constitución exige pruebas automatizadas para
reglas de negocio críticas y casos de uso; no son opcionales en este proyecto.

**Organization**: Las tareas están agrupadas por historia de usuario (US1–US4, ver `spec.md`).
Esta feature **extiende y refactoriza** la solución `BancaDigitalPeru` ya existente de `001`/`002`;
no crea proyectos nuevos, y una parte significativa de la fase Foundational consiste en renombrar/
generalizar código ya implementado de `002` (plan.md, "Impact Analysis"), no en escribir código
nuevo desde cero.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivo distinto, sin dependencias pendientes)
- **[Story]**: Historia de usuario a la que pertenece la tarea (US1…US4)
- Cada tarea incluye la ruta de archivo exacta

---

## Phase 1: Setup

- [ ] T001 [P] Renombrar el `Purpose` de `DataProtectionPreviewTokenSigner` de
      `"BancaDigitalPeru.Transfers.OwnAccountTransferPreview.v1"` a
      `"BancaDigitalPeru.Transfers.TransferPreview.v1"` en
      `src/BancaDigitalPeru.Infrastructure/Security/DataProtectionPreviewTokenSigner.cs`
      (research.md §4: el mecanismo ahora firma vistas previas de ambos tipos; cambio de un solo
      literal, sin efecto en el contrato HTTP)

**Checkpoint**: Solución sigue compilando y la suite de `002` sigue en verde.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Reutilización/refactor del diseño financiero de `002` (research.md §0-§9) y las
abstracciones nuevas que TODAS las historias de usuario de `003` necesitan.

**⚠️ CRITICAL**: Ninguna historia de usuario puede empezar hasta que esta fase esté completa, y
esta fase **no está completa** hasta que la suite de pruebas existente de `002` (retargeted a los
nombres nuevos) siga en verde (T025).

### Domain

- [ ] T002 [P] Añadir la propiedad computada `DisplayNameMasked` a
      `src/BancaDigitalPeru.Domain/Customers/Customer.cs` (data-model.md, "Customer"): primer
      token completo de `DisplayName` + inicial del segundo token + asteriscos (p. ej.
      `"Juan Pérez García"` → `"Juan P***"`, clarificación 2026-09-25, FR-011); si `DisplayName`
      tiene un solo token, devolver ese token seguido de asteriscos sin inicial adicional (mismo
      patrón que `AccountNumber.Masked`/`CardNumber.Masked` de `001`)
- [ ] T003 [P] Prueba unitaria de `Customer.DisplayNameMasked` (nombre con dos o más tokens;
      nombre de un solo token) en
      `tests/BancaDigitalPeru.Domain.UnitTests/Customers/CustomerDisplayNameMaskedTests.cs`
      (depende de T002)

### Application — reubicación y extracción de reglas comunes

- [ ] T004 Crear `CommonTransferValidation` en
      `src/BancaDigitalPeru.Application/Transfers/CommonTransferValidation.cs`: extraer de
      `OwnAccountTransferValidation` las comprobaciones que dependen únicamente de la cuenta
      origen y el importe (research.md §6, tabla): origen `null` → `AccountNotEligible`; origen no
      `Active` → `AccountBlocked`; `amount <= 0` → `InvalidAmount`; `amount >
      sourceAccount.Balance.Amount` → `InsufficientFunds`
- [ ] T005 Refactorizar `OwnAccountTransferValidation` en
      `src/BancaDigitalPeru.Application/Transfers/OwnAccountTransferValidation.cs` para delegar en
      `CommonTransferValidation` para las reglas de origen, manteniendo únicamente sus reglas
      propias (cuentas distintas, destino del mismo cliente, destino `Active`) — **sin cambiar su
      comportamiento observable**: la suite existente de `PreviewOwnAccountTransferUseCaseTests`/
      `ConfirmOwnAccountTransferUseCaseTests` debe seguir pasando sin modificar sus aserciones
      (depende de T004)
- [ ] T006 [P] Crear `ThirdPartyTransferValidation` en
      `src/BancaDigitalPeru.Application/Transfers/ThirdPartyTransferValidation.cs`: delega en
      `CommonTransferValidation` para origen; valida además `destinationAccount == null` →
      `DestinationAccountNotFound`; `destinationAccount.CustomerId == currentCustomerId` →
      `DestinationIsOwnAccount`; `destinationAccount.Status != Active` → `AccountBlocked`
      (research.md §6/§7) (depende de T004)
- [ ] T007 [P] Reubicar `TransferPreviewPayload` de
      `src/BancaDigitalPeru.Application/Transfers/PreviewOwnAccountTransfer/TransferPreviewPayload.cs`
      a `src/BancaDigitalPeru.Application/Transfers/TransferPreviewPayload.cs`, **sin cambiar sus
      campos** (`SourceAccountId`, `DestinationAccountId`, `Amount`, `Currency`, `IssuedAtUtc` —
      research.md §4)
- [ ] T008 [P] Reubicar `TransferPreviewResult` de
      `src/BancaDigitalPeru.Application/Transfers/PreviewOwnAccountTransfer/TransferPreviewResult.cs`
      a `src/BancaDigitalPeru.Application/Transfers/TransferPreviewResult.cs`, agregando la
      propiedad opcional `DestinationCustomerDisplayNameMasked` (`string?`, `null` por defecto —
      data-model.md)
- [ ] T009 [P] Agregar la propiedad opcional `DestinationCustomerDisplayNameMasked` (`string?`) a
      `TransferResultDto` en `src/BancaDigitalPeru.Application/Transfers/TransferResultDto.cs`
      (data-model.md)
- [ ] T010 [P] Agregar los valores `DestinationAccountNotFound` y `DestinationIsOwnAccount` a
      `TransferRejectionReason` en
      `src/BancaDigitalPeru.Application/Transfers/TransferRejectionReason.cs`, sin modificar los
      7 valores existentes (research.md §7)

### Application — abstracciones nuevas

- [ ] T011 [P] Crear `ICustomerRepository` en
      `src/BancaDigitalPeru.Application/Abstractions/Persistence/ICustomerRepository.cs`: único
      método `GetByIdAsync(CustomerId, CancellationToken) -> Customer?` (data-model.md; patrón de
      un repositorio por Aggregate Root, igual que `IAccountRepository`/`ITransferRepository`)
- [ ] T012 Ampliar `IAccountRepository` en
      `src/BancaDigitalPeru.Application/Abstractions/Persistence/IAccountRepository.cs` con dos
      métodos nuevos, sin modificar los dos existentes (data-model.md):
      `Task<Account?> GetByNumberAsync(AccountNumber, CancellationToken)` (sin restricción de
      propietario) y `Task<Account?> GetByIdAsync(AccountId, CancellationToken)` (sin restricción
      de propietario; solo debe invocarse con un `AccountId` ya resuelto por el propio backend —
      research.md §3, nota de seguridad)

### Application — unificación de Confirm/Get (refactor de `002`)

- [ ] T013 Renombrar la carpeta
      `src/BancaDigitalPeru.Application/Transfers/ConfirmOwnAccountTransfer/` a
      `src/BancaDigitalPeru.Application/Transfers/ConfirmTransfer/` y la clase
      `ConfirmOwnAccountTransferUseCase` a `ConfirmTransferUseCase`
      (`ConfirmTransferUseCase.cs`); generalizar su paso 4 (carga de cuenta destino) para usar
      `IAccountRepository.GetByIdAsync` en vez de `GetByIdForCustomerAsync`; insertar la
      clasificación dinámica `var isThirdParty = destinationAccount is not null &&
      destinationAccount.CustomerId != customerId;` y aplicar `ThirdPartyTransferValidation`
      cuando `isThirdParty` sea verdadero, `OwnAccountTransferValidation` en caso contrario; al
      construir el `TransferResultDto` final, poblar `DestinationCustomerDisplayNameMasked`
      (vía `ICustomerRepository`) únicamente cuando `isThirdParty` (plan.md, "Third-Party Transfer
      Use Case") (depende de T005, T006, T008, T009, T010, T011, T012)
- [ ] T014 Renombrar la carpeta
      `src/BancaDigitalPeru.Application/Transfers/GetOwnAccountTransfer/` a
      `src/BancaDigitalPeru.Application/Transfers/GetTransfer/` y la clase
      `GetOwnAccountTransferUseCase` a `GetTransferUseCase` (`GetTransferUseCase.cs`); al construir
      el `TransferResultDto`, resolver el propietario actual de `transfer.DestinationAccountId`
      (vía `IAccountRepository.GetByIdAsync`) y poblar `DestinationCustomerDisplayNameMasked` (vía
      `ICustomerRepository`) únicamente cuando ese propietario sea distinto de `transfer.CustomerId`
      (depende de T009, T011, T012)
- [ ] T015 Actualizar los `using` de
      `src/BancaDigitalPeru.Application/Transfers/PreviewOwnAccountTransfer/PreviewOwnAccountTransferUseCase.cs`
      para referenciar `TransferPreviewPayload`/`TransferPreviewResult` en su ubicación nueva, sin
      ningún otro cambio (depende de T007, T008)
- [ ] T016 Actualizar `src/BancaDigitalPeru.Application/DependencyInjection.cs`: reemplazar los
      registros de `ConfirmOwnAccountTransferUseCase`/`GetOwnAccountTransferUseCase` por
      `ConfirmTransferUseCase`/`GetTransferUseCase` (depende de T013, T014)

### Infrastructure

- [ ] T017 [P] Agregar
      `builder.HasIndex(a => a.Number).IsUnique().HasDatabaseName("ux_accounts_number")` a
      `src/BancaDigitalPeru.Infrastructure/Persistence/Configurations/AccountConfiguration.cs`
      (research.md §3/§9)
- [ ] T018 [P] Implementar `CustomerRepository` en
      `src/BancaDigitalPeru.Infrastructure/Persistence/Repositories/CustomerRepository.cs`
      (`GetByIdAsync` con `AsNoTracking()`, mismo patrón que `AccountRepository.GetByCustomerAsync`)
      (depende de T011)
- [ ] T019 Implementar `GetByNumberAsync`/`GetByIdAsync` en
      `src/BancaDigitalPeru.Infrastructure/Persistence/Repositories/AccountRepository.cs`: **sin**
      `AsNoTracking()` en `GetByIdAsync` (mismo criterio que `GetByIdForCustomerAsync` desde `002`:
      la cuenta destino debe quedar rastreada para que `Credit()` se persista); `AsNoTracking()`
      en `GetByNumberAsync` (solo se usa en la vista previa, de solo lectura) (depende de T012)
- [ ] T020 Crear la migración EF Core `AddThirdPartyTransferSupport` en
      `src/BancaDigitalPeru.Infrastructure/Migrations/`: `CreateIndex` único sobre
      `accounts.number` (T017), y `migrationBuilder.UpdateData(...)` **dentro de esta misma
      migración nueva** (nunca editando `InitialCreate`, ya aplicada) para cambiar
      `customers.display_name` de `Cliente A`/`Cliente B` a nombres ficticios completos, p. ej.
      `"María López Torres"` (Cliente A, `a1111111-...`) y `"Juan Pérez García"` (Cliente B,
      `b2222222-...`) — research.md §9 (depende de T017)
- [ ] T021 Registrar `ICustomerRepository -> CustomerRepository` en `AddInfrastructure()`
      (`src/BancaDigitalPeru.Infrastructure/DependencyInjection.cs`) (depende de T018)

### Api

- [ ] T022 Ampliar `TransferOutcomeMapping` en
      `src/BancaDigitalPeru.Api/ErrorHandling/TransferOutcomeMapping.cs` con dos casos nuevos:
      `DestinationAccountNotFound` → 404 `type: .../errors/destination-not-found`, `title:
      "Cuenta destino no encontrada"`, `detail: "No existe ninguna cuenta con el número de cuenta
      indicado."`; `DestinationIsOwnAccount` → 422 `type: .../errors/destination-is-own-account`,
      `title: "Transferencia rechazada"`, `detail: "La cuenta destino indicada le pertenece a
      usted; use transferencias entre cuentas propias."` (contrato `third-party-transfers-v1.yaml`)
      (depende de T010)
- [ ] T023 Actualizar `src/BancaDigitalPeru.Api/Controllers/TransfersController.cs`: referenciar
      `ConfirmTransferUseCase`/`GetTransferUseCase` renombrados en el constructor y en las
      acciones existentes (`ConfirmTransfer`, `GetTransferById`), sin cambiar su firma HTTP
      (depende de T013, T014)

### Regresión de `002` (gate obligatorio antes de continuar)

- [ ] T024 [P] Renombrar
      `tests/BancaDigitalPeru.Application.UnitTests/Transfers/ConfirmOwnAccountTransferUseCaseTests.cs`
      a `ConfirmTransferUseCaseTests.cs`, actualizando únicamente el nombre de la clase bajo
      prueba (`ConfirmTransferUseCase`) — **sin modificar ninguna aserción existente** (depende de
      T013)
- [ ] T025 [P] Renombrar
      `tests/BancaDigitalPeru.Application.UnitTests/Transfers/GetOwnAccountTransferUseCaseTests.cs`
      a `GetTransferUseCaseTests.cs`, actualizando únicamente el nombre de la clase bajo prueba
      (`GetTransferUseCase`) — **sin modificar ninguna aserción existente** (depende de T014)
- [ ] T026 Ejecutar la suite completa de `BancaDigitalPeru.Domain.UnitTests` y
      `BancaDigitalPeru.Application.UnitTests` y confirmar que los 79 casos ya existentes de `001`/
      `002` (más T003) siguen en verde antes de iniciar cualquier historia de usuario de `003`
      (depende de T024, T025; checkpoint de la fase Foundational)

**Checkpoint**: Solución compila, la suite de pruebas de `002` sigue en verde bajo los nombres
nuevos, migraciones aplican. Las historias de usuario de `003` pueden empezar.

---

## Phase 3: User Story 1 - Transferir dinero a la cuenta de un tercero (Priority: P1) 🎯 MVP

**Goal**: El cliente ordenante puede solicitar una vista previa de una transferencia hacia la
cuenta de un tercero (identificada por número de cuenta) y confirmarla, ejecutando el movimiento
de dinero de forma atómica.

**Independent Test**: Con el Cliente A sembrado con la Cuenta A (ACTIVA, S/ 2,500.00) y el Cliente
B sembrado con la Cuenta B1 (ACTIVA, S/ 700.00, número `00123456780003`), solicitar una vista
previa de S/ 300.00 desde la Cuenta A hacia el número de cuenta de la Cuenta B1, confirmarla, y
verificar que la Cuenta A queda en S/ 2,200.00 y la Cuenta B1 en S/ 1,000.00.

### Application

- [ ] T027 [US1] Implementar `PreviewThirdPartyTransferUseCase` en
      `src/BancaDigitalPeru.Application/Transfers/PreviewThirdPartyTransfer/PreviewThirdPartyTransferUseCase.cs`
      (plan.md, "Third-Party Transfer Use Case", 10 pasos): resuelve cliente actual, carga cuenta
      origen vía `GetByIdForCustomerAsync`, aplica `CommonTransferValidation`, resuelve cuenta
      destino vía `IAccountRepository.GetByNumberAsync`, aplica las reglas propias de
      `ThirdPartyTransferValidation` restantes (destino inexistente/propio/bloqueado), resuelve
      `Customer` del destino vía `ICustomerRepository.GetByIdAsync` y calcula
      `DisplayNameMasked`, construye `TransferPreviewPayload` y lo protege vía
      `IPreviewTokenSigner.Protect`, devuelve `TransferPreviewResult` con
      `DestinationCustomerDisplayNameMasked` poblado (depende de T004, T006, T007, T008, T011,
      T012, T019)
- [ ] T028 [US1] Registrar `PreviewThirdPartyTransferUseCase` en
      `src/BancaDigitalPeru.Application/DependencyInjection.cs` (depende de T027)
- [ ] T029 [US1] Prueba unitaria de `PreviewThirdPartyTransferUseCase` (caso exitoso: devuelve
      referencia, cuenta destino enmascarada y `destinationCustomerDisplayName`, sin modificar
      ningún saldo) en
      `tests/BancaDigitalPeru.Application.UnitTests/Transfers/PreviewThirdPartyTransferUseCaseTests.cs`
      (depende de T027)

### Api

- [ ] T030 [US1] Crear los contratos de Api (`ThirdPartyTransferPreviewRequest`,
      `ThirdPartyTransferPreviewResponse`) reflejando `third-party-transfers-v1.yaml` en
      `src/BancaDigitalPeru.Api/Contracts/Transfers/ThirdPartyTransferContracts.cs`
- [ ] T031 [US1] Crear `ThirdPartyTransferPreviewRequestValidator` (FluentValidation:
      `sourceAccountId` GUID válido; `destinationAccountNumber` solo dígitos, longitud mínima 4
      caracteres — mismo invariante que el constructor de `AccountNumber`, research.md §3;
      `amount` con máximo 2 decimales) en
      `src/BancaDigitalPeru.Api/Validation/ThirdPartyTransferRequestValidators.cs`
- [ ] T032 [US1] Agregar la acción `POST /api/v1/third-party-transfer-previews` a
      `TransfersController` en `src/BancaDigitalPeru.Api/Controllers/TransfersController.cs`
      (depende de T027, T030, T031)
- [ ] T033 [US1] Servir `third-party-transfers-v1.yaml` como tercera fuente OpenAPI estática y
      agregarla a la configuración de Scalar en
      `src/BancaDigitalPeru.Api/OpenApi/OpenApiEndpoints.cs` (y el `Content` correspondiente en
      `src/BancaDigitalPeru.Api/BancaDigitalPeru.Api.csproj`)

### Pruebas de integración

- [ ] T034 [US1] Prueba de integración del flujo completo vista previa de terceros → confirmación
      exitosa (CA1) y transferencia por el total del saldo disponible (CL1) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransfersEndpointTests.cs` (depende
      de T020, T032)
- [ ] T035 [US1] Prueba de integración de atomicidad ante error (CA12, mismo patrón que
      `TransferAtomicityTests` de `002`, ahora con destino ajeno) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransferAtomicityTests.cs` (depende
      de T032)
- [ ] T036 [US1] Prueba de integración de vista previa desactualizada al confirmar (FR-012,
      reutilizado de `002`, ahora ejercitado también desde el flujo de terceros) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransfersEndpointTests.cs` (depende
      de T032)

**Checkpoint**: US1 completamente funcional y verificable de forma independiente (CA1, CA2, CL1).

---

## Phase 4: User Story 2 - Rechazar transferencias a terceros inválidas (Priority: P1)

**Goal**: Verificar explícitamente que cada intento de transferencia a un tercero inválido es
rechazado sin alterar ningún saldo, con el código HTTP correcto para cada motivo.

**Independent Test**: Con las cuentas de referencia, verificar de forma aislada que cada intento
inválido devuelve el código HTTP correcto y que los saldos de ambas cuentas no cambian.

### Application

- [ ] T037 [P] [US2] Prueba unitaria de `PreviewThirdPartyTransferUseCase`: cuenta origen
      inexistente, cuenta origen ajena (mismo resultado que inexistente — FR-022), cuenta origen
      bloqueada, cuenta destino inexistente, cuenta destino resulta ser propia del ordenante,
      cuenta destino bloqueada, importe ≤ 0, saldo insuficiente en
      `tests/BancaDigitalPeru.Application.UnitTests/Transfers/PreviewThirdPartyTransferUseCaseTests.cs`
      (depende de T027)
- [ ] T038 [P] [US2] Prueba unitaria de `ConfirmTransferUseCase` (rama third-party): los mismos 8
      rechazos anteriores aplicados en la confirmación, verificando que ningún saldo cambia, en
      `tests/BancaDigitalPeru.Application.UnitTests/Transfers/ConfirmTransferUseCaseTests.cs`
      (depende de T013)
- [ ] T039 [P] [US2] Prueba unitaria directa de `CommonTransferValidation` y
      `ThirdPartyTransferValidation` (sin pasar por un caso de uso completo) en
      `tests/BancaDigitalPeru.Application.UnitTests/Transfers/CommonTransferValidationTests.cs` y
      `tests/BancaDigitalPeru.Application.UnitTests/Transfers/ThirdPartyTransferValidationTests.cs`
      (depende de T004, T006)

### Pruebas de integración

- [ ] T040 [US2] Prueba de integración: cuenta origen inexistente/ajena devuelve `404` idéntico
      (CA6, mismo criterio que `002`) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransfersEndpointTests.cs` (depende
      de T022, T032)
- [ ] T041 [US2] Prueba de integración: cuenta destino inexistente devuelve `404` específico y
      revelador, distinto del 404 genérico del origen (CA7, CL2, research.md §7) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransfersEndpointTests.cs` (depende
      de T022, T032)
- [ ] T042 [US2] Prueba de integración: cuenta destino resulta ser propia del ordenante devuelve
      `422` con `type: .../errors/destination-is-own-account` (CA8, CL6) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransfersEndpointTests.cs` (depende
      de T022, T032)
- [ ] T043 [US2] Prueba de integración: importe cero/negativo (CA4, CA5), cuenta origen bloqueada
      (CA9, CL4) y cuenta destino bloqueada (CL5) devuelven `422` con `detail` específico y ningún
      saldo cambia en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransfersEndpointTests.cs` (depende
      de T022, T032)
- [ ] T044 [US2] Prueba de integración: saldo insuficiente (CA3) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransfersEndpointTests.cs` (depende
      de T022, T032)
- [ ] T045 [US2] Prueba de integración: número de cuenta con formato inválido (no numérico) y
      importe con más de dos decimales (CL7) rechazados con `400` por
      `ThirdPartyTransferPreviewRequestValidator` en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransfersEndpointTests.cs` (depende
      de T031, T032)

**Checkpoint**: US1 y US2 funcionan juntas e independientemente (CA3-CA9, CL2, CL4-CL7).

---

## Phase 5: User Story 3 - Evitar transferencias a terceros duplicadas (Priority: P1)

**Goal**: Garantizar que una misma solicitud lógica de confirmación hacia un tercero nunca aplique
el movimiento de dinero más de una vez, incluso ante reenvíos concurrentes, y que la concurrencia
proteja tanto el saldo del ordenante como el de la cuenta destino.

**Independent Test**: Con una transferencia a un tercero de S/ 300.00 ya completada, reenviar la
misma solicitud lógica y verificar que no se produce un segundo débito ni un segundo crédito.

### Pruebas de integración

- [ ] T046 [US3] Prueba de integración: solicitud duplicada secuencial hacia un tercero (CA11,
      CL9 — reutiliza la idempotencia de `ConfirmTransferUseCase`, ya implementada en Foundational;
      esta tarea solo agrega la prueba específica del flujo de terceros) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransferIdempotencyTests.cs` (depende
      de T020, T032)
- [ ] T047 [US3] Prueba de integración: idempotencia concurrente hacia un tercero — dos
      confirmaciones simultáneas con la misma `Idempotency-Key` (mismo patrón que `002`) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransferIdempotencyTests.cs` (depende
      de T032)
- [ ] T048 [US3] Prueba de integración: concurrencia de saldo del ordenante — dos confirmaciones
      simultáneas sobre la misma cuenta origen hacia terceros distintos, importes individualmente
      válidos pero conjuntamente superiores al saldo (mismo patrón que `002`, sección 16 del
      input); verificar que nunca se produce overspending en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransferConcurrencyTests.cs` (depende
      de T032)
- [ ] T049 [US3] **Prueba de integración nueva** (research.md §2, sin precedente en `002`):
      créditos concurrentes al mismo destino — dos confirmaciones simultáneas de dos ordenantes
      distintos (o del mismo ordenante desde dos cuentas propias distintas) acreditando la misma
      cuenta destino; verificar mediante dos `DbContext` reales contra el mismo Postgres que
      **ambos** créditos se aplican sin pérdida de actualizaciones (saldo final = saldo inicial +
      ambos importes, nunca solo uno) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransferConcurrencyTests.cs` (depende
      de T032)

**Checkpoint**: US1, US2 y US3 funcionan juntas e independientemente (CA11, CL9, CL10 — incluido
el escenario de créditos concurrentes verificado por primera vez).

---

## Phase 6: User Story 4 - Consultar el resultado de una transferencia a terceros (Priority: P2)

**Goal**: El cliente ordenante puede consultar el resultado de una transferencia a un tercero que
realizó, incluyendo la información permitida del destinatario, sin exponer datos no permitidos.

**Independent Test**: Con una transferencia a un tercero ya completada y su identificador
conocido, consultarla directamente y verificar que expone los datos mínimos requeridos, incluido
`destinationCustomerDisplayName`, y que no expone saldo ni otros productos del destinatario.

### Pruebas de integración

- [ ] T050 [US4] Prueba de integración: consultar el resultado tras confirmar una transferencia a
      terceros (CA13) y verificar que `destinationCustomerDisplayName` está presente y coincide
      con el nombre enmascarado esperado (p. ej. `"Juan P***"` para el seed de T020) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransfersEndpointTests.cs` (depende
      de T014, T023)
- [ ] T051 [US4] Prueba de integración: consultar el resultado de una transferencia entre cuentas
      propias (regresión de `002`) y verificar que `destinationCustomerDisplayName` está **ausente**
      en `tests/BancaDigitalPeru.IntegrationTests/Api/TransfersEndpointTests.cs` (depende de T014,
      T023)
- [ ] T052 [US4] Prueba de integración: persistencia observable tras reiniciar la Api (CA14, mismo
      patrón que `002`) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransfersEndpointTests.cs` (depende
      de T023)
- [ ] T053 [US4] Prueba de integración: la respuesta de vista previa y de resultado de una
      transferencia a terceros nunca contiene el saldo, otros productos ni ningún identificador
      interno del destinatario (RB10/FR-012, CA10) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransfersEndpointTests.cs` (depende
      de T032, T023)

**Checkpoint**: Las 4 historias de usuario están implementadas y verificadas de forma
independiente.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T054 [P] Prueba de conformidad OpenAPI en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransferOpenApiConformanceTests.cs`,
      validando que las respuestas reales del endpoint nuevo cumplen
      `third-party-transfers-v1.yaml`, y que `TransferResult` (endpoints compartidos) cumple el
      `own-account-transfers-v1.yaml` actualizado, incluido el campo opcional
      `destinationCustomerDisplayName` (depende de T034, T050)
- [ ] T055 [P] Prueba de integración verificando que un error inesperado (500) durante una
      confirmación o vista previa de terceros nunca expone stack traces, excepciones de EF Core/
      Npgsql ni SQL (mismo patrón que `TransferErrorHandlingTests` de `002`) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/ThirdPartyTransferErrorHandlingTests.cs`
      (depende de T032)
- [ ] T056 Ejecutar manualmente los escenarios de `quickstart.md` de extremo a extremo
      (transferencia exitosa a tercero, destino inexistente, destino propio, saldo insuficiente,
      origen bloqueada, duplicidad, atomicidad, concurrencia de origen, créditos concurrentes al
      destino, consulta, privacidad) y registrar los resultados como evidencia de aceptación de la
      feature (depende de T034-T053)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias.
- **Foundational (Phase 2)**: depende de Setup — BLOQUEA todas las historias de usuario. Incluye
  el refactor/renombrado de código ya implementado de `002`; termina únicamente cuando T026
  confirma que la suite existente de `002` sigue en verde.
- **User Stories (Phase 3-6)**: todas dependen de Foundational.
  - US2, US3 y US4 dependen de que US1 haya creado `PreviewThirdPartyTransferUseCase`/la acción
    `POST /third-party-transfer-previews` (extienden esos mismos archivos y flujos, no los
    recrean).
- **Polish (Phase 7)**: depende de las historias de usuario completadas.

### User Story Dependencies

- **US1 (Transferir a terceros, P1)**: depende de Foundational. Primera historia implementable;
  construye el motor completo de validación específico de terceros (reutilizado por US2).
- **US2 (Rechazar inválidas, P1)**: depende de US1 — añade pruebas explícitas sobre las
  validaciones que US1 ya implementó (la lógica de rechazo en sí ya vive en Foundational/US1).
- **US3 (Evitar duplicados, P1)**: depende de US1 — la idempotencia/concurrencia ya se implementó
  en Foundational (`ConfirmTransferUseCase` unificado); esta fase solo agrega la verificación
  explícita, incluido el escenario nuevo de créditos concurrentes.
- **US4 (Consultar resultado, P2)**: depende de US1 — la lógica ya vive en `GetTransferUseCase`
  (Foundational); esta fase solo agrega la verificación explícita.

### Within Each User Story

- Domain (T002-T003) antes de Application (T004-T016) antes de Infrastructure (T017-T021) antes de
  Api (T022-T023) antes de la regresión de `002` (T024-T026) — mismo orden que `001`/`002`.
- Las pruebas unitarias de Domain/Application se escriben junto con la clase que verifican.

### Parallel Opportunities

- T002, T003 en paralelo (Domain).
- T006, T007, T008, T009, T010, T011 en paralelo entre sí una vez completado T004 (archivos
  distintos, sin dependencias cruzadas).
- T017, T018 en paralelo (archivos distintos).
- T024, T025 en paralelo (archivos distintos).
- Dentro de US2: T037, T038, T039 en paralelo (archivos distintos).
- T054, T055 en paralelo (archivos distintos) en Polish.

---

## Parallel Example: Foundational (extracción y reubicación)

```bash
# Una vez completado T004 (CommonTransferValidation), lanzar juntas:
Task: "Crear ThirdPartyTransferValidation en src/BancaDigitalPeru.Application/Transfers/ThirdPartyTransferValidation.cs"
Task: "Reubicar TransferPreviewPayload a src/BancaDigitalPeru.Application/Transfers/TransferPreviewPayload.cs"
Task: "Reubicar y ampliar TransferPreviewResult a src/BancaDigitalPeru.Application/Transfers/TransferPreviewResult.cs"
Task: "Ampliar TransferResultDto en src/BancaDigitalPeru.Application/Transfers/TransferResultDto.cs"
Task: "Ampliar TransferRejectionReason en src/BancaDigitalPeru.Application/Transfers/TransferRejectionReason.cs"
Task: "Crear ICustomerRepository en src/BancaDigitalPeru.Application/Abstractions/Persistence/ICustomerRepository.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1: Setup.
2. Completar Phase 2: Foundational — **incluye tocar y refactorizar código real de `002`**; no
   avanzar hasta que T026 confirme que la suite existente sigue en verde.
3. Completar Phase 3: US1 (Transferir a un tercero).
4. **PARAR y VALIDAR**: probar US1 de forma independiente (quickstart.md, escenario 1).
5. Dado que US1 ya implementa todas las validaciones de negocio (Foundational + US1), el sistema
   es financieramente seguro desde este punto, aunque US2/US3/US4 aún no tengan sus pruebas
   explícitas completas.

### Incremental Delivery

1. Setup + Foundational → base lista (incluye la migración a `002`).
2. US1 → transferencia exitosa a terceros + motor de validación completo → Demo.
3. US2 → verificación explícita de cada rechazo → Demo.
4. US3 → idempotencia y concurrencia, incluido el escenario nuevo de créditos concurrentes al
   destino → Demo/cierre de garantías financieras.
5. US4 → consulta de resultado con información del destinatario → Demo.
6. Polish → conformidad OpenAPI (ambos contratos), manejo de errores 500, validación manual de
   `quickstart.md`.

### Parallel Team Strategy

1. El equipo completa Setup + Foundational en conjunto (el refactor de `002` requiere
   coordinación: nadie más debería tocar `ConfirmOwnAccountTransferUseCase`/
   `GetOwnAccountTransferUseCase` mientras se renombran).
2. Un desarrollador completa US1 (es prerrequisito de las demás).
3. Una vez lista US1, dos o tres desarrolladores pueden avanzar en paralelo sobre US2/US3/US4
   (todas son mayormente pruebas explícitas sobre archivos ya creados en Foundational/US1).

---

## Notes

- `[P]` = archivos distintos, sin dependencias pendientes entre sí.
- Las pruebas no son opcionales en este proyecto (Principio VIII de la constitución).
- A diferencia de `002`, la fase Foundational de esta feature incluye un refactor real y acotado
  de código ya implementado y probado (`ConfirmOwnAccountTransferUseCase` →
  `ConfirmTransferUseCase`, `GetOwnAccountTransferUseCase` → `GetTransferUseCase`,
  `OwnAccountTransferValidation` extrae `CommonTransferValidation`) — plan.md, "Impact Analysis".
  Ese refactor DEBE mantener en verde la suite de pruebas existente de `002` (T024-T026) antes de
  añadir ninguna cobertura nueva de `003`.
- Confirmar que cada prueba fallaría antes de la implementación correspondiente y pasa después.
- Hacer commit tras cada tarea o grupo lógico de tareas.
