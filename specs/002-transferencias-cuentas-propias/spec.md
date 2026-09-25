# Feature Specification: Transferencias entre cuentas propias

**Feature Branch**: `002-transferencias-cuentas-propias`

**Created**: 2026-09-24

**Status**: Approved

**Input**: User description: "Transferencias de dinero entre cuentas de ahorro que pertenecen al
mismo cliente. El cliente selecciona una cuenta origen y una cuenta destino, ambas propias, e
indica un importe en PEN. La operación debe preservar la integridad de los saldos (débito en
origen, crédito idéntico en destino), ser atómica, generar un identificador de operación con
fecha/hora, y no debe aplicar el mismo movimiento dos veces ante una solicitud repetida.
Continúa sin autenticación, sobre el cliente ficticio ya definido en la spec 001."

## Clarifications

### Session 2026-09-24

- Q: ¿La transferencia entre cuentas propias necesita un paso explícito de "vista previa" separado de la ejecución final, o basta con que una única solicitud incluya cuenta origen, cuenta destino e importe y se ejecute directamente? → A: Flujo en dos pasos: un paso de vista previa que valida y muestra los datos, y un paso separado de confirmación que recién ejecuta.
- Q: ¿La respuesta ante una cuenta destino inexistente debe ser indistinguible de la respuesta ante una cuenta destino que pertenece a otro cliente, o puede indicar específicamente que la cuenta destino no es válida? → A: Respuesta genérica idéntica en ambos casos, sin revelar si la cuenta destino existe (mismo criterio que la spec 001).
- Q: ¿Una cuenta destino BLOQUEADA puede recibir una transferencia entre cuentas propias, o debe rechazarse igual que si fuera cuenta origen? → A: Se rechaza igual que el origen: ninguna cuenta BLOQUEADA participa en una transferencia, ni como origen ni como destino.
- Q: ¿La regla de respuesta indistinguible de FR-022 (cuenta destino ajena/inexistente) debe aplicar simétricamente a la cuenta origen ajena/inexistente, o solo a la cuenta destino? → A: Aplica simétricamente a ambas: ninguna respuesta debe permitir distinguir "no existe" de "es de otro cliente", ni para origen ni para destino (mismo criterio de privacidad, detectado durante /speckit.plan).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Transferir dinero entre mis cuentas propias (Priority: P1)

Como cliente, quiero indicar cuenta origen, cuenta destino e importe, ver una vista previa de la
operación y luego confirmarla, para mover dinero entre mis propios productos con la certeza de lo
que voy a ejecutar.

**Why this priority**: Es la razón de ser de esta feature; sin una transferencia exitosa no hay
ningún valor de negocio que entregar. Es el incremento mínimo demostrable.

**Independent Test**: Con el Cliente A sembrado con la Cuenta A (ACTIVA, S/ 2,500.00) y la Cuenta
B (ACTIVA, S/ 800.00), solicitar una vista previa de S/ 300.00 de A hacia B, confirmarla, y
verificar que A queda en S/ 2,200.00 y B en S/ 1,100.00, sin necesidad de que existan rechazos ni
duplicados para que este flujo tenga sentido de uso completo.

**Acceptance Scenarios**:

1. **Given** que el cliente posee la Cuenta A (ACTIVA, S/ 2,500.00) y la Cuenta B (ACTIVA,
   S/ 800.00), **When** solicita una vista previa de S/ 300.00 desde la Cuenta A hacia la Cuenta
   B, **Then** el sistema identifica cuenta origen, cuenta destino e importe y entrega una
   referencia de vista previa, sin modificar ningún saldo todavía.
2. **Given** la vista previa del escenario anterior, **When** el cliente la confirma, **Then** la
   transferencia se completa, la Cuenta A queda en S/ 2,200.00 y la Cuenta B en S/ 1,100.00.
3. **Given** el escenario anterior, **When** se completa la transferencia, **Then** el importe
   debitado de la cuenta origen es exactamente igual al acreditado en la cuenta destino y la suma
   total de ambos saldos no cambia.
