---

description: "Task list template for feature implementation"
---

# Tasks: Transferencias entre cuentas propias

**Input**: Design documents from `/specs/002-transferencias-cuentas-propias/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/openapi/own-account-transfers-v1.yaml](./contracts/openapi/own-account-transfers-v1.yaml),
[quickstart.md](./quickstart.md)

**Tests**: Obligatorias — el Principio VIII de la constitución exige pruebas automatizadas para
reglas de negocio críticas y casos de uso; no son opcionales en este proyecto. El riesgo financiero
de esta feature (primera operación de escritura) exige una cobertura más estricta que `001`
(plan.md, Testing Strategy).

**Organization**: Las tareas están agrupadas por historia de usuario (US1–US4, ver `spec.md`).
Esta feature extiende la solución `BancaDigitalPeru` ya existente de `001`; no crea proyectos
nuevos.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivo distinto, sin dependencias pendientes)
- **[Story]**: Historia de usuario a la que pertenece la tarea (US1…US4)
- Cada tarea incluye la ruta de archivo exacta

---

## Phase 1: Setup

- [ ] T001 [P] Agregar la referencia a `Microsoft.AspNetCore.DataProtection.Abstractions` (ya
      forma parte del framework de ASP.NET Core, sin paquete de terceros — research.md §4) en
      `src/BancaDigitalPeru.Infrastructure/BancaDigitalPeru.Infrastructure.csproj`

**Checkpoint**: Solución sigue compilando con la nueva referencia.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Comportamiento de dominio, entidades nuevas, persistencia y abstracciones que TODAS
las historias de usuario necesitan.

**⚠️ CRITICAL**: Ninguna historia de usuario puede empezar hasta que esta fase esté completa.

### Domain

- [ ] T002 Modificar `src/BancaDigitalPeru.Domain/Accounts/Account.cs`: cambiar `Balance` de
      `{ get; }` a `{ get; private set; }` y agregar los métodos `Debit(Money amount)` y
      `Credit(Money amount)` (data-model.md "Account (modificada)"): `Debit` exige misma moneda,
      `amount.Amount > 0`, `Status == AccountStatus.Active` (RF-005/FR-023) y
      `amount.Amount <= Balance.Amount` (RF-009/FR-019); `Credit` exige misma moneda,
      `amount.Amount > 0` y `Status == AccountStatus.Active` (FR-006, clarificación 2026-09-24:
      destino BLOQUEADA se rechaza igual que origen)
- [ ] T003 [P] Crear value object `TransferId` en `src/BancaDigitalPeru.Domain/Transfers/TransferId.cs`
      (envoltorio de `Guid`, invariante no-vacío, mismo patrón que `AccountId`)
- [ ] T004 [P] Crear value object `IdempotencyKey` en `src/BancaDigitalPeru.Domain/Transfers/IdempotencyKey.cs`
      (envoltorio de `string`, invariante: longitud entre 1 y 255 caracteres — data-model.md)
- [ ] T005 [P] Crear enum `TransferStatus` en `src/BancaDigitalPeru.Domain/Transfers/TransferStatus.cs`
      con único miembro `Completed` (data-model.md, cerrado intencionalmente: una transferencia
      rechazada nunca se persiste como `Transfer`)
- [ ] T006 Crear entidad `Transfer` en `src/BancaDigitalPeru.Domain/Transfers/Transfer.cs` con
      factory `Transfer.Create(...)`: invariantes `Amount.Currency == PEN` y
      `SourceAccountId != DestinationAccountId` (data-model.md) (depende de T003, T004, T005)
- [ ] T007 [P] Prueba unitaria de `Account.Debit`/`Credit` (débito válido, crédito válido, saldo
      insuficiente, importe inválido, cuenta no ACTIVA en origen y en destino) en
      `tests/BancaDigitalPeru.Domain.UnitTests/Accounts/AccountDebitCreditTests.cs` (depende de T002)
- [ ] T008 [P] Prueba unitaria de `Transfer.Create` (moneda PEN, cuentas distintas) en
      `tests/BancaDigitalPeru.Domain.UnitTests/Transfers/TransferTests.cs` (depende de T006)

### Application

- [ ] T009 [P] Crear interfaz `ITransferRepository` en
      `src/BancaDigitalPeru.Application/Abstractions/Persistence/ITransferRepository.cs`:
      `Add(Transfer)`, `GetByIdForCustomerAsync(CustomerId, TransferId, CancellationToken) -> Transfer?`,
      `GetByIdempotencyKeyAsync(IdempotencyKey, CancellationToken) -> Transfer?` (plan.md
      "Repository Strategy") (depende de T006)
- [ ] T010 [P] Crear interfaz `IPreviewTokenSigner` en
      `src/BancaDigitalPeru.Application/Abstractions/IPreviewTokenSigner.cs`:
      `Protect(TransferPreviewPayload) -> string`, `Unprotect(string) -> TransferPreviewPayload?`
      (research.md §4)
- [ ] T011 [P] Crear record `TransferPreviewPayload` en
      `src/BancaDigitalPeru.Application/Transfers/PreviewOwnAccountTransfer/TransferPreviewPayload.cs`
      (`SourceAccountId`, `DestinationAccountId`, `Amount`, `IssuedAtUtc` — data-model.md)
- [ ] T012 [P] Crear tipo de resultado `TransferOutcome` en
      `src/BancaDigitalPeru.Application/Transfers/TransferOutcome.cs`: estado `Found` con el
      payload correspondiente, o rechazo con una razón (`AccountNotEligible`, `SameAccount`,
      `AccountBlocked`, `InvalidAmount`, `InsufficientFunds`, `IdempotencyConflict`,
      `ConcurrencyConflict`) — plan.md "Application Use Case"; las razones se van consumiendo
      progresivamente en US1/US2/US3
- [ ] T013 [P] Crear `TransferResultDto` en `src/BancaDigitalPeru.Application/Transfers/TransferResultDto.cs`
      (`TransferId`, `CompletedAtUtc`, `SourceAccountId`, `DestinationAccountId`, `Amount`,
      `Status` — reutilizado por la confirmación exitosa y por la consulta de US4)

### Infrastructure

- [ ] T014 [P] Modificar `src/BancaDigitalPeru.Infrastructure/Persistence/Configurations/AccountConfiguration.cs`:
      agregar `UseXminAsConcurrencyToken()` como concurrency token nativo de PostgreSQL, sin
      columna nueva (research.md §6) (depende de T002)
- [ ] T015 [P] Crear `TransferConfiguration` (Fluent API) en
      `src/BancaDigitalPeru.Infrastructure/Persistence/Configurations/TransferConfiguration.cs`:
      tabla `transfers`, `Money` vía `OwnsOne`, `TransferId`/`AccountId`/`CustomerId`/`IdempotencyKey`
      vía `HasConversion`, `TransferStatus` vía `HasConversion<string>()`, índice único en
      `idempotency_key` (data-model.md) (depende de T006)
- [ ] T016 [P] Agregar `DbSet<Transfer> Transfers` a
      `src/BancaDigitalPeru.Infrastructure/Persistence/BancaDigitalPeruDbContext.cs` (depende de T006)
- [ ] T017 Implementar `TransferRepository` en
      `src/BancaDigitalPeru.Infrastructure/Persistence/Repositories/TransferRepository.cs`,
      implementando `ITransferRepository` con `AsNoTracking()` en las lecturas (depende de T009, T015, T016)
- [ ] T018 [P] Implementar `DataProtectionPreviewTokenSigner` en
      `src/BancaDigitalPeru.Infrastructure/Security/DataProtectionPreviewTokenSigner.cs` usando
      `IDataProtectionProvider` (research.md §4) (depende de T001, T010, T011)
- [ ] T019 Crear la migración EF Core `AddTransfersAndAccountConcurrencyToken` (tabla `transfers` +
      `xmin` en `accounts`) en `src/BancaDigitalPeru.Infrastructure/Migrations/`, incluyendo
      `migrationBuilder.UpdateData(...)` **dentro de esta misma migración nueva** (nunca editando
      la migración `AddAccounts` de `001`, ya aplicada) para poner la Cuenta B del Cliente A en
      estado `Active` con saldo suficiente para los escenarios de esta feature — editar una
      migración histórica no tiene efecto en una base de datos donde ya se aplicó (depende de
      T014, T015, T016)
- [ ] T020 Registrar `ITransferRepository -> TransferRepository` e `IPreviewTokenSigner ->
      DataProtectionPreviewTokenSigner` en `AddInfrastructure()`
      (`src/BancaDigitalPeru.Infrastructure/DependencyInjection.cs`) (depende de T017, T018)

### Api

- [ ] T021 [P] Registrar `AddDataProtection()` en el Composition Root
      (`src/BancaDigitalPeru.Api/Program.cs`) (depende de T001)
- [ ] T022 [P] Crear `TransferPreviewRequestValidator` y `ConfirmTransferRequestValidator`
      (FluentValidation: GUID válido para `sourceAccountId`/`destinationAccountId`, `amount` con
      máximo 2 decimales, `Idempotency-Key` entre 1 y 255 caracteres, `previewReference` no vacío
      — plan.md "Validation Strategy") en `src/BancaDigitalPeru.Api/Validation/TransferRequestValidators.cs`
- [ ] T023 [P] Servir el contrato `own-account-transfers-v1.yaml` como segunda fuente OpenAPI
      estática y agregarla a la configuración de Scalar en
      `src/BancaDigitalPeru.Api/OpenApi/OpenApiEndpoints.cs` (y el `Content` correspondiente en
      `BancaDigitalPeru.Api.csproj`), habilitado al menos en `Development` (plan.md "API First Strategy")

**Checkpoint**: Solución compila, migraciones aplican, `/openapi/transfers-v1.yaml` se sirve.
Las historias de usuario pueden empezar.

---

## Phase 3: User Story 1 - Transferir dinero entre mis cuentas propias (Priority: P1) 🎯 MVP

**Goal**: El cliente actual puede solicitar una vista previa de una transferencia entre dos
cuentas propias y confirmarla, ejecutando el movimiento de dinero de forma atómica.

**Independent Test**: Con el Cliente A sembrado con la Cuenta A (ACTIVA, S/ 2,500.00) y la Cuenta
B (ACTIVA, S/ 800.00), solicitar una vista previa de S/ 300.00 de A hacia B, confirmarla, y
verificar que A queda en S/ 2,200.00 y B en S/ 1,100.00.

### Application

- [ ] T024 [US1] Implementar `PreviewOwnAccountTransferUseCase` en
      `src/BancaDigitalPeru.Application/Transfers/PreviewOwnAccountTransfer/PreviewOwnAccountTransferUseCase.cs`:
      resuelve el cliente actual vía `ICurrentCustomerProvider`, carga cuenta origen y destino vía
      `IAccountRepository.GetByIdForCustomerAsync`, valida propiedad (null → `AccountNotEligible`),
      cuentas distintas (RB2 → `SameAccount`), ambas ACTIVA (RF-005/FR-006 → `AccountBlocked`),
      importe > 0 y saldo suficiente (RB4/RB7 → `InvalidAmount`/`InsufficientFunds`); en éxito
      construye `TransferPreviewPayload` y lo protege vía `IPreviewTokenSigner.Protect` (depende
      de T009, T010, T011, T012, T018, T020)
- [ ] T025 [US1] Implementar `ConfirmOwnAccountTransferUseCase` en
      `src/BancaDigitalPeru.Application/Transfers/ConfirmOwnAccountTransfer/ConfirmOwnAccountTransferUseCase.cs`:
      desprotege la referencia vía `IPreviewTokenSigner.Unprotect`, repite exactamente las mismas
      validaciones que `PreviewOwnAccountTransferUseCase` contra el estado vigente (FR-012); en
      éxito ejecuta `sourceAccount.Debit(amount)`, `destinationAccount.Credit(amount)`, crea
      `Transfer.Create(...)`, lo añade vía `ITransferRepository.Add`, y llama
      `IUnitOfWork.SaveChangesAsync()` una única vez, produciendo `TransferResultDto` (pasos 1-8 y
      10-13 de plan.md "Application Use Case"; el paso 9, verificación de idempotencia, se agrega
      en US3) (depende de T024, T013)
- [ ] T026 [US1] Prueba unitaria de `PreviewOwnAccountTransferUseCase` (caso exitoso: devuelve
      referencia y los tres datos identificados, sin modificar ningún saldo) en
      `tests/BancaDigitalPeru.Application.UnitTests/Transfers/PreviewOwnAccountTransferUseCaseTests.cs`
      (depende de T024)
- [ ] T027 [US1] Prueba unitaria de `ConfirmOwnAccountTransferUseCase` (caso exitoso: saldo origen
      decrementado y destino incrementado exactamente por el importe — CA2) en
      `tests/BancaDigitalPeru.Application.UnitTests/Transfers/ConfirmOwnAccountTransferUseCaseTests.cs`
      (depende de T025)

### Api

- [ ] T028 [US1] Crear los contratos de Api (`TransferPreviewRequest`, `TransferPreviewResponse`,
      `ConfirmTransferRequest`, `TransferResultResponse`, `MoneyResponse`) reflejando
      `own-account-transfers-v1.yaml` en `src/BancaDigitalPeru.Api/Contracts/Transfers/`
- [ ] T029 [US1] Crear `TransferOutcomeMapping` en
      `src/BancaDigitalPeru.Api/ErrorHandling/TransferOutcomeMapping.cs`: mapea cada razón de
      rechazo de `TransferOutcome` a su `ProblemDetails` (404 `AccountNotEligible`; 422
      `SameAccount`/`AccountBlocked`/`InvalidAmount`/`InsufficientFunds`; 409
      `IdempotencyConflict`/`ConcurrencyConflict` — research.md §8) (depende de T012)
- [ ] T030 [US1] Crear `TransfersController` con las acciones `POST /api/v1/transfer-previews` y
      `POST /api/v1/transfers` en `src/BancaDigitalPeru.Api/Controllers/TransfersController.cs`
      (depende de T024, T025, T028, T029, T022)

### Pruebas de integración

- [ ] T031 [US1] Prueba de integración del flujo completo vista previa → confirmación exitosa
      (CA1) y transferencia por el total del saldo disponible (CL1) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/TransfersEndpointTests.cs` (depende de T030, T019)
