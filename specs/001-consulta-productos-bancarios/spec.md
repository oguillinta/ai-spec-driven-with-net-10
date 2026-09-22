# Feature Specification: Consulta de productos bancarios del cliente

**Feature Branch**: `001-consulta-productos-bancarios`

**Created**: 2026-09-20

**Status**: Draft

**Input**: User description: "Consulta de productos bancarios del cliente: el cliente debe poder
listar y consultar el detalle de sus cuentas de ahorro y de sus tarjetas de débito, viendo saldo
disponible, moneda, estado y datos principales, sin poder acceder a productos de otros clientes.
No incluye autenticación ni operaciones de escritura; todos los datos son ficticios."

## Clarifications

### Session 2026-09-20

- Q: ¿Qué formato de enmascaramiento debe usarse para el número de cuenta mostrado al cliente? → A: Igual que las tarjetas: se muestran solo los últimos 4 dígitos, el resto enmascarado (ej. ****5678).
- Q: Cuando un cliente intenta consultar un producto que no existe versus uno que pertenece a otro cliente, ¿el sistema debe mostrar la misma respuesta genérica en ambos casos, o puede distinguir entre "no encontrado" y "no autorizado"? → A: Respuesta genérica idéntica en ambos casos, sin revelar si el producto ajeno existe.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Listar mis cuentas (Priority: P1)

Como cliente, quiero visualizar todas mis cuentas de ahorro, para conocer qué cuentas bancarias
tengo disponibles y su saldo.

**Why this priority**: Es el punto de entrada principal de la aplicación: sin poder ver la lista
de cuentas propias, el cliente no obtiene ningún valor. Es el MVP mínimo demostrable.

**Independent Test**: Con el Cliente A cargado con dos cuentas de ahorro (una ACTIVA y una
BLOQUEADA), se puede verificar que al consultar sus cuentas aparecen exactamente esas dos, cada
una con tipo, número enmascarado, saldo disponible, moneda y estado, sin necesidad de que existan
tarjetas ni otras funcionalidades.

**Acceptance Scenarios**:

1. **Given** que el Cliente A posee dos cuentas de ahorro, **When** consulta sus cuentas, **Then**
   visualiza exactamente sus dos cuentas y cada una muestra tipo, número enmascarado, saldo
   disponible, moneda y estado.
2. **Given** que una de las cuentas del Cliente A está BLOQUEADA, **When** consulta sus cuentas,
   **Then** la cuenta continúa apareciendo y se identifica claramente como BLOQUEADA.
3. **Given** que el Cliente A consulta cualquiera de sus cuentas, **When** visualiza su
   información financiera, **Then** la moneda mostrada es PEN y los importes se presentan en
   Soles con dos decimales.

---

### User Story 2 - Consultar el detalle de una cuenta (Priority: P2)

Como cliente, quiero consultar el detalle de una de mis cuentas, para conocer su número, saldo
disponible, moneda y estado con mayor precisión que en el listado.

**Why this priority**: Complementa el listado (US1) permitiendo profundizar en un producto
específico; aporta valor adicional pero depende de que el listado ya exista.

**Independent Test**: Con una cuenta conocida del Cliente A, se puede solicitar su detalle de
forma directa y verificar que expone tipo, número enmascarado, saldo disponible, moneda y estado,
de forma independiente de si se navegó o no desde el listado.

**Acceptance Scenarios**:

1. **Given** que una cuenta pertenece al Cliente A, **When** consulta su detalle, **Then** puede
   visualizar su tipo, número enmascarado, saldo disponible, moneda y estado.

---

### User Story 3 - Listar mis tarjetas de débito (Priority: P2)

Como cliente, quiero visualizar mis tarjetas de débito, para conocer qué tarjetas están asociadas
a mis productos bancarios.

**Why this priority**: Extiende la visibilidad de productos bancarios más allá de las cuentas;
aporta valor equivalente al listado de cuentas pero sobre una segunda familia de productos.

