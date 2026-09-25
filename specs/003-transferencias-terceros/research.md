# Research: Transferencias a cuentas de terceros

**Feature**: `003-transferencias-terceros` | **Date**: 2026-09-25

**Purpose**: Esta feature reutiliza casi íntegramente el diseño financiero de `002` (atomicidad,
concurrencia, idempotencia, Unit of Work, `Transfer`, `Money`). Este documento resuelve
únicamente las decisiones nuevas o afectadas por `003`, sin repetir investigación ya cerrada en
`002-transferencias-cuentas-propias/research.md` (referenciado como "research.md de 002" cuando
aplica sin cambios).

## 0. Qué reutiliza `003` sin ningún cambio

**Decision**: Reutilizar sin modificar: `Money`/`CurrencyCode`, `Account.Debit`/`Credit`,
`AccountId`, `Transfer`/`Transfer.Create`, `TransferId`, `IdempotencyKey`, `TransferStatus`,
`ITransferRepository` (interfaz completa, sin cambios de forma), `IUnitOfWork`/`UnitOfWork`
(incluida la traducción de excepciones EF/Npgsql → `ConcurrencyConflictException`/
`UniqueConstraintViolationException`), el concurrency token `xmin` de `Account`
(`AccountConfiguration`), el índice único de `transfers.idempotency_key`, `IPreviewTokenSigner`/
`DataProtectionPreviewTokenSigner` (mecanismo, no el `Purpose` — ver §4), `TransferOutcome<T>`,
`ApiProblemDetails`/`GlobalExceptionHandler`, `ICurrentCustomerProvider`, y el patrón
`AddApplication()`/`AddInfrastructure()`/`AddApiServices()`.

**Rationale**: Ninguna de estas piezas asume que ambas cuentas de una transferencia pertenecen al
mismo cliente a nivel de su propio contrato/comportamiento — esa suposición vivía únicamente en
`OwnAccountTransferValidation` (Application) y en cómo `PreviewOwnAccountTransferUseCase`
resolvía la cuenta destino (§5). Aislar la suposición allí, en vez de esparcirla en Domain/
Infrastructure, es precisamente lo que permite esta reutilización casi total (Principio I;
instrucción explícita de la sección 4 del input: no crear `ThirdPartyTransferRepository`/
`ThirdPartyTransferUnitOfWork`).

**Alternatives considered**: Ninguna — esto es un inventario, no una decisión con alternativas.

## 1. Reutilización de la estrategia de idempotencia de `002`

**Decision**: Reutilizar exactamente el mecanismo de `002` (research.md de 002 §5): header
`Idempotency-Key` obligatorio en la confirmación, verificación previa por
`ITransferRepository.GetByIdempotencyKeyAsync`, índice único persistente como respaldo ante
condiciones de carrera, y el mismo orden (idempotencia comprobada **antes** de cualquier
validación de negocio, no después — ver research.md de 002 §5 y el comentario de código en
`ConfirmOwnAccountTransferUseCase`). Ninguna pieza de este mecanismo distingue entre transferencia
propia y a terceros: `Transfer.IdempotencyKey` es un valor opaco por diseño.

**Rationale**: Cumple RF-031/RF-032/RB11 de `003` sin ninguna pieza nueva. La sección 14 del input
de planificación exige explícitamente no crear una segunda implementación.

**Alternatives considered**: Ninguna alternativa evaluada — es reutilización directa, no una
decisión de diseño nueva.

## 2. Comportamiento concurrente sobre la cuenta destino (créditos concurrentes)

**Decision**: El mismo concurrency token `xmin` que protege el débito concurrente sobre una cuenta
origen (research.md de 002 §6) protege igualmente un crédito concurrente sobre una cuenta
destino, porque `xmin` es una propiedad de la **fila** `accounts`, no del rol que esa cuenta
cumple en una transferencia particular. Dos `Credit()` concurrentes contra la misma cuenta destino
(p. ej. dos ordenantes distintos transfiriéndole dinero al mismo tercero) generan dos `UPDATE`
sobre la misma fila con el mismo `xmin` de partida; el segundo en confirmar falla con
`DbUpdateConcurrencyException` exactamente igual que en el escenario de débito concurrente de
`002`, sin necesidad de ningún mecanismo adicional.