- [ ] T032 [US1] Prueba de integración de atomicidad: forzar un fallo antes de completar
      `SaveChangesAsync` y verificar que ambos saldos y la ausencia del `Transfer` quedan como
      antes del intento (RB6, sección 11 del plan) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/TransferAtomicityTests.cs` (depende de T030)
- [ ] T033 [US1] Prueba de integración de vista previa desactualizada al confirmar (FR-012):
      cambiar el saldo de la cuenta origen entre la vista previa y la confirmación, y verificar
      que la confirmación se rechaza sin modificar ningún saldo en
      `tests/BancaDigitalPeru.IntegrationTests/Api/TransfersEndpointTests.cs` (depende de T030)

**Checkpoint**: US1 completamente funcional y verificable de forma independiente (CA1, CA2, CL1).

---

## Phase 4: User Story 2 - Rechazar transferencias inválidas (Priority: P1)

**Goal**: Verificar explícitamente que cada intento de transferencia inválido (saldo insuficiente,
importe inválido, misma cuenta, cuenta bloqueada, cuenta ajena o inexistente) es rechazado sin
alterar ningún saldo. El mecanismo de validación ya quedó implementado en US1
(`PreviewOwnAccountTransferUseCase`/`ConfirmOwnAccountTransferUseCase` validan todas estas reglas
desde el primer momento); esta fase añade la verificación explícita de cada rama y completa el
mapeo de errores HTTP específicos.

**Independent Test**: Con las cuentas de referencia, verificar de forma aislada que cada intento
inválido devuelve el código HTTP correcto y que los saldos de ambas cuentas no cambian.

### Application

- [ ] T034 [P] [US2] Prueba unitaria de `PreviewOwnAccountTransferUseCase`: cuenta origen
      inexistente, cuenta destino inexistente, cuenta origen ajena, cuenta destino ajena (mismo
      resultado que inexistente — FR-022), misma cuenta, cuenta origen BLOQUEADA, cuenta destino
      BLOQUEADA (FR-006), importe ≤ 0, saldo insuficiente en
      `tests/BancaDigitalPeru.Application.UnitTests/Transfers/PreviewOwnAccountTransferUseCaseTests.cs`
      (depende de T024)
- [ ] T035 [P] [US2] Prueba unitaria de `ConfirmOwnAccountTransferUseCase`: los mismos 9 rechazos
      anteriores aplicados en la confirmación, verificando que ningún saldo cambia en
      `tests/BancaDigitalPeru.Application.UnitTests/Transfers/ConfirmOwnAccountTransferUseCaseTests.cs`
      (depende de T025)

### Pruebas de integración

- [ ] T036 [US2] Prueba de integración: cuenta origen o destino inexistente/ajena devuelve `404`
      (CA8, CA9, CL3, CL4); comparar explícitamente el cuerpo `ProblemDetails` de un intento con
      un GUID aleatorio inexistente contra el de un intento con una cuenta real perteneciente a
      otro cliente y verificar que son iguales (`Assert.Equal`, mismo patrón que
      `AccountsEndpointTests` de `001`) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/TransfersEndpointTests.cs` (depende de T029, T030)
