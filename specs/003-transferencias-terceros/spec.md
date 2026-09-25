# Feature Specification: Transferencias a cuentas de terceros

**Feature Branch**: `003-transferencias-terceros`

**Created**: 2026-09-25

**Status**: Draft

**Input**: User description: "Transferencias de dinero desde una cuenta de ahorro del cliente
actual hacia una cuenta de ahorro perteneciente a un cliente distinto, dentro del mismo banco. La
cuenta origen debe pertenecer al cliente actual y estar ACTIVA; la cuenta destino debe existir y
pertenecer a otro cliente. La operación debe preservar la integridad financiera (débito en origen,
crédito idéntico en destino), ser atómica, generar un identificador de operación con fecha/hora, no
duplicar el movimiento ante una solicitud repetida, y no revelar información financiera sensible
del cliente destinatario (saldo, otros productos, identificadores internos). Continúa sin
autenticación, sobre el cliente ficticio ordenante ya definido en las specs 001/002."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Transferir dinero a la cuenta de un tercero (Priority: P1)

Como cliente ordenante, quiero seleccionar una cuenta propia como origen, indicar la cuenta de un
tercero como destino, verificar que el destino es reconocible antes de confirmar, e indicar un
importe, para enviar dinero a otro cliente del banco con la certeza de lo que voy a ejecutar.

**Why this priority**: Es la razón de ser de esta feature; sin una transferencia a terceros
exitosa no hay ningún valor de negocio que entregar. Es el incremento mínimo demostrable.

**Independent Test**: Con el Cliente A sembrado con una cuenta ACTIVA (S/ 2,500.00) y el Cliente B
sembrado con una cuenta destino válida (S/ 700.00), solicitar una vista previa de S/ 300.00 desde
la cuenta del Cliente A hacia la cuenta del Cliente B, confirmarla, y verificar que la cuenta del
Cliente A queda en S/ 2,200.00 y la cuenta del Cliente B en S/ 1,000.00.

**Acceptance Scenarios**:

1. **Given** que el Cliente A posee una cuenta ACTIVA con S/ 2,500.00 y el Cliente B posee una
   cuenta destino válida con S/ 700.00, **When** el Cliente A solicita una vista previa de
   S/ 300.00 hacia la cuenta del Cliente B, **Then** el sistema identifica la cuenta origen, la
   cuenta destino enmascarada, la información mínima permitida del destinatario y el importe, y
   entrega una referencia de vista previa, sin modificar ningún saldo todavía.
2. **Given** la vista previa del escenario anterior, **When** el Cliente A la confirma, **Then**
   la transferencia se completa, la cuenta del Cliente A queda en S/ 2,200.00 y la cuenta del
   Cliente B en S/ 1,000.00.
3. **Given** el escenario anterior, **When** se completa la transferencia, **Then** el importe
   debitado de la cuenta origen es exactamente igual al acreditado en la cuenta destino y la suma
   total de ambos saldos no cambia.
4. **Given** que la cuenta origen tiene S/ 500.00 disponibles, **When** el cliente solicita una
   vista previa y confirma una transferencia por el total de S/ 500.00 hacia la cuenta de un
   tercero, **Then** la operación se completa y la cuenta origen queda en S/ 0.00.
5. **Given** que el cliente solicitó una vista previa válida y, antes de confirmarla, el saldo de
   la cuenta origen cambió de forma que el importe ya no es cubierto, **When** el cliente confirma
   esa vista previa, **Then** la confirmación es rechazada en ese momento y ningún saldo se
   modifica.

---

### User Story 2 - Rechazar transferencias a terceros inválidas (Priority: P1)

Como cliente ordenante, quiero que cualquier intento inválido (saldo insuficiente, importe cero o
negativo, cuenta origen ajena o bloqueada, cuenta destino inexistente, cuenta destino que en
realidad es mía) sea rechazado sin alterar ningún saldo, para que mi dinero esté protegido frente a
operaciones erróneas o no autorizadas.

**Why this priority**: Sin estas validaciones, User Story 1 por sí sola permitiría mover dinero de
forma incorrecta o insegura; es tan crítica como el camino feliz (Principio V/VI de la
constitución).