**Independent Test**: Con el Cliente A cargado con dos tarjetas de débito (una ACTIVA y una
BLOQUEADA), se puede verificar que al consultar sus tarjetas aparecen exactamente esas dos, cada
una con número enmascarado, últimos cuatro dígitos, cuenta asociada, estado y fecha de
vencimiento.

**Acceptance Scenarios**:

1. **Given** que el Cliente A posee dos tarjetas de débito, **When** consulta sus tarjetas,
   **Then** visualiza exactamente sus dos tarjetas y cada una muestra número enmascarado, últimos
   cuatro dígitos, cuenta asociada, estado y fecha de vencimiento.
2. **Given** que una tarjeta del Cliente A está BLOQUEADA, **When** consulta sus tarjetas,
   **Then** la tarjeta continúa apareciendo y se identifica claramente como BLOQUEADA.
3. **Given** que una tarjeta termina en 4582, **When** aparece en una lista o detalle, **Then**
   los últimos cuatro dígitos 4582 son visibles y los demás dígitos no se muestran.

---

### User Story 4 - Consultar el detalle de una tarjeta (Priority: P3)

Como cliente, quiero consultar el detalle de una tarjeta de débito, para conocer sus datos
principales, estado, vencimiento y cuenta asociada.

**Why this priority**: Igual que US2 respecto de US1, profundiza sobre un producto específico ya
visible en el listado de tarjetas (US3); es la funcionalidad de menor prioridad relativa por
depender de las tres anteriores para tener sentido de uso completo.

**Independent Test**: Con una tarjeta conocida del Cliente A, se puede solicitar su detalle de
forma directa y verificar que expone número enmascarado, últimos cuatro dígitos, cuenta asociada,
estado y fecha de vencimiento, incluyendo que la cuenta asociada pertenece también al Cliente A.

**Acceptance Scenarios**:

1. **Given** que una tarjeta pertenece al Cliente A, **When** consulta su detalle, **Then** puede
   visualizar su número enmascarado, últimos cuatro dígitos, cuenta asociada, estado y fecha de
   vencimiento.
2. **Given** que una tarjeta de débito pertenece al Cliente A, **When** consulta la tarjeta,
   **Then** puede identificar la cuenta de ahorro a la que está asociada y dicha cuenta pertenece
   también al Cliente A.

---

### User Story 5 - Proteger mis productos (Priority: P1)

Como cliente, quiero que únicamente se muestren productos que me pertenecen, para que la
información financiera de otros clientes no quede expuesta.

**Why this priority**: Es una garantía de seguridad no negociable (ver Principio VI de la
constitución) que debe cumplirse desde el primer incremento; sin ella, listar o consultar
productos representaría una fuga de información aunque el resto de funcionalidades ya funcione.

**Independent Test**: Con un Cliente A y un Cliente B, cada uno con productos propios, se puede
verificar de forma aislada que el Cliente A no logra ver ni consultar el detalle de ninguna
cuenta o tarjeta perteneciente al Cliente B, incluso conociendo su identificador.

**Acceptance Scenarios**:

1. **Given** que una cuenta pertenece al Cliente B, **When** el Cliente A intenta consultar esa
   cuenta, **Then** el sistema no muestra su información financiera y responde con el mismo
   mensaje genérico que usaría si esa cuenta no existiera.
2. **Given** que una tarjeta pertenece al Cliente B, **When** el Cliente A intenta consultar dicha
   tarjeta, **Then** el sistema no muestra sus datos y responde con el mismo mensaje genérico que
   usaría si esa tarjeta no existiera.

---

### Edge Cases

- **Cliente sin cuentas**: si el cliente no posee cuentas, la consulta devuelve una colección
  vacía y se muestra un estado vacío comprensible; no se deben mostrar productos de otros
  clientes.