- [ ] T037 [US2] Prueba de integración: importe cero/negativo (CA4, CA5), misma cuenta (CA6),
      cuenta origen BLOQUEADA (CA7) y cuenta destino BLOQUEADA devuelven `422` con `detail`
      específico y ningún saldo cambia en
      `tests/BancaDigitalPeru.IntegrationTests/Api/TransfersEndpointTests.cs` (depende de T029, T030)
- [ ] T038 [US2] Prueba de integración: saldo insuficiente (CA3) y cuenta con saldo cero no puede
      transferir un importe positivo (CL5) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/TransfersEndpointTests.cs` (depende de T029, T030)
- [ ] T039 [US2] Prueba de integración: importe con más de dos decimales rechazado con `400` por
      `TransferPreviewRequestValidator` (CL2) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/TransfersEndpointTests.cs` (depende de T022, T030)

**Checkpoint**: US1 y US2 funcionan juntas e independientemente (CA3-CA9, CL2-CL5).

---

## Phase 5: User Story 3 - Evitar transferencias duplicadas (Priority: P1)

**Goal**: Garantizar que una misma solicitud lógica de confirmación nunca aplique el movimiento de
dinero más de una vez, incluso ante reenvíos concurrentes.

**Independent Test**: Con una transferencia de S/ 300.00 ya completada, reenviar la misma
solicitud lógica (mismo `Idempotency-Key`, mismos datos) y verificar que no se produce un segundo
débito ni un segundo crédito.