**Rationale**: La sección 16 del input de planificación exige verificar explícitamente que la
estrategia de `002` cubre también créditos concurrentes al mismo destino (lost updates), no solo
el overspending del ordenante. El análisis confirma que sí la cubre, porque EF Core aplica el
concurrency check por fila actualizada, sin importar si esa fila es la cuenta origen o la cuenta
destino de la operación. No se requiere ajustar el diseño de `002`; se requiere una prueba de
integración nueva que ejercite este escenario específico (nunca antes probado, porque `002` no
tenía dos ordenantes distintos acreditando a la misma cuenta), para convertir esta garantía
teórica en una garantía verificada (Principio VIII).

**Alternatives considered**: Bloqueo pesimista (`SELECT ... FOR UPDATE`) sobre la cuenta destino
específicamente (descartado: introduciría dos estrategias de concurrencia distintas — optimista
para origen, pesimista para destino — sin necesidad demostrada, ya que el mecanismo optimista
existente ya protege ambos casos).

## 3. Lookup de la cuenta destino por número de cuenta

**Decision**: Añadir `IAccountRepository.GetByNumberAsync(AccountNumber, CancellationToken) ->
Account?`, sin restricción de propietario (a diferencia de `GetByIdForCustomerAsync`, que exige
`CustomerId`). Añadir también `IAccountRepository.GetByIdAsync(AccountId, CancellationToken) ->
Account?`, igualmente sin restricción de propietario, usado exclusivamente por el paso de
confirmación (§8) para revalidar una cuenta destino cuyo `AccountId` ya fue resuelto y firmado
durante la vista previa — nunca invocado con un `AccountId` provisto directamente por el cliente
HTTP.

Persistencia: agregar un índice único en `accounts.number` (`ux_accounts_number`), justificado
porque esta feature introduce por primera vez una consulta real por ese campo (antes solo se leía,
nunca se buscaba por él) y la unicidad del número de cuenta es una invariante de negocio real que
la base de datos debe reforzar como segunda línea de defensa (mismo criterio que el índice único
de `idempotency_key` en `002`).

**Rationale**: RF-009/FR-009 (clarificación 2026-09-25) exige que el cliente identifique el
destino mediante su número de cuenta completo, no un `AccountId` interno que un tercero no puede
conocer. Ampliar `IAccountRepository` (en vez de crear `IThirdPartyAccountRepository`) es
exactamente el ejemplo que cita la sección 19 del input: una operación nueva coherente con la
responsabilidad ya existente del repositorio (`Account`), no un concepto nuevo.

**Alternatives considered**: Exponer `IQueryable<Account>` desde el repositorio para que
Application construya su propio filtro (rechazado: viola explícitamente la sección 19 del input y
el patrón ya establecido en `001`/`002` de repositorios con métodos de intención explícita, no
genéricos).

## 4. Vista previa de terceros: mismo mecanismo de token firmado, sin cambio de forma

**Decision**: Reutilizar `TransferPreviewPayload` (mismos campos: `SourceAccountId`,
`DestinationAccountId`, `Amount`, `Currency`, `IssuedAtUtc`) y `IPreviewTokenSigner` sin ningún
cambio de forma. La vista previa a terceros resuelve el número de cuenta a un `AccountId` interno
**antes** de firmar el payload (§3), de modo que el payload firmado nunca necesita transportar un
número de cuenta ni ningún dato del destinatario — exactamente el mismo contrato interno que ya
usa la vista previa entre cuentas propias.

Cambio menor: renombrar el `Purpose` de `DataProtectionPreviewTokenSigner` de
`"BancaDigitalPeru.Transfers.OwnAccountTransferPreview.v1"` a
`"BancaDigitalPeru.Transfers.TransferPreview.v1"` (un solo literal de cadena), porque el mecanismo
ahora firma vistas previas de ambos tipos y el nombre anterior ya no describe correctamente su
alcance. No es un cambio de comportamiento observable ni de contrato HTTP.