4. **Given** que la Cuenta A tiene S/ 500.00 disponibles, **When** el cliente solicita una vista
   previa y confirma una transferencia por el total de S/ 500.00 hacia otra cuenta propia,
   **Then** la operación se completa y la Cuenta A queda en S/ 0.00.
5. **Given** que el cliente solicitó una vista previa válida y, antes de confirmarla, el saldo de
   la cuenta origen cambió de forma que el importe ya no es cubierto, **When** el cliente confirma
   esa vista previa, **Then** la confirmación es rechazada en ese momento y ningún saldo se
   modifica.

---

### User Story 2 - Rechazar transferencias inválidas (Priority: P1)

Como cliente, quiero que cualquier intento de transferencia inválido (saldo insuficiente, importe
cero o negativo, misma cuenta como origen y destino, cuenta origen o destino bloqueada, cuenta
origen o destino ajena) sea rechazado sin alterar ningún saldo, para que mi dinero esté protegido
frente a operaciones erróneas o no autorizadas.

**Why this priority**: Sin estas validaciones, User Story 1 por sí sola permitiría mover dinero de
forma incorrecta o insegura; es tan crítica como el camino feliz (Principio V/VI de la
constitución).

**Independent Test**: Con las mismas cuentas de referencia, se puede verificar de forma aislada
que cada intento inválido (saldo insuficiente, importe ≤ 0, misma cuenta, cuenta origen o destino
bloqueada, cuenta origen o destino ajena) es rechazado y que los saldos de ambas cuentas quedan
exactamente igual que antes del intento.

**Acceptance Scenarios**:

1. **Given** que la cuenta origen posee S/ 100.00 disponibles, **When** el cliente intenta
   transferir S/ 150.00, **Then** la operación es rechazada y los saldos de ambas cuentas
   permanecen sin cambios.
2. **Given** que existen dos cuentas propias activas, **When** el cliente intenta transferir
   S/ 0.00 o un importe negativo, **Then** la operación es rechazada y ningún saldo cambia.
3. **Given** que el cliente selecciona una cuenta como origen, **When** intenta seleccionar la
   misma cuenta como destino, **Then** la transferencia no puede completarse.
4. **Given** que una cuenta propia está BLOQUEADA, **When** el cliente intenta utilizarla como
   cuenta origen, **Then** la transferencia es rechazada y ningún saldo cambia.
5. **Given** que una cuenta propia está BLOQUEADA, **When** el cliente intenta utilizarla como
   cuenta destino, **Then** la transferencia es rechazada y ningún saldo cambia (clarificación
   2026-09-24).
6. **Given** que una cuenta pertenece a otro cliente, **When** el cliente actual intenta
   utilizarla como origen, **Then** la operación es rechazada, ningún saldo es modificado, y la
   respuesta es idéntica a la que se obtendría si esa cuenta no existiera (clarificación
   2026-09-24).
7. **Given** que la cuenta destino pertenece a otro cliente, **When** se intenta ejecutar la
   operación mediante esta funcionalidad, **Then** no se procesa como transferencia entre cuentas
   propias, ningún saldo es modificado, y la respuesta es idéntica a la que se obtendría si la
   cuenta destino no existiera (clarificación 2026-09-24).

---

### User Story 3 - Evitar transferencias duplicadas (Priority: P1)

Como cliente, quiero que reenviar por error la misma solicitud de transferencia no aplique el
movimiento de dinero más de una vez, para no sufrir débitos duplicados.

**Why this priority**: Es una garantía de integridad financiera no negociable (Principio V de la
constitución); sin ella, un simple reintento de red podría duplicar un débito real.

**Independent Test**: Con una transferencia de S/ 300.00 ya completada, reenviar la misma
solicitud lógica y verificar que no se produce un segundo débito ni un segundo crédito.

**Acceptance Scenarios**:

1. **Given** que una transferencia de S/ 300.00 ya fue completada correctamente, **When** la
   misma solicitud lógica vuelve a recibirse, **Then** no se realiza un segundo débito de
   S/ 300.00 ni un segundo crédito de S/ 300.00.

---

### User Story 4 - Consultar el resultado de una transferencia (Priority: P2)

Como cliente, quiero poder consultar el resultado de una transferencia que realicé (identificador
de operación, fecha y hora, cuentas involucradas, importe y resultado), para verificar que la
información financiera mostrada es correcta.

**Why this priority**: Complementa User Story 1 permitiendo revisar una operación ya realizada;
aporta valor adicional pero depende de que exista al menos una transferencia completada.

**Independent Test**: Con una transferencia ya completada y su identificador de operación
conocido, se puede consultar su resultado de forma directa y verificar que expone los 6 datos
mínimos requeridos.

**Acceptance Scenarios**:

1. **Given** que una transferencia se completa correctamente, **When** el cliente consulta su
   resultado, **Then** puede identificar el número de operación, fecha y hora, cuenta origen y
   destino enmascaradas, importe y resultado de la operación.
2. **Given** que una transferencia fue completada correctamente, **When** la aplicación se cierra
   y posteriormente vuelve a utilizarse, **Then** los saldos actualizados continúan disponibles y
   la transferencia realizada conserva su información básica.

---

### Edge Cases

- **Importe con más de dos decimales**: la operación no debe ejecutarse con un importe cuya
  precisión no sea válida para esta versión (rechazo, ningún saldo cambia).
- **Cuenta origen inexistente**: la operación se rechaza sin modificar ningún saldo, con la misma
  respuesta que si la cuenta origen perteneciera a otro cliente (FR-021).
- **Cuenta destino inexistente**: la operación se rechaza sin modificar ningún saldo, con la misma
  respuesta que si la cuenta destino perteneciera a otro cliente (FR-022).
- **Saldo cero**: una cuenta con saldo S/ 0.00 no puede realizar una transferencia por un importe
  positivo.
- **Solicitud repetida**: reenviar la misma operación no genera movimientos financieros
  adicionales (ver User Story 3).
- **Error durante la operación**: si la transferencia no puede completarse íntegramente, ambos
  saldos deben conservar los valores que tenían antes de iniciarla (no se observa un estado a
  medio aplicar).
- **Vista previa desactualizada al confirmar**: si las condiciones que hicieron válida una vista
  previa (saldo, estado de la cuenta) cambiaron antes de que el cliente la confirme, la
  confirmación se rechaza en ese momento sin modificar ningún saldo (ver FR-012).

## Requirements *(mandatory)*

### Functional Requirements

**Selección de cuentas**

- **FR-001**: El sistema DEBE permitir seleccionar como cuenta origen únicamente una cuenta
  perteneciente al cliente actual.
- **FR-002**: El sistema DEBE permitir seleccionar como cuenta destino únicamente otra cuenta
  perteneciente al mismo cliente.
- **FR-003**: La cuenta origen y la cuenta destino NO DEBEN ser la misma cuenta.
- **FR-004**: Ambas cuentas DEBEN ser cuentas de ahorro denominadas en Soles peruanos (PEN).
- **FR-005**: La cuenta origen DEBE encontrarse en estado ACTIVA para poder iniciar una
  transferencia.
- **FR-006**: La cuenta destino DEBE encontrarse en estado ACTIVA para poder recibir una
  transferencia; si está BLOQUEADA, la operación DEBE rechazarse igual que si la cuenta BLOQUEADA
  fuera la cuenta origen (clarificación 2026-09-24).

**Importe**

- **FR-007**: El cliente DEBE indicar un importe mayor que S/ 0.00.
- **FR-008**: El importe de la transferencia DEBE expresarse en PEN y admitir como máximo dos
  posiciones decimales.
- **FR-009**: El importe NO DEBE superar el saldo disponible de la cuenta origen.

**Ejecución**