### Application

- [ ] T040 [US3] Extender `ConfirmOwnAccountTransferUseCase`: antes de `Debit`/`Credit`, buscar un
      `Transfer` existente vía `ITransferRepository.GetByIdempotencyKeyAsync`; si existe y coincide
      con los datos actuales, devolver su `TransferResultDto` (replay); si existe y no coincide,
      devolver `TransferOutcome.IdempotencyConflict` (paso 9 de plan.md "Application Use Case") en
      `src/BancaDigitalPeru.Application/Transfers/ConfirmOwnAccountTransfer/ConfirmOwnAccountTransferUseCase.cs`
      (depende de T025)
- [ ] T041 [US3] Extender `ConfirmOwnAccountTransferUseCase`: capturar la violación de unicidad de
      `IdempotencyKey` tras `SaveChangesAsync` (condición de carrera entre solicitudes
      concurrentes), recargar el `Transfer` ganador y devolver su resultado como replay
      (research.md §5) en el mismo archivo (depende de T040)
- [ ] T042 [US3] Extender `ConfirmOwnAccountTransferUseCase`: capturar
      `DbUpdateConcurrencyException` tras `SaveChangesAsync` y devolver
      `TransferOutcome.ConcurrencyConflict`, sin reintento automático (research.md §6) en el mismo
      archivo (depende de T040)