**Rationale**: `TransferPreviewPayload` nunca asumió estructuralmente que ambas cuentas pertenecen
al mismo cliente — esa suposición vivía en qué método de repositorio se usaba para resolver el
destino (`GetByIdForCustomerAsync` vs `GetByNumberAsync`), no en la forma del payload. Reutilizarlo
tal cual evita duplicar el mecanismo criptográfico (sección 4 del input, "Money; débito; crédito;
... Transfer; persistencia de Transfer" listados como comunes — la vista previa firmada es
igualmente común aunque el input no la liste explícitamente por nombre).

**Alternatives considered**: Payload separado `ThirdPartyTransferPreviewPayload` con los mismos
campos (rechazado: duplicaría un tipo idéntico solo por el nombre de la carpeta que lo contiene,
sin ninguna diferencia real de datos — Principio I).

## 5. Modelado own-account vs third-party: sin flag, inferido por propiedad — y su impacto en el diseño de casos de uso

**Decision**: Ningún caso de uso ni contrato acepta un campo `transferType`. La naturaleza de la
operación se determina comparando el propietario real de la cuenta destino contra el cliente
actual, en el momento en que ambas cuentas ya están cargadas:

```text
destination.CustomerId == currentCustomerId  -> transferencia entre cuentas propias (002)
destination.CustomerId != currentCustomerId  -> transferencia a terceros (003)
```

Esta comparación es **estable**: la propiedad de una cuenta nunca cambia en este sistema (no existe
operación de "transferir titularidad"), por lo que una vista previa firmada por el flujo de
terceros seguirá siendo, en el momento de confirmar, una operación de terceros, y viceversa — no
hay ventana en la que la clasificación pueda "cambiar" entre vista previa y confirmación.

Esto permite una división de responsabilidades más precisa que "una clase por feature":

| Paso | Own-account (002) | Third-party (003) | Relación |
|---|---|---|---|
| Vista previa | `PreviewOwnAccountTransferUseCase` (sin cambios) | `PreviewThirdPartyTransferUseCase` (nuevo) | **Separados**: el *request* difiere de forma irreducible (`destinationAccountId` vs `destinationAccountNumber`), igual que difiere el método de repositorio usado para resolver el destino (§3). Forzarlos a una sola clase mezclaría dos modelos de privacidad distintos (§6) dentro de una rama condicional, violando cohesión. |
| Confirmación | *(antes: `ConfirmOwnAccountTransferUseCase`)* | *(antes: no existía)* | **Unificados** en `ConfirmTransferUseCase` (renombra y generaliza la clase de 002). El *request* (`previewReference` + `Idempotency-Key`) es idéntico en ambos casos; la clasificación own-vs-third-party se recalcula dinámicamente cargando el destino sin restricción de propietario (`GetByIdAsync`, §3) y comparando su `CustomerId`. Cada rama aplica exactamente las mismas reglas que su vista previa original ya validó (ver tabla de reglas en §6 más abajo). |
| Consulta de resultado | *(antes: `GetOwnAccountTransferUseCase`)* | *(antes: no existía)* | **Unificados** en `GetTransferUseCase` (renombra y generaliza). `Transfer.CustomerId` ya identifica al cliente ordenante en ambos casos (ver data-model.md); la única diferencia observable es si se agrega el nombre enmascarado del destinatario a la respuesta, decidido comparando `destinationAccount.CustomerId` contra `transfer.CustomerId` — el mismo criterio, aplicado una sola vez dentro del caso de uso compartido. |

**Rationale**: La sección 8 del input de planificación es explícita: la relación se infiere de la
propiedad real de las cuentas, no de un flag. Unificar confirmación y consulta (donde el *request*
es realmente idéntico) evita crear `ConfirmThirdPartyTransferUseCase`/`GetThirdPartyTransferUseCase`
como clases casi-copia de las de `002` — exactamente el antipatrón que la sección 4 del input
prohíbe. Mantener la vista previa separada (donde el *request* difiere estructuralmente) evita el
antipatrón opuesto: una sola clase con una rama `if (esTercero)` que mezcla dos contratos y dos
políticas de privacidad distintas en un solo método, lo cual sería peor para mantenibilidad que dos
clases pequeñas y cohesivas.