- **FR-010**: El sistema DEBE proveer un paso de vista previa que, dados cuenta origen, cuenta
  destino e importe, valide las condiciones de la operación (pertenencia de ambas cuentas,
  cuentas distintas, moneda, estado de la cuenta origen, importe válido y saldo suficiente) y
  entregue una referencia de vista previa junto con los tres datos identificados, sin modificar
  ningún saldo (clarificación 2026-09-24).
- **FR-011**: El sistema DEBE proveer un paso de confirmación separado que, a partir de una
  referencia de vista previa, ejecute la transferencia.
- **FR-012**: Si, al momento de confirmar, las condiciones que hicieron válida la vista previa ya
  no se cumplen (p. ej. el saldo disponible cambió o la cuenta pasó a BLOQUEADA), la confirmación
  DEBE rechazarse en ese momento sin modificar ningún saldo.
- **FR-013**: Una transferencia confirmada válidamente DEBE disminuir el saldo disponible de la
  cuenta origen exactamente por el importe transferido.
- **FR-014**: La misma transferencia DEBE aumentar el saldo disponible de la cuenta destino
  exactamente por el mismo importe.
- **FR-015**: El débito y el crédito DEBEN formar parte de una única operación lógica: la
  confirmación NO DEBE dejar una sola de las cuentas modificada si la operación no puede
  completarse correctamente.
- **FR-016**: Una confirmación exitosa DEBE generar un identificador único de operación.
- **FR-017**: Una confirmación exitosa DEBE registrar la fecha y hora en que fue realizada.
- **FR-018**: Después de confirmar una transferencia, el sistema DEBE permitir conocer al menos
  identificador de operación, fecha y hora, cuenta origen enmascarada, cuenta destino
  enmascarada, importe y resultado de la operación.

**Rechazos**

- **FR-019**: Si el saldo disponible de la cuenta origen es insuficiente para el importe
  solicitado, la operación (vista previa o confirmación) DEBE rechazarse y ningún saldo DEBE
  modificarse.
- **FR-020**: Si el importe es cero o negativo, la operación DEBE ser rechazada y ningún saldo
  DEBE modificarse.
- **FR-021**: Si la cuenta origen no pertenece al cliente actual o no existe, la operación DEBE
  ser rechazada, y la respuesta DEBE ser la misma en ambos casos (idéntica a la de cualquier otro
  rechazo de esta funcionalidad), sin revelar si la cuenta origen existe (clarificación
  2026-09-24, mismo criterio de privacidad que FR-022).
- **FR-022**: Si la cuenta destino no pertenece al cliente actual o no existe, esta funcionalidad
  NO DEBE procesar la operación como transferencia entre cuentas propias, y la respuesta DEBE ser
  la misma en ambos casos (idéntica a la de cualquier otro rechazo de esta funcionalidad), sin
  revelar si la cuenta destino existe (clarificación 2026-09-24, mismo criterio que FR-022 de la
  spec 001).
- **FR-023**: Si la cuenta origen está BLOQUEADA, la operación DEBE ser rechazada y ningún saldo
  DEBE modificarse.

**Duplicidad**

- **FR-024**: Una misma solicitud lógica de confirmación NO DEBE producir más de un movimiento
  financiero.
- **FR-025**: Si una confirmación ya procesada correctamente se recibe nuevamente como la misma
  operación, los saldos NO DEBEN modificarse una segunda vez.

**Persistencia observable**

- **FR-026**: Después de una transferencia exitosa, los nuevos saldos DEBEN permanecer
  disponibles cuando la aplicación sea cerrada y utilizada nuevamente.
- **FR-027**: La información necesaria para identificar posteriormente la transferencia realizada
  DEBE conservarse.

### Key Entities *(include if feature involves data)*