- [ ] T043 [US3] Hacer obligatorio el header `Idempotency-Key` en la acción de confirmación de
      `TransfersController` (validado por `ConfirmTransferRequestValidator`, T022) en
      `src/BancaDigitalPeru.Api/Controllers/TransfersController.cs` (depende de T030, T040)
- [ ] T044 [US3] Prueba unitaria: replay idempotente (misma clave, mismos datos → mismo resultado)
      y conflicto de idempotencia (misma clave, datos distintos → `IdempotencyConflict`) en
      `tests/BancaDigitalPeru.Application.UnitTests/Transfers/ConfirmOwnAccountTransferUseCaseTests.cs`
      (depende de T040)
- [ ] T045 [US3] Prueba unitaria: la `IUnitOfWork` no confirma cambios ante un error simulado del
      repositorio (el doble de prueba lanza tras `Add`; verificar que ningún saldo del doble de
      `IAccountRepository` cambió) en
      `tests/BancaDigitalPeru.Application.UnitTests/Transfers/ConfirmOwnAccountTransferUseCaseTests.cs`
      (depende de T042)

### Pruebas de integración

- [ ] T046 [US3] Prueba de integración: solicitud duplicada secuencial (CA10, CL6) — mismo
      `Idempotency-Key`, mismos datos, un único débito/crédito aplicado en
      `tests/BancaDigitalPeru.IntegrationTests/Api/TransferIdempotencyTests.cs` (depende de T043, T019)