**Alternatives considered**:
- Cuatro clases separadas (Preview/Confirm/Get × Own/ThirdParty) sin ninguna unificación
  (rechazada: duplica íntegramente la lógica de confirmación y consulta, que es idéntica salvo por
  una comparación de propiedad — viola la sección 4 del input y el Principio I).
- Una única clase para los tres pasos (Preview/Confirm/Get) con ramas internas own-vs-third-party
  en los tres (rechazada: el *request* de la vista previa difiere de forma irreducible — ver tabla
  arriba —, por lo que unificar ese paso específico no elimina duplicación real, solo la traslada a
  una rama condicional dentro de una clase más grande y más difícil de probar de forma aislada).

## 6. Reglas comunes vs. reglas propias de cada tipo de transferencia

**Decision**: Extraer las validaciones verdaderamente comunes a una función compartida nueva,
`CommonTransferValidation` (Application), que cubre exclusivamente lo que depende únicamente de la
cuenta origen y el importe — nunca de la cuenta destino:

| Regla | Dónde vive | Aplica a |
|---|---|---|
| Cuenta origen debe existir y pertenecer al cliente actual | `IAccountRepository.GetByIdForCustomerAsync` (ya lo hace, sin cambios) | Ambas |
| Cuenta origen debe estar ACTIVA | `CommonTransferValidation` (nuevo) | Ambas |
| Importe > 0 | `CommonTransferValidation` (nuevo) | Ambas |
| Saldo suficiente en origen | `CommonTransferValidation` (nuevo) | Ambas |
| Débito/crédito preservan a exactitud el importe; conservación del dinero | `Account.Debit`/`Credit` (Domain, sin cambios) | Ambas |
| Atomicidad, idempotencia, concurrencia, trazabilidad | Ver §0-§2 (sin cambios) | Ambas |
| Cuentas origen y destino deben ser diferentes | `OwnAccountTransferValidation` (ya existe; para terceros es una garantía estructural automática — ver más abajo) | Own: explícita. Third-party: implícita |
| Cuenta destino debe estar ACTIVA | `OwnAccountTransferValidation` (existe) / `ThirdPartyTransferValidation` (nuevo) | Ambas, pero cada una la aplica en su propia validación específica |
| Cuenta destino debe pertenecer al **mismo** cliente | `OwnAccountTransferValidation` (existe, vía `GetByIdForCustomerAsync`) | Solo Own |
| Respuesta indistinguible ante cuenta destino ajena/inexistente | `OwnAccountTransferValidation` (existe) | Solo Own (FR-022 de 002) |
| Cuenta destino debe pertenecer a un cliente **distinto** | `ThirdPartyTransferValidation` (nuevo) | Solo Third-party |
| Cuenta destino inexistente → rechazo específico y revelador (no indistinguible) | `ThirdPartyTransferValidation` (nuevo) | Solo Third-party (FR-024 de 003 — ver §6 de la spec 003, Assumptions) |
| Cuenta destino resulta ser una cuenta propia del ordenante → rechazo específico | `ThirdPartyTransferValidation` (nuevo) | Solo Third-party (FR-006 de 003) |
| Privacidad del destinatario (qué se muestra) | `Customer.DisplayNameMasked` (Domain, nuevo) + decisión de "cuándo incluirlo" en `GetTransferUseCase`/`ConfirmTransferUseCase`/`PreviewThirdPartyTransferUseCase` | Solo Third-party |

`OwnAccountTransferValidation` se refactoriza para delegar en `CommonTransferValidation` para las
reglas de la cuenta origen, y mantiene únicamente sus reglas propias (mismo cliente en ambas
cuentas, cuentas distintas). `ThirdPartyTransferValidation` (nuevo) hace lo mismo desde el otro
lado. Ninguna de las dos duplica las reglas de origen.

**Rationale**: Esto es exactamente la distinción COMMON / OWN-ACCOUNT / THIRD-PARTY que pide la
sección 4 y la sección 5 del input de planificación, aplicada a nivel de código (no solo de
documentación). El refactor de `OwnAccountTransferValidation` es deliberado, acotado (una función
extraída, sin cambio de comportamiento observable para `002`) y necesario para que la regla de
"saldo suficiente" exista en un único lugar en vez de dos copias divergentes.