- **Transferencia (entre cuentas propias)**: registro de un movimiento de dinero completado entre
  dos cuentas del mismo cliente. Atributos relevantes: identificador único de operación, fecha y
  hora, cuenta origen (referencia a `Account` de la spec 001), cuenta destino (referencia a
  `Account`), importe (PEN, 2 decimales), resultado. No se modela un estado "pendiente"
  prolongado (Assumptions): el resultado observable es completada o rechazada.
- **Vista previa de transferencia**: representación transitoria de una solicitud de transferencia
  aún no confirmada (cuenta origen, cuenta destino, importe y una referencia para confirmarla).
  No es una `Transferencia`: no tiene efecto financiero y no queda registrada como movimiento;
  solo existe para permitir el paso de confirmación (FR-010/FR-011). No reserva saldo (ver
  Assumptions).
- **Cuenta de ahorro (`Account`)**: entidad ya definida en `001-consulta-productos-bancarios`;
  esta spec añade la operación de débito/crédito sobre su saldo, sujeta a las reglas de
  propiedad, estado y moneda ya establecidas allí.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de las transferencias completadas exitosamente preservan la suma total de
  los saldos de la cuenta origen y destino (conservación del dinero, sin comisiones).
- **SC-002**: El 0% de las transferencias rechazadas (saldo insuficiente, importe inválido, misma
  cuenta, cuenta origen o destino bloqueada, cuenta origen o destino ajena) modifica el saldo de
  alguna de las cuentas involucradas, y la respuesta ante una cuenta (origen o destino)
  inexistente es idéntica a la de esa misma cuenta perteneciendo a otro cliente (clarificación
  2026-09-24).
- **SC-003**: El 100% de las solicitudes de transferencia lógicamente repetidas resultan en como
  máximo un único movimiento financiero aplicado.
- **SC-004**: El 100% de las transferencias completadas exitosamente son identificables
  posteriormente mediante un identificador único de operación, incluso después de cerrar y volver
  a utilizar la aplicación.
- **SC-005**: El 100% de los importes de transferencia se procesan en Soles (PEN) con exactamente
  dos posiciones decimales.

## Assumptions

- La especificación `001-consulta-productos-bancarios` ya define la existencia, propiedad y
  consulta de las cuentas de ahorro del cliente; esta spec reutiliza esas cuentas y no redefine
  su modelo de listado/detalle.
- Existen datos ficticios previamente creados (al menos dos cuentas propias) para demostrar esta
  funcionalidad.
- Existe un único cliente ficticio activo en el contexto de esta versión; no hay autenticación.
- Todas las cuentas utilizadas por esta spec son cuentas de ahorro en PEN; no hay conversión de
  moneda ni comisiones.
- Una transferencia entre cuentas propias se procesa de forma inmediata: su resultado observable
  es completada o rechazada; no se modela un estado pendiente prolongado.
- Las transferencias rechazadas no requieren conservar un historial propio consultable; solo las
  transferencias completadas exitosamente deben poder identificarse posteriormente (FR-027).
- La vista previa (FR-010) no reserva ni bloquea el saldo de la cuenta origen; el saldo disponible
  se revalida recién al confirmar (FR-012). Por lo tanto, dos vistas previas simultáneas podrían
  competir por el mismo saldo, y solo debe prevalecer la confirmación que aún encuentre
  condiciones válidas; la otra se rechaza sin modificar saldos.
- Una vista previa no vence explícitamente en esta spec (no se define un tiempo de expiración);
  su validez se determina exclusivamente al momento de confirmarla, revalidando las condiciones
  vigentes en ese instante (FR-012).
- El identificador de operación solo necesita ser único y legible por el cliente; no existe un
  formato funcional específico requerido.
- La fecha y hora de una transferencia se registra y se muestra en la zona horaria de Perú
  (UTC-5), consistente con el Principio III de la constitución (mercado peruano).
- Los identificadores utilizados para localizar cuentas no constituyen por sí mismos prueba de
  propiedad (mismo principio que en `001`).
- Las transferencias hacia cuentas pertenecientes a otros clientes quedan fuera de esta spec y se
  tratarán en `003-third-party-transfers`.