- [ ] T047 [US3] Prueba de integración: idempotencia concurrente — dos confirmaciones simultáneas
      con la misma `Idempotency-Key` (`Task.WhenAll`), verificar que solo una aplica el movimiento
      en `tests/BancaDigitalPeru.IntegrationTests/Api/TransferIdempotencyTests.cs` (depende de T043)
- [ ] T048 [US3] Prueba de integración: concurrencia de saldo — dos confirmaciones simultáneas
      sobre la misma cuenta origen con importes individualmente válidos pero conjuntamente
      superiores al saldo disponible (sección 12 del plan); verificar que nunca se produce
      overspending y que como máximo una tiene éxito en
      `tests/BancaDigitalPeru.IntegrationTests/Api/TransferConcurrencyTests.cs` (depende de T042, T043)

**Checkpoint**: US1, US2 y US3 funcionan juntas e independientemente (CA10, CL6, y el escenario de
concurrencia de la sección 12 del plan).

---

## Phase 6: User Story 4 - Consultar el resultado de una transferencia (Priority: P2)

**Goal**: El cliente actual puede consultar el resultado de una transferencia que realizó.

**Independent Test**: Con una transferencia ya completada y su identificador conocido, consultarla
directamente y verificar que expone los 6 datos mínimos requeridos.

### Application

