# Research: Transferencias entre cuentas propias

**Feature**: `002-transferencias-cuentas-propias` | **Date**: 2026-09-24

**Purpose**: Esta feature introduce la primera operación de escritura financiera del proyecto.
El input de planificación fija el stack (idéntico a `001`) y exige decisiones explícitas sobre
atomicidad, concurrencia, idempotencia y trazabilidad. Este documento las cierra todas antes de
Phase 1, reutilizando el código real ya existente de `001` (`BancaDigitalPeru.Domain/Application/
Infrastructure/Api`) en lugar de rediseñar desde cero.

## 0. Punto de partida: qué reutiliza esta feature de `001`

**Decision**: Reutilizar sin modificar su forma pública: `Account` (Domain, se le añade
comportamiento, no se reemplaza), `AccountId`, `CustomerId`, `Money`, `CurrencyCode`,
`ICurrentCustomerProvider`, `IUnitOfWork`/`UnitOfWork` (definidos en `001` pero **nunca
invocados** por ningún caso de uso hasta ahora — esta es su primera consumidora real),
`IAccountRepository` (se le añade un método, no se reemplaza), `BancaDigitalPeruDbContext`,
`AccountConfiguration`, `ApiProblemDetails`/`GlobalExceptionHandler`, y el patrón de
`AddApplication()`/`AddInfrastructure()`/`AddApiServices()` del Composition Root.

**Rationale**: Principio I (simplicidad) y la instrucción explícita del input de planificación de
no duplicar modelos o servicios ya existentes. `IUnitOfWork` fue diseñado en `001` precisamente
para esta situación (research.md de `001` §2, Complexity Tracking): "preparada para las futuras
features de transferencias, donde sí existirán operaciones de escritura".