- **Cliente sin tarjetas**: si el cliente no posee tarjetas de débito, la consulta devuelve una
  colección vacía y se muestra un estado vacío comprensible.
- **Saldo cero**: una cuenta con saldo disponible S/ 0.00 continúa siendo visible normalmente.
- **Todos los productos bloqueados**: si todas las cuentas o tarjetas están bloqueadas, continúan
  apareciendo con su estado correspondiente.
- **Producto inexistente**: al intentar consultar un producto que no existe, el sistema no
  muestra información de ningún otro producto y responde con el mismo mensaje genérico definido
  para un producto ajeno (ver FR-022).
- **Producto de otro cliente**: conocer o proporcionar el identificador de una cuenta o tarjeta
  ajena no permite visualizar sus datos; la respuesta es indistinguible de la de un producto
  inexistente (ver FR-022).
- **Tarjeta con cuenta bloqueada**: una tarjeta asociada a una cuenta bloqueada continúa siendo
  visible; esta spec no define ningún comportamiento operativo adicional derivado de esa
  condición.

## Requirements *(mandatory)*

### Functional Requirements

**Cuentas**

- **FR-001**: El sistema DEBE permitir consultar la lista de cuentas de ahorro pertenecientes al
  cliente actual.
- **FR-002**: Por cada cuenta listada, el sistema DEBE mostrar como mínimo tipo de cuenta, número
  de cuenta enmascarado (solo los últimos cuatro dígitos visibles), saldo disponible, moneda y
  estado.
- **FR-003**: El sistema DEBE permitir consultar el detalle de una cuenta perteneciente al cliente
  actual.
- **FR-004**: El detalle de la cuenta DEBE mostrar tipo de cuenta, número de cuenta enmascarado
  (solo los últimos cuatro dígitos visibles), saldo disponible, moneda y estado.
- **FR-005**: En esta versión, todas las cuentas DEBEN ser cuentas de ahorro denominadas en Soles
  peruanos (PEN).
- **FR-006**: Una cuenta puede tener uno de los siguientes estados: ACTIVA o BLOQUEADA.
- **FR-007**: Una cuenta BLOQUEADA DEBE continuar siendo visible para su propietario y su estado
  DEBE mostrarse claramente.

**Tarjetas de débito**

- **FR-008**: El sistema DEBE permitir consultar la lista de tarjetas de débito pertenecientes al
  cliente actual.
- **FR-009**: Por cada tarjeta listada, el sistema DEBE mostrar como mínimo número de tarjeta
  enmascarado, últimos cuatro dígitos, cuenta asociada, estado y fecha de vencimiento.
- **FR-010**: El sistema DEBE permitir consultar el detalle de una tarjeta de débito perteneciente
  al cliente actual.
- **FR-011**: El detalle de una tarjeta DEBE mostrar número de tarjeta enmascarado, últimos cuatro
  dígitos, cuenta asociada, estado y fecha de vencimiento.
- **FR-012**: Cada tarjeta de débito DEBE estar asociada a exactamente una cuenta de ahorro del
  cliente.
- **FR-013**: Una tarjeta puede tener uno de los siguientes estados: ACTIVA o BLOQUEADA.
- **FR-014**: Una tarjeta BLOQUEADA DEBE continuar siendo visible para su propietario y su estado
  DEBE mostrarse claramente.

**Propiedad y privacidad**

- **FR-015**: El sistema DEBE mostrar únicamente cuentas y tarjetas pertenecientes al cliente
  actual.
- **FR-016**: El cliente NO DEBE poder consultar el detalle de una cuenta perteneciente a otro
  cliente.
- **FR-017**: El cliente NO DEBE poder consultar el detalle de una tarjeta perteneciente a otro
  cliente.
- **FR-018**: Los números completos de cuenta y de tarjeta NO DEBEN mostrarse en las interfaces de
  consulta.