- [ ] T049 [US4] Implementar `GetOwnAccountTransferUseCase` en
      `src/BancaDigitalPeru.Application/Transfers/GetOwnAccountTransfer/GetOwnAccountTransferUseCase.cs`:
      resuelve el cliente actual, busca el `Transfer` vía
      `ITransferRepository.GetByIdForCustomerAsync`, devuelve `Result<TransferResultDto>` (mismo
      patrón `Found`/`NotFound` de `001`) (depende de T009, T013, T017, T020)
- [ ] T050 [US4] Prueba unitaria de `GetOwnAccountTransferUseCase`: transferencia propia
      encontrada, inexistente, y ajena (mismo resultado que inexistente) en
      `tests/BancaDigitalPeru.Application.UnitTests/Transfers/GetOwnAccountTransferUseCaseTests.cs`
      (depende de T049)

### Api

- [ ] T051 [US4] Agregar la acción `GET /api/v1/transfers/{transferId}` a `TransfersController`,
      con validación de formato del identificador y `404` genérico ante no encontrada/ajena en
      `src/BancaDigitalPeru.Api/Controllers/TransfersController.cs` (depende de T030, T049)

### Pruebas de integración

- [ ] T052 [US4] Prueba de integración: consultar el resultado tras confirmar (CA11) y
      persistencia observable tras reiniciar la Api (CA12) en
      `tests/BancaDigitalPeru.IntegrationTests/Api/TransfersEndpointTests.cs` (depende de T051)
- [ ] T053 [US4] Prueba de integración: transferencia inexistente o ajena devuelve `404` genérico
      idéntico en ambos casos en `tests/BancaDigitalPeru.IntegrationTests/Api/TransfersEndpointTests.cs`
      (depende de T051)

**Checkpoint**: Las 4 historias de usuario están implementadas y verificadas de forma
independiente.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T054 [P] Prueba de conformidad OpenAPI en
      `tests/BancaDigitalPeru.IntegrationTests/Api/TransferOpenApiConformanceTests.cs`, validando
      que las respuestas reales de los 3 endpoints cumplen los schemas de
      `contracts/openapi/own-account-transfers-v1.yaml` (depende de T031, T036, T052)
- [ ] T055 [P] Prueba de integración verificando que un error inesperado (500) durante una
      confirmación nunca expone stack traces, excepciones de EF Core/Npgsql ni SQL en
      `tests/BancaDigitalPeru.IntegrationTests/Api/TransferErrorHandlingTests.cs` (depende de T030)
- [ ] T056 Prueba adicional verificando que `Account.Balance` solo puede modificarse a través de
      `Debit`/`Credit` (ninguna otra vía pública de mutación) en
      `tests/BancaDigitalPeru.Domain.UnitTests/Accounts/AccountDebitCreditTests.cs` (depende de T002)
- [ ] T057 Ejecutar manualmente los escenarios de `quickstart.md` de extremo a extremo
      (transferencia exitosa, saldo insuficiente, duplicidad, atomicidad, concurrencia, consulta)
      y registrar los resultados como evidencia de aceptación de la feature (depende de T031, T032,
      T033, T036, T037, T038, T039, T046, T047, T048, T052, T053)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias.
- **Foundational (Phase 2)**: depende de Setup — BLOQUEA todas las historias de usuario.
- **User Stories (Phase 3-6)**: todas dependen de Foundational.
  - US2 y US3 dependen de que US1 haya creado `PreviewOwnAccountTransferUseCase`/
    `ConfirmOwnAccountTransferUseCase`/`TransfersController` (extienden esos mismos archivos, no
    los recrean).
  - US4 depende de que US1 haya creado `TransfersController` (le agrega una acción).
- **Polish (Phase 7)**: depende de las historias de usuario completadas.

### User Story Dependencies

- **US1 (Transferir, P1)**: sin dependencia de otras historias. Primera historia implementable
  tras Foundational; construye el motor completo de validación (reutilizado por US2) y de
  ejecución (extendido por US3).