**Independent Test**: Con las mismas cuentas de referencia, se puede verificar de forma aislada
que cada intento inválido es rechazado y que los saldos de ambas cuentas quedan exactamente igual
que antes del intento.

**Acceptance Scenarios**:

1. **Given** que la cuenta origen posee S/ 100.00 disponibles, **When** el cliente intenta
   transferir S/ 150.00, **Then** la operación es rechazada y ningún saldo cambia.
2. **Given** que existen una cuenta origen y una cuenta destino válidas, **When** el cliente
   intenta transferir S/ 0.00 o un importe negativo, **Then** la operación es rechazada y ningún
   saldo cambia.
3. **Given** que una cuenta pertenece a otro cliente, **When** el cliente actual intenta
   utilizarla como origen, **Then** la operación es rechazada, ningún saldo es modificado, y la
   respuesta es idéntica a la que se obtendría si esa cuenta no existiera (mismo criterio de
   privacidad que 001/002).
4. **Given** que la cuenta origen está BLOQUEADA, **When** el cliente intenta transferir dinero,
   **Then** la operación es rechazada y ningún saldo cambia.
5. **Given** que la cuenta origen es válida, **When** el cliente indica una cuenta destino
   inexistente, **Then** la transferencia es rechazada y la cuenta origen conserva su saldo.
6. **Given** que las cuentas origen y destino pertenecen al mismo cliente, **When** se intenta
   realizar la operación mediante esta funcionalidad, **Then** la operación no se procesa como
   transferencia a terceros (permanece gobernada por `002-transferencias-cuentas-propias`).

---

### User Story 3 - Evitar transferencias a terceros duplicadas (Priority: P1)

Como cliente ordenante, quiero que reenviar por error la misma solicitud de transferencia no
aplique el movimiento de dinero más de una vez, para no sufrir débitos duplicados.

**Why this priority**: Es una garantía de integridad financiera no negociable (Principio V de la
constitución); sin ella, un simple reintento de red podría duplicar un débito real hacia un
tercero.

**Independent Test**: Con una transferencia de S/ 300.00 ya completada, reenviar la misma
solicitud lógica y verificar que no se produce un segundo débito ni un segundo crédito.

**Acceptance Scenarios**:

1. **Given** que una transferencia de S/ 300.00 ya fue completada correctamente, **When** la
   misma solicitud lógica vuelve a recibirse, **Then** no se realiza un segundo débito de
   S/ 300.00 ni un segundo crédito de S/ 300.00.

---

### User Story 4 - Consultar el resultado de una transferencia a terceros (Priority: P2)

Como cliente ordenante, quiero poder consultar el resultado de una transferencia que realicé
(identificador de operación, fecha y hora, cuentas involucradas, información permitida del
destinatario, importe y resultado), para verificar que la información financiera mostrada es
correcta.

**Why this priority**: Complementa User Story 1 permitiendo revisar una operación ya realizada;
aporta valor adicional pero depende de que exista al menos una transferencia completada.

**Independent Test**: Con una transferencia ya completada y su identificador de operación
conocido, se puede consultar su resultado de forma directa y verificar que expone los datos
mínimos requeridos sin exponer información no permitida del destinatario.

**Acceptance Scenarios**:

1. **Given** que una transferencia se completa correctamente, **When** el cliente ordenante
   consulta su resultado, **Then** puede identificar el número de operación, fecha y hora, cuenta
   origen y destino enmascaradas, información permitida del destinatario, importe y resultado de
   la operación, sin poder ver el saldo ni otros productos del destinatario.
2. **Given** que una transferencia fue completada correctamente, **When** la aplicación se cierra
   y posteriormente vuelve a utilizarse, **Then** los saldos actualizados continúan disponibles y
   la transferencia realizada conserva su información básica.

---

### Edge Cases

- **Importe con más de dos decimales**: la operación no debe ejecutarse con un importe cuya
  precisión no sea válida para esta versión (rechazo, ningún saldo cambia).
- **Cuenta origen inexistente**: la operación se rechaza sin modificar ningún saldo, con la misma
  respuesta que si la cuenta origen perteneciera a otro cliente (FR-022).