**Alternatives considered**: Dejar `OwnAccountTransferValidation` intacta y duplicar sus 3 primeras
comprobaciones (origen existe/activo/saldo) dentro de una nueva `ThirdPartyTransferValidation`
independiente (rechazada: es exactamente la duplicación de "COMMON TRANSFER RULES" que la sección
4 del input prohíbe explícitamente).

## 7. Status codes para destino inválido/no disponible (a diferencia de `002`)

**Decision**: A diferencia de `002` (donde origen y destino comparten el mismo criterio de
indistinguibilidad porque ambas cuentas deben pertenecer al mismo cliente), en `003` la cuenta
destino **no** recibe el mismo tratamiento de privacidad que la cuenta origen, porque revelar que
un número de cuenta corresponde a una cuenta válida de un tercero es precisamente el propósito de
la feature (HU3 de la spec: "verificar que la cuenta destino corresponde a una cuenta válida").
Mapeo de outcomes nuevo para el destino:

| Outcome | HTTP | Motivo |
|---|---|---|
| Cuenta destino no existe | 404 | `DestinationAccountNotFound` (nuevo) — revelador por diseño, no genérico |
| Cuenta destino resulta ser propia del ordenante | 422 | `DestinationIsOwnAccount` (nuevo) — no revela nada que el cliente no supiera ya (es su propia cuenta) |
| Cuenta destino BLOQUEADA | 422 | Reutiliza `AccountBlocked` (ya existe, mismo valor usado para origen bloqueada) |

La cuenta **origen**, en cambio, conserva exactamente el criterio de `002`: inexistente y ajena
producen la misma respuesta 404 genérica (`AccountNotEligible`, reutilizado sin cambios).

**Rationale**: Spec `003`, Assumptions: "la regla de respuesta indistinguible... se aplica
únicamente a la cuenta origen... 'cuenta destino inexistente' y 'cuenta destino resulta ser una
cuenta propia' sí pueden distinguirse entre sí, porque ninguna de las dos revela información de un
tercero". Este research.md traduce esa decisión de spec a dos nuevos valores de
`TransferRejectionReason` y su mapeo HTTP, en vez de forzarlos dentro de `AccountNotEligible`
(que seguiría comunicando semántica de privacidad que aquí no aplica).

**Alternatives considered**: Reutilizar `AccountNotEligible` (404) también para "destino
inexistente" en terceros (rechazada: mezclaría dos semánticas HTTP distintas bajo el mismo
`ProblemDetails.type` — uno genuinamente oculta información, el otro la revela deliberadamente —
dificultando que un consumidor de la API distinga los casos si algún día importara).

## 8. ¿`Transfer` necesita cambios de modelo?

**Decision**: No. `Transfer.CustomerId` ya se documentaba en `002` como "el propietario — ambas
cuentas pertenecen al mismo cliente por diseño" (data-model.md de 002); esa frase describía una
coincidencia de `002`, no una restricción estructural del campo. El campo en realidad siempre
significó "el cliente ordenante" (el propietario de `SourceAccountId`), lo cual sigue siendo
exactamente correcto para una transferencia a terceros, sin ningún cambio de columna, índice ni
migración sobre la tabla `transfers`. `GetByIdForCustomerAsync(customerId, transferId)` (ya
existente en `ITransferRepository`) sigue filtrando correctamente por "transferencias que
inicié", sin exponerlas al cliente destinatario (que no tiene ninguna vía de consulta definida en
ninguna de las dos specs — mínimo privilegio, Principio VI).

No se introduce un campo `TransferType`: si en algún momento una respuesta necesita distinguir el
tipo, se deriva comparando el propietario de `DestinationAccountId` contra `CustomerId` en el
momento de la consulta (§5), nunca almacenado.

**Rationale**: Sección 17 del input de planificación, explícita: evitar un `TransferType`
puramente decorativo si puede derivarse de la propiedad. Documentar aquí que el campo
`CustomerId` de `002` no necesitaba renombrarse ni reinterpretarse formalmente evita una migración
innecesaria (Principio I) y dejar constancia de que esto fue analizado, no pasado por alto.