- **US2 (Rechazar inválidas, P1)**: depende de US1 — añade pruebas explícitas y el mapeo de
  errores específico sobre las validaciones que US1 ya implementó.
- **US3 (Evitar duplicados, P1)**: depende de US1 — extiende `ConfirmOwnAccountTransferUseCase`
  con la verificación de idempotencia y el manejo de conflictos de concurrencia.
- **US4 (Consultar resultado, P2)**: depende de US1 — reutiliza `TransferResultDto` y
  `TransfersController` ya creados.

### Within Each User Story

- Domain (value objects → entidad) antes de Application (interfaces/DTOs → caso de uso) antes de
  Infrastructure (configuración → repositorio → migración) antes de Api (contrato → controller)
  antes de pruebas de integración — mismo orden que en `001`.
- Las pruebas unitarias de Domain/Application se escriben junto con la clase que verifican.

### Parallel Opportunities

- Todas las tareas `[P]` de Foundational (T003–T005, T007–T013, T014–T016, T018, T021–T023) en
  paralelo entre sí, respetando sus dependencias individuales.
- Dentro de US2: T034 y T035 en paralelo (archivos distintos).
- T054 y T055 en paralelo (archivos distintos) en Polish.

---

## Parallel Example: Foundational (Domain)

```bash
# Lanzar juntos los value objects nuevos de Transfer (T003-T005):
Task: "Crear value object TransferId en src/BancaDigitalPeru.Domain/Transfers/TransferId.cs"
Task: "Crear value object IdempotencyKey en src/BancaDigitalPeru.Domain/Transfers/IdempotencyKey.cs"
Task: "Crear enum TransferStatus en src/BancaDigitalPeru.Domain/Transfers/TransferStatus.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1: Setup.
2. Completar Phase 2: Foundational (CRÍTICO).
3. Completar Phase 3: US1 (Transferir dinero entre mis cuentas propias).
4. **PARAR y VALIDAR**: probar US1 de forma independiente (quickstart.md, escenario 1).
5. Dado que US1 ya implementa todas las validaciones de negocio (research.md, plan.md), el sistema
   es financieramente seguro desde este punto, aunque US2/US3 aún no tengan sus pruebas explícitas
   ni el mapeo de errores completo — US2/US3 fortalecen la verificación, no la seguridad
   subyacente, que ya reside en US1.

### Incremental Delivery

1. Setup + Foundational → base lista.
2. US1 → transferencia exitosa + motor de validación completo → Demo.
3. US2 → verificación explícita de cada rechazo + mapeo de errores específico → Demo.
4. US3 → idempotencia y concurrencia → Demo/cierre de garantías financieras.
5. US4 → consulta de resultado → Demo.
6. Polish → conformidad OpenAPI, manejo de errores 500, validación manual de `quickstart.md`.

### Parallel Team Strategy

1. El equipo completa Setup + Foundational en conjunto.
2. Un desarrollador completa US1 (es prerrequisito de las demás).
3. Una vez lista US1, dos desarrolladores pueden avanzar en paralelo sobre US2 y US3 (archivos
   compartidos de `ConfirmOwnAccountTransferUseCase`/`TransfersController` requieren coordinación
   de merge, ya que ambas extienden los mismos archivos).
4. US4 puede iniciarse en paralelo con US2/US3 apenas US1 esté lista.

---

## Notes

- `[P]` = archivos distintos, sin dependencias pendientes entre sí.
- Las pruebas no son opcionales en este proyecto (Principio VIII de la constitución).
- A diferencia de `001`, US2 y US3 dependen explícitamente de US1 porque extienden los mismos
  archivos (`ConfirmOwnAccountTransferUseCase`, `TransfersController`) en vez de crear los suyos
  propios — coordinar el orden de merge si se trabaja en paralelo.
- Confirmar que cada prueba fallaría antes de la implementación correspondiente y pasa después.
- Hacer commit tras cada tarea o grupo lógico de tareas.