**Alternatives considered**: crear un `TransferDbContext`/módulo de persistencia paralelo
(rechazado: fragmentaría la Unit of Work en dos fronteras transaccionales distintas, violando
directamente la sección 10 del input — "no distribuir la misma operación lógica entre varias Unit
of Work").

## 1. Frontera de agregados y coordinación

**Decision**: `Account` sigue siendo su propio Aggregate Root (ya lo era en `001`). `Transfer` es
un segundo Aggregate Root, independiente, que registra el resultado de una transferencia
**completada** (nunca una rechazada — ver Assumptions de spec.md). El caso de uso de Application
(`TransferBetweenOwnAccountsUseCase`) coordina ambos agregados dentro de una única
`IUnitOfWork.SaveChangesAsync()`.

**Rationale**: Convertir todas las cuentas de un cliente en un único agregado (p. ej. un
`CustomerAccountsAggregate`) impediría que `001` siguiera tratando cada `Account` como su propia
raíz consistente, y no protegería ninguna invariante adicional: la única invariante multi-cuenta
("las dos cuentas deben ser distintas y del mismo cliente") es una regla del *caso de uso*, no del
agregado — no requiere que ambas cuentas compartan una raíz común. Mantenerlas separadas respeta
Clean Architecture (Application coordina, Domain protege invariantes locales) y evita inventar un
agregado sin necesidad concreta (Principio I).

**Alternatives considered**: agregado compuesto `Customer -> Accounts[]` (rechazado: rompería el
diseño ya implementado de `001` sin necesidad, y no aporta ninguna protección que la coordinación
a nivel de caso de uso + transacción de base de datos no proporcione ya).

## 2. Comportamiento de dominio para el movimiento (evitar modelo anémico)

**Decision**: Añadir a la entidad `Account` (existente) dos métodos de comportamiento explícito:
`Debit(Money amount)` y `Credit(Money amount)`. Ambos verifican, como invariantes de Domain: misma
moneda que el saldo, importe > 0, la cuenta debe estar `Active` (para `Debit`, RF-005/FR-023; para
`Credit`, FR-006 clarificado — una cuenta `Blocked` no puede ser destino), y — solo en `Debit` —
que el saldo resultante no sea negativo. `Balance` pasa de `{ get; }` a `{ get; private set; }`
para permitir esta mutación controlada; ningún código fuera de `Account` puede escribir `Balance`
directamente.

**Rationale**: El input de planificación prohíbe explícitamente `account.Balance -= amount` desde
Application. Estas verificaciones ya fueron decididas por Application *antes* de invocar
`Debit`/`Credit` (ver §6), por lo que actúan como una segunda línea de defensa (invariantes reales
del dominio), no como el camino primario de rechazo — si llegaran a dispararse sería síntoma de un
error de programación en Application, no una respuesta HTTP legítima.

**Alternatives considered**: modelo anémico con un `AccountDomainService.Transfer(source,
destination, amount)` externo (rechazado explícitamente por la sección 4 del input: evitar
Domain Services sin responsabilidad real; el comportamiento pertenece naturalmente a `Account`).

## 3. Modelo `Transfer` y `TransferStatus`

**Decision**: `Transfer` solo se crea y persiste cuando la operación se completa con éxito. Por lo
tanto `TransferStatus` es un enum con un único miembro, `Completed`, documentado como
intencionalmente cerrado (mismo patrón que `AccountType.Savings` en `001`): una transferencia
rechazada nunca se convierte en una entidad `Transfer` (spec, Key Entities: "Vista previa... no es
una `Transferencia`"; Assumptions: "las transferencias rechazadas no requieren conservar un
historial propio consultable").

**Rationale**: Cumple RB10/FR-016..FR-018 (trazabilidad de lo completado) sin inventar estados
adicionales por conveniencia técnica (instrucción explícita de la sección 13 del input).

**Alternatives considered**: `TransferStatus { Completed, Rejected }` con persistencia de
intentos fallidos (rechazado: la spec asume explícitamente lo contrario — ver Assumptions —, y
persistir intentos rechazados no aporta ninguna garantía adicional que esta spec exija).

## 4. Vista previa: token firmado y sin persistencia (no un "TransferPreview" en base de datos)

**Decision**: El paso de vista previa (FR-010) **no se persiste**. El endpoint de vista previa
valida la operación (propiedad, cuentas distintas, moneda, estado ACTIVA de ambas cuentas, importe
válido, saldo suficiente) y devuelve una **referencia opaca y firmada** que codifica
`{sourceAccountId, destinationAccountId, amount, issuedAtUtc}`, generada con
`Microsoft.AspNetCore.DataProtection` (`IDataProtector`, ya incluido en el framework de ASP.NET
Core — sin paquete NuGet nuevo). El endpoint de confirmación (FR-011) decodifica esa referencia y
**vuelve a validar todo desde cero** contra el estado vigente en ese momento (FR-012): la
referencia es solo un portador de los tres datos identificados, nunca una fuente de verdad
autoritativa sobre saldos.

**Rationale**: La propia spec dice explícitamente que la vista previa "no reserva ni bloquea el
saldo" y "no vence explícitamente" (Assumptions), y que "no es una `Transferencia`: no tiene
efecto financiero y no queda registrada como movimiento". Una tabla `TransferPreview` en base de
datos añadiría una migración, una entidad y (eventualmente) una tarea de limpieza de filas
obsoletas, sin proteger ninguna garantía adicional — todo se revalida igualmente al confirmar. Un
token firmado y sin estado es la alternativa más simple que satisface FR-010/FR-011/FR-012
(Principio I) y es intrínsecamente compatible con múltiples instancias de la Api (sección 24 del
input: sin locks en memoria, sin estado servidor) siempre que las instancias compartan el mismo
key ring de Data Protection (ver Riesgos Técnicos, §9 más abajo).

**Alternatives considered**:
- Tabla `TransferPreview` persistida con expiración (rechazada: complejidad no justificada, ver
  arriba).
- Reenviar los mismos 3 campos crudos a la confirmación, sin ningún concepto de "referencia"
  (rechazada: FR-010 exige explícitamente que el sistema "entregue una referencia de vista
  previa", que quedó fijado durante `/speckit.clarify` como parte del contrato observable de la
  feature).

## 5. Idempotencia

**Decision**: El endpoint de confirmación (`POST /transfers`) exige un header HTTP
`Idempotency-Key` obligatorio (string opaco, 1–255 caracteres, validado únicamente en formato por
FluentValidation). `Transfer` persiste ese valor en una columna con **índice único** en
PostgreSQL. Flujo:

1. Antes de ejecutar débito/crédito, Application busca un `Transfer` existente con esa
   `IdempotencyKey` (vía `ITransferRepository.GetByIdempotencyKeyAsync`).
   - Si existe y sus `SourceAccountId`/`DestinationAccountId`/`Amount` coinciden exactamente con
     los de la vista previa decodificada → se devuelve el resultado de ese `Transfer` ya existente
     (200 OK, replay idempotente; RF-024/RF-025/CA10).
   - Si existe pero esos datos **no coinciden** → se rechaza con un conflicto de
     Idempotency-Key (409), sin tocar ningún saldo.
   - Si no existe → continúa el flujo normal.
2. Si dos solicitudes con la misma clave llegan **simultáneamente** y ambas superan el paso 1 (ninguna
   ve todavía la fila de la otra), ambas intentan debitar/acreditar y guardar. El índice único de
   PostgreSQL garantiza que solo un `INSERT` de `Transfer` tenga éxito; el otro falla con una
   violación de unicidad (`23505`). Como toda la operación (débito + crédito + inserción de
   `Transfer`) ocurre en una única llamada a `SaveChangesAsync` (una sola transacción implícita),
   la solicitud perdedora no persiste ningún cambio de saldo: EF Core descarta el batch completo.
   Esa solicitud perdedora recarga el `Transfer` real (ya insertado por la ganadora) y devuelve su
   resultado, exactamente como un replay idempotente.

**Rationale**: Satisface RB9/FR-021 (revisado del original RF21/RF22) sin locks en memoria ni
mecanismos que fallen al escalar horizontalmente (sección 24 del input). El mecanismo de
"verificar primero, restricción única como respaldo ante condiciones de carrera" es el patrón
estándar para idempotencia en APIs financieras y no requiere infraestructura adicional (Redis,
locks distribuidos) — la propia base de datos relacional ya ofrece la garantía necesaria.

**Alternatives considered**: caché en memoria de claves procesadas (rechazada explícitamente por
la sección 24: no sobrevive reinicios ni funciona con múltiples instancias); usar un almacén
externo tipo Redis solo para idempotencia (rechazada: no está justificada para el alcance
académico de esta feature — la restricción única de PostgreSQL ya es suficiente y evita una
dependencia nueva, sección 26 del input).

## 6. Concurrencia (optimistic concurrency con PostgreSQL)

**Decision**: Usar el token de concurrencia optimista nativo de PostgreSQL: la columna de sistema
`xmin`, mapeada por EF Core/Npgsql como concurrency token (`UseXminAsConcurrencyToken()` sobre la
configuración de `Account`), sin añadir ninguna columna nueva al esquema. Cuando dos solicitudes
leen la misma fila de `Account` y ambas intentan actualizarla, la segunda `UPDATE` (con un `xmin`
ya obsoleto en su cláusula `WHERE`) afecta 0 filas; EF Core lo detecta y lanza
`DbUpdateConcurrencyException`. El caso de uso captura esa excepción y la traduce en un resultado
de "conflicto de concurrencia" (409), **sin reintento automático**.

**Rationale**: Es la alternativa más simple que protege el escenario exigido por la sección 12 del
input (dos solicitudes de S/ 80.00 contra un saldo de S/ 100.00 no deben aprobarse ambas): al no
añadir columna ni migración manual, y al ser un mecanismo soportado de forma nativa por el
proveedor Npgsql, cumple "preferir Optimistic Concurrency para mantener la solución simple"
(sección 12) sin trabajo adicional de mantenimiento de una columna `RowVersion` propia. No se
reintenta automáticamente porque un reintento tendría que releer el saldo actual y volver a
validar íntegramente la operación — equivalente a que el cliente reenvíe la misma solicitud
(mismo `Idempotency-Key`), lo cual ya está cubierto por el mecanismo de idempotencia; introducir
un reintento *dentro* del mismo request añadiría complejidad sin una necesidad demostrada (la
sección 12 exige explícitamente no reintentar sin demostrar que se respeta idempotencia, y no
demostrarlo aquí es más simple y igual de correcto — Principio I).

**Alternatives considered**: columna `RowVersion`/`byte[]` explícita gestionada por la aplicación
(rechazada: requiere una migración y mantenimiento propio que `xmin` ya resuelve de forma nativa);
bloqueo pesimista (`SELECT ... FOR UPDATE`) (rechazada: mantiene una transacción abierta más tiempo
y no se ajusta mejor al escenario descrito; optimista es explícitamente lo preferido por el input).

## 7. Estrategia transaccional

**Decision**: Todo el efecto financiero de una confirmación exitosa (débito en origen, crédito en
destino, inserción de `Transfer`) se envía en una única llamada a
`BancaDigitalPeruDbContext.SaveChangesAsync()`, dentro de la misma `UnitOfWork` ya existente de
`001`. EF Core envuelve automáticamente todos los cambios rastreados de una llamada a
`SaveChangesAsync` en una única transacción de base de datos; no se abre una transacción explícita
adicional.

**Rationale**: Cumple RF-015/RB6 (atomicidad observable) de la forma más simple posible: si
cualquier parte falla (violación de unicidad, conflicto de concurrencia, error inesperado), *nada*
se persiste — ni el débito, ni el crédito, ni el registro de `Transfer` — porque es una sola
operación de base de datos. Esto también resuelve el "escenario de fallo deliberado" exigido por
la sección 11: forzar una excepción justo antes de `SaveChangesAsync` (p. ej. en una prueba de
integración) demuestra que ambos saldos permanecen en su valor anterior.

**Alternatives considered**: transacción explícita (`BeginTransaction`/`Commit`) envolviendo
múltiples llamadas a `SaveChangesAsync` (rechazada: el input prohíbe exponer estos conceptos a
Application/Domain salvo necesidad estrictamente demostrada, y una sola llamada ya es atómica sin
ellos).

## 8. Mapeo de outcomes a códigos HTTP

| Outcome | HTTP | Cuerpo |
|---|---|---|
| Transferencia confirmada (nueva) | 201 Created | `TransferResult` |
| Replay idempotente (misma clave, mismos datos) | 200 OK | `TransferResult` (el ya existente) |
| Formato inválido (GUID, importe con precisión inválida, `Idempotency-Key` ausente/mal formada, referencia de vista previa no decodificable) | 400 | `ProblemDetails` genérico "Solicitud inválida" |
| Cuenta origen o destino inexistente, o perteneciente a otro cliente | 404 | `ProblemDetails` genérico idéntico en ambos casos (research.md aplica el mismo criterio de FR-022 simétricamente a origen y destino — spec solo lo pidió explícito para destino, pero la misma razón de privacidad aplica igual al origen) |
| Misma cuenta como origen y destino, cuenta origen o destino BLOQUEADA, importe ≤ 0, saldo insuficiente | 422 | `ProblemDetails` con `detail` específico por caso (no son datos sensibles: el cliente ya conoce el estado de sus propias cuentas) |
| `Idempotency-Key` reutilizada con datos distintos | 409 | `ProblemDetails` "conflicto de idempotencia" |
| Conflicto de concurrencia optimista | 409 | `ProblemDetails` "conflicto de concurrencia, intente nuevamente" |
| Error inesperado | 500 | `ProblemDetails` genérico (mismo `GlobalExceptionHandler` de `001`, sin cambios) |

**Rationale**: separa claramente lo que es sensible a privacidad (404 uniforme) de lo que es una
regla de negocio ordinaria sobre las propias cuentas del cliente (422 con detalle específico),
siguiendo el precedente de seguridad de `001` solo donde corresponde.

## 9. Escalabilidad horizontal

**Decision**: La Api permanece sin estado en memoria. El único elemento que requiere coordinación
entre instancias si se despliega con más de una réplica es el *key ring* de Data Protection usado
para firmar las referencias de vista previa (§4): por defecto, ASP.NET Core persiste las claves en
el sistema de archivos local, lo cual **no** es compatible con múltiples instancias sin un
almacén compartido (p. ej. un volumen compartido o una tabla de base de datos vía
`PersistKeysToDbContext`). Para el alcance académico de esta feature (una sola instancia de
desarrollo/demo), se documenta como limitación conocida en Technical Risks, no como bloqueante.

**Rationale**: Resolver un almacén de claves distribuido para Data Protection es una
preocupación operativa de despliegue, no una capacidad funcional exigida por la spec; añadirla
ahora sería complejidad no justificada por una necesidad concreta del alcance actual (Principio
I). Se deja documentada para no sorprender a un futuro despliegue multi-instancia.

**Alternatives considered**: evitar Data Protection por completo para no tener esta limitación
(rechazada: ya se evaluó en §4 que la alternativa — persistir la vista previa — es más compleja
sin aportar más garantías).

## 10. Pruebas con PostgreSQL real

**Decision**: Reutilizar la infraestructura de `IntegrationTests` ya existente de `001`
(`PostgresContainerFixture` con Testcontainers.PostgreSql, `BankingApiFactory`). Las pruebas de
concurrencia (§ sección 23 del input) se implementan dañando dos `DbContext` distintos apuntando
al mismo contenedor, ejecutando ambas operaciones en paralelo (`Task.WhenAll`) contra la misma
cuenta origen, y verificando que como máximo una tenga éxito y que el saldo final sea consistente
con exactamente una transferencia aplicada.

**Rationale**: No se introduce ninguna herramienta de pruebas de carga/concurrencia adicional; dos
`DbContext` reales contra el mismo Postgres ya reproducen fielmente una condición de carrera real,
sin necesidad de infraestructura de test adicional (Principio I).

**Alternatives considered**: herramientas de testing de concurrencia dedicadas (rechazadas: no
justificadas para el alcance de esta feature).