**Alternatives considered**: Agregar `TransferType { OwnAccount, ThirdParty }` persistido
(rechazada explícitamente por la sección 17 del input); agregar un segundo `CustomerId` para el
destinatario (rechazada: no se necesita para ninguna consulta — el destinatario nunca consulta sus
transferencias recibidas en el alcance de esta spec — y `DestinationAccountId` ya permite
resolver el propietario actual del destino cuando se necesita, sin duplicar el dato).

## 9. ¿Se necesita algún cambio de schema?

**Decision**: Sí, exactamente dos, ambos aditivos y acotados a la tabla `accounts` (ninguno a
`transfers`):

1. Índice único `ux_accounts_number` sobre `accounts.number` (§3) — nueva necesidad de consulta
   real introducida por esta feature.
2. Actualización de datos (`UpdateData`, no de esquema) del `display_name` de los clientes
   ficticios `Cliente A`/`Cliente B` de "Cliente A (ficticio)"/"Cliente B (ficticio)" a nombres
   ficticios completos (p. ej. "María López Torres" / "Juan Pérez García") — necesario porque el
   algoritmo de enmascaramiento de FR-011 (primer nombre + inicial de apellido) requiere un
   nombre con al menos dos tokens para producir un resultado representativo; el valor actual
   ("Cliente A (ficticio)") produciría un enmascarado confuso ("Cliente A***"). Aplicado en una
   migración **nueva** (`AddThirdPartyTransferSupport` o equivalente), nunca editando
   `InitialCreate` (ya aplicada) — mismo criterio que el ajuste de estado de la Cuenta B en `002`.

**Rationale**: Ambos cambios son mínimos, aditivos, y no afectan ninguna garantía ya probada de
`001`/`002`. El cambio de `display_name` no altera ningún requisito de `001` (que nunca expuso ese
campo en ningún contrato) ni de `002` (que nunca lo usó).

**Alternatives considered**: Sembrar un tercer cliente ficticio nuevo específicamente para el
demo de terceros en vez de reutilizar Cliente A/B (rechazada: Cliente A ya tiene saldo y cuentas
ACTIVAS reutilizables del seed de `002`, y Cliente B ya es un cliente ajeno preexistente — no hay
necesidad real de un tercer cliente, Principio I).

## 10. Organización del contrato OpenAPI

**Decision**: `003` obtiene su propio documento (`third-party-transfers-v1.yaml`), pero **solo**
para lo que es genuinamente nuevo: `POST /api/v1/third-party-transfer-previews`. Los endpoints
`POST /api/v1/transfers` y `GET /api/v1/transfers/{transferId}` **no se duplican**: siguen
definidos una sola vez en `own-account-transfers-v1.yaml` (actualizado de forma aditiva — ver
data-model.md — para documentar el campo opcional `destinationCustomerDisplayName` en
`TransferResult`, presente solo cuando la transferencia es a un tercero), porque ahora sirven a
ambos flujos con el mismo contrato HTTP (§5).

**Rationale**: Cumple literalmente la preferencia de la sección 11 del input ("un recurso
conceptual coherente `/transfers`... evitar duplicar innecesariamente contratos para recursos
equivalentes"). El campo nuevo es compatible hacia atrás: FR-018 de `002` exige "conocer **al
menos**" los campos ya listados, por lo que agregar un campo opcional adicional no contradice ni
reinterpreta ningún requisito ya aprobado de `002` (Principio II) — es una evolución aditiva del
mismo recurso, exactamente lo que pide la sección 32 del input ("preferir evolución compatible del
recurso Transfer en lugar de crear una API paralela").

**Alternatives considered**: Documento único para toda la familia `/transfers` fusionando `002` y
`003` en un solo archivo (rechazada: perdería la separación por capacidad de negocio ya
justificada y aceptada en el plan de `002`, y obligaría a reescribir un contrato ya aprobado en vez
de evolucionarlo aditivamente); duplicar `POST /transfers`/`GET /transfers/{id}` en el nuevo
documento de `003` únicamente para "completitud" (rechazada: crea dos fuentes de verdad para el
mismo endpoint físico, con riesgo real de que diverjan con el tiempo).