- **Cuenta destino inexistente**: la operación se rechaza sin producir ningún movimiento
  financiero (FR-024).
- **Cuenta destino perteneciente al cliente ordenante**: no se procesa mediante esta feature (ver
  User Story 2, escenario 6).
- **Saldo cero**: una cuenta origen con saldo S/ 0.00 no puede realizar una transferencia por un
  importe positivo.
- **Solicitud repetida**: reenviar la misma operación no genera movimientos financieros
  adicionales (ver User Story 3).
- **Error durante la operación**: si la transferencia no puede completarse íntegramente, ambos
  saldos deben conservar los valores que tenían antes de iniciarla (no se observa un estado a
  medio aplicar).
- **Vista previa desactualizada al confirmar**: si las condiciones que hicieron válida una vista
  previa (saldo, estado de la cuenta origen) cambiaron antes de que el cliente la confirme, la
  confirmación se rechaza en ese momento sin modificar ningún saldo.
- **Solicitudes concurrentes sobre el mismo saldo**: si varias transferencias (a terceros o entre
  cuentas propias) intentan consumir el mismo saldo disponible de la cuenta origen al mismo
  tiempo, el resultado final nunca debe permitir gastar más dinero del disponible.

## Requirements *(mandatory)*

### Functional Requirements

**Cuenta origen**

- **FR-001**: El sistema DEBE permitir utilizar como cuenta origen únicamente una cuenta
  perteneciente al cliente ordenante actual.
- **FR-002**: La cuenta origen DEBE ser una cuenta de ahorro denominada en Soles peruanos (PEN).
- **FR-003**: La cuenta origen DEBE encontrarse en estado ACTIVA para poder realizar una
  transferencia.

**Cuenta destino**

- **FR-004**: El sistema DEBE permitir indicar como destino una cuenta que exista, esté
  denominada en PEN, y pertenezca a un cliente distinto del cliente ordenante (mismo banco).
- **FR-005**: La cuenta destino DEBE ser diferente de la cuenta origen.
- **FR-006**: Si la cuenta indicada como destino pertenece al mismo cliente ordenante, la
  operación NO DEBE procesarse como transferencia a terceros: se rechaza desde esta
  funcionalidad, sin redirigir automáticamente a `002-transferencias-cuentas-propias` (ver
  Assumptions).
- **FR-007**: El sistema NO DEBE revelar información financiera sensible de la cuenta destino (p.
  ej. su saldo) durante la validación de su existencia.
- **FR-008**: Si la cuenta destino existe pero se encuentra BLOQUEADA, el sistema DEBE
  [NEEDS CLARIFICATION: ¿una cuenta destino BLOQUEADA puede recibir una transferencia a terceros,
  o debe rechazarse igual que si fuera cuenta origen? PA3 del input original]
- **FR-009**: El cliente DEBE identificar la cuenta destino mediante
  [NEEDS CLARIFICATION: ¿qué dato ingresa el cliente para indicar la cuenta destino — el número de
  cuenta bancaria completo del destinatario, un identificador interno, u otro medio funcional? PA1
  del input original]

**Privacidad del destinatario**

- **FR-010**: Antes de confirmar la operación, el sistema DEBE mostrar al cliente ordenante,
  como mínimo: cuenta origen, cuenta destino enmascarada, información mínima del destinatario
  suficiente para reconocer razonablemente el destino, e importe.
- **FR-011**: La información mínima del destinatario mostrada en FR-010 y en el resultado
  (FR-021) DEBE limitarse a
  [NEEDS CLARIFICATION: ¿qué dato(s) del destinatario deben mostrarse — nombre completo, nombre
  parcialmente oculto, u otra representación? PA2 del input original]
- **FR-012**: El sistema NO DEBE exponer al cliente ordenante el saldo, otros productos
  (cuentas, tarjetas), identificadores internos, ni ninguna otra información financiera del
  cliente destinatario que no sea la definida en FR-011.

**Importe**