- **FR-019**: El número de tarjeta DEBE mostrarse enmascarado conservando visibles únicamente los
  últimos cuatro dígitos.

**Disponibilidad de la información**

- **FR-020**: Las cuentas y tarjetas ficticias previamente creadas DEBEN continuar disponibles
  cuando la aplicación sea cerrada y utilizada nuevamente.
- **FR-021**: Esta especificación es exclusivamente de consulta. El sistema NO DEBE permitir
  crear, modificar ni eliminar cuentas o tarjetas mediante las funcionalidades definidas en esta
  spec.
- **FR-022**: Ante un intento de consulta sobre una cuenta o tarjeta que no existe, y ante un
  intento de consulta sobre una cuenta o tarjeta perteneciente a otro cliente, el sistema DEBE
  responder con el mismo mensaje genérico en ambos casos, de modo que el cliente NO DEBA poder
  distinguir si un identificador ajeno corresponde a un producto real.

### Key Entities *(include if feature involves data)*

- **Cliente**: persona ficticia propietaria de productos bancarios. En esta versión existe un
  único cliente activo en el contexto de uso; no se maneja registro ni autenticación.
- **Cuenta de ahorro**: producto bancario propiedad de un cliente. Atributos relevantes: tipo de
  cuenta, número (enmascarado en toda interfaz de consulta, solo últimos cuatro dígitos visibles),
  saldo disponible, moneda (PEN) y estado (ACTIVA o BLOQUEADA). Pertenece a exactamente un
  cliente.
- **Tarjeta de débito**: producto bancario propiedad de un cliente, asociado a exactamente una
  cuenta de ahorro del mismo cliente. Atributos relevantes: número (enmascarado, solo últimos
  cuatro dígitos visibles), fecha de vencimiento, estado (ACTIVA o BLOQUEADA) y referencia a la
  cuenta asociada.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de las cuentas y tarjetas propiedad del cliente activo aparecen en sus
  respectivos listados, sin omisiones.
- **SC-002**: El 0% de los intentos de consulta sobre cuentas o tarjetas pertenecientes a otro
  cliente exponen información financiera o de identificación de esos productos, y la respuesta
  obtenida es indistinguible de la de un producto inexistente.
- **SC-003**: El 100% de los números de cuenta y de tarjeta mostrados en listados y detalles
  aparecen enmascarados, sin exponer el número completo en ningún caso.
- **SC-004**: El 100% de los saldos e importes se presentan en Soles (PEN) con exactamente dos
  posiciones decimales.
- **SC-005**: Un cliente puede identificar, desde el listado de cuentas o tarjetas, el estado
  (ACTIVA/BLOQUEADA) de cada producto en menos de 5 segundos de lectura, sin pasos adicionales.
- **SC-006**: Las cuentas y tarjetas ficticias registradas permanecen disponibles e inalteradas
  (mismos saldos, estados y asociaciones) tras cerrar y volver a utilizar la aplicación.

## Assumptions

- Los clientes y productos bancarios necesarios para demostrar esta funcionalidad ya existen como
  datos ficticios de referencia (Cliente A con dos cuentas y dos tarjetas; al menos un Cliente B
  con productos propios).
- Existe un único cliente de demostración activo en el contexto de uso de esta versión; no hay
  selección ni cambio de identidad.
- No es necesario autenticar al cliente para esta spec.
- Los identificadores internos utilizados para localizar productos no constituyen por sí mismos
  prueba de propiedad ni otorgan derecho de acceso.
- Todos los productos, saldos, números e identificadores utilizados son ficticios y no
  representan información financiera ni personal real.
- La funcionalidad descrita en esta spec es exclusivamente de solo lectura; no crea, modifica ni
  elimina cuentas o tarjetas.
- Las transferencias entre cuentas propias y a terceros quedan fuera de esta spec y se tratarán en
  especificaciones independientes (`002-own-account-transfers`, `003-third-party-transfers`).