- **FR-013**: El cliente DEBE indicar un importe mayor que S/ 0.00.
- **FR-014**: El importe DEBE expresarse en PEN y admitir como máximo dos posiciones decimales.
- **FR-015**: El importe NO DEBE superar el saldo disponible de la cuenta origen.

**Ejecución**

- **FR-016**: Una transferencia confirmada válidamente DEBE disminuir el saldo disponible de la
  cuenta origen exactamente por el importe transferido.
- **FR-017**: La misma transferencia DEBE aumentar el saldo disponible de la cuenta destino
  exactamente por el mismo importe.
- **FR-018**: El débito y el crédito DEBEN formar parte de una única operación lógica: la
  confirmación NO DEBE dejar una sola de las cuentas modificada si la operación no puede
  completarse correctamente.
- **FR-019**: Una confirmación exitosa DEBE generar un identificador único de operación.
- **FR-020**: Una confirmación exitosa DEBE registrar la fecha y hora en que fue realizada.
- **FR-021**: Después de confirmar una transferencia, el sistema DEBE permitir conocer al menos:
  identificador de operación, fecha y hora, cuenta origen enmascarada, cuenta destino
  enmascarada, información permitida del destinatario (FR-011), importe, y resultado de la
  operación.

**Rechazos**

- **FR-022**: Si la cuenta origen no pertenece al cliente ordenante actual o no existe, la
  operación DEBE ser rechazada, y la respuesta DEBE ser la misma en ambos casos, sin revelar si
  la cuenta origen existe (mismo criterio de privacidad que FR-021/FR-022 de
  `002-transferencias-cuentas-propias`).
- **FR-023**: Si la cuenta origen está BLOQUEADA, la operación DEBE ser rechazada y ningún saldo
  DEBE modificarse.
- **FR-024**: Si la cuenta destino no existe, la operación DEBE ser rechazada y ningún saldo DEBE
  modificarse.
- **FR-025**: Si el importe es cero o negativo, la operación DEBE ser rechazada y ningún saldo
  DEBE modificarse.
- **FR-026**: Si el saldo disponible de la cuenta origen es insuficiente para el importe
  solicitado, la operación DEBE ser rechazada y ningún saldo DEBE modificarse.
- **FR-027**: Toda operación rechazada por una validación previa al movimiento de dinero DEBE
  conservar sin cambios los saldos de la cuenta origen y de la cuenta destino.

**Duplicidad**

- **FR-028**: Una misma solicitud lógica de confirmación NO DEBE producir más de un movimiento
  financiero.
- **FR-029**: Si una confirmación ya procesada correctamente se recibe nuevamente como la misma
  operación, los saldos NO DEBEN modificarse una segunda vez.

**Persistencia observable**

- **FR-030**: Después de una transferencia exitosa, los saldos resultantes DEBEN continuar
  disponibles cuando la aplicación sea cerrada y posteriormente utilizada nuevamente.
- **FR-031**: La información necesaria para identificar y consultar posteriormente la
  transferencia DEBE conservarse.

### Key Entities *(include if feature involves data)*

- **Transferencia a terceros**: registro de un movimiento de dinero completado entre la cuenta de
  un cliente ordenante y la cuenta de un cliente destinatario distinto. Atributos relevantes:
  identificador único de operación, fecha y hora, cuenta origen (referencia a `Account` de la
  spec 001), cuenta destino (referencia a `Account`), importe (PEN, 2 decimales), resultado. No se
  modela un estado "pendiente" prolongado (Assumptions): el resultado observable es completada o
  rechazada.
- **Vista previa de transferencia a terceros**: representación transitoria de una solicitud aún no
  confirmada (cuenta origen, cuenta destino, información mínima del destinatario permitida,
  importe, y una referencia para confirmarla). No es una `Transferencia`: no tiene efecto
  financiero y no queda registrada como movimiento; no reserva saldo.
- **Cliente destinatario**: cliente ficticio distinto del cliente ordenante, propietario de la
  cuenta destino. No participa activamente en la operación; solo aporta la información mínima
  visible definida en FR-011.
- **Cuenta de ahorro (`Account`)**: entidad ya definida en `001-consulta-productos-bancarios`;
  esta spec reutiliza la operación de débito/crédito sobre su saldo introducida en
  `002-transferencias-cuentas-propias`, sujeta a las reglas de propiedad, estado y moneda ya
  establecidas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de las transferencias a terceros completadas exitosamente preservan la suma
  total de los saldos de la cuenta origen y destino (conservación del dinero, sin comisiones).
- **SC-002**: El 0% de las transferencias a terceros rechazadas (saldo insuficiente, importe
  inválido, cuenta origen ajena o bloqueada, cuenta destino inexistente, destino que resulta ser
  cuenta propia) modifica el saldo de alguna de las cuentas involucradas, y la respuesta ante una
  cuenta origen inexistente es idéntica a la de esa misma cuenta perteneciendo a otro cliente.
- **SC-003**: El 100% de las solicitudes de transferencia a terceros lógicamente repetidas
  resultan en como máximo un único movimiento financiero aplicado.
- **SC-004**: El 100% de las transferencias a terceros completadas exitosamente son identificables
  posteriormente mediante un identificador único de operación, incluso después de cerrar y volver
  a utilizar la aplicación.
- **SC-005**: El 100% de los importes de transferencia se procesan en Soles (PEN) con exactamente
  dos posiciones decimales.
- **SC-006**: El 0% de las vistas previas o resultados de transferencia a terceros expone el
  saldo u otros productos del cliente destinatario al cliente ordenante.

## Assumptions

- Las especificaciones `001-consulta-productos-bancarios` y
  `002-transferencias-cuentas-propias` ya definen la existencia, propiedad, consulta y operación
  de débito/crédito de las cuentas de ahorro; esta spec reutiliza esos conceptos sin redefinirlos.
- Existen múltiples clientes ficticios con cuentas ficticias previamente creadas; existe un único
  cliente ordenante activo en el contexto de la aplicación (sin autenticación), igual que en
  `001`/`002`.
- Todas las cuentas utilizadas por esta spec son cuentas de ahorro en PEN; no hay conversión de
  moneda ni comisiones.
- La transferencia a terceros se procesa de forma inmediata: su resultado observable es
  completada o rechazada; no se modela un estado pendiente prolongado (mismo criterio que `002`).
- Al igual que en `002`, esta feature requiere un paso explícito de vista previa (que valida y
  muestra los datos, incluyendo la información permitida del destinatario) separado de un paso de
  confirmación que recién ejecuta el movimiento — manteniendo consistencia con el único patrón de
  transferencia ya establecido en el sistema, en vez de introducir un segundo patrón de flujo.
- Las transferencias rechazadas no requieren conservar un historial propio consultable; solo las
  transferencias completadas exitosamente deben poder identificarse posteriormente (FR-031),
  mismo criterio que `002`.
- El identificador de operación solo necesita ser único y legible por el cliente; no existe un
  formato funcional específico requerido (mismo criterio que `002`).
- La fecha y hora de una transferencia se registra y se muestra en la zona horaria de Perú
  (UTC-5), consistente con el Principio III de la constitución (mercado peruano) y con `002`.
- Los identificadores o números utilizados para localizar cuentas no constituyen por sí mismos
  prueba de propiedad ni de autorización (mismo principio que `001`/`002`).
- La regla de respuesta indistinguible entre "cuenta inexistente" y "cuenta perteneciente a otro
  cliente" (Principio VI, FR-022) se aplica únicamente a la **cuenta origen**: para la cuenta
  destino, "pertenecer a otro cliente" es precisamente el camino exitoso de esta feature (no un
  rechazo), por lo que no aplica el mismo criterio de indistinguibilidad allí. "Cuenta destino
  inexistente" (FR-024) y "cuenta destino resulta ser una cuenta propia del ordenante" (FR-006) sí
  pueden distinguirse entre sí, porque ninguna de las dos revela información de un tercero: el
  cliente ordenante ya sabe si una cuenta le pertenece.
- Si el destino indicado resulta pertenecer al propio cliente ordenante, la operación se rechaza
  desde esta funcionalidad sin redirigir automáticamente al flujo de `002`; el cliente debe
  invocar esa funcionalidad por separado si esa era su intención.
