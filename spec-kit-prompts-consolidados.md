# Prompts consolidados de GitHub Spec Kit

Proyecto: **Banca Digital Perú**  
Alcance: backend académico con tres especificaciones funcionales.

> Nota: este documento consolida, por spec, los prompts de `/speckit.specify` y `/speckit.plan` que se fueron definiendo durante el proyecto. La redacción está normalizada para dejarlos en un único archivo reutilizable, preservando las decisiones funcionales y técnicas adoptadas.

---


# Constitución del proyecto — Banca Digital Perú

## `/speckit.constitution`

```text
/speckit.constitution

Crea la constitución del proyecto académico "Banca Digital Perú".

El proyecto es una aplicación académica de banca digital orientada al mercado
peruano. No procesa dinero real ni datos reales de personas: todo dato financiero,
personal o transaccional utilizado en specs, planes, pruebas y demostraciones
DEBE ser ficticio.

La constitución debe establecer principios globales, normativos y verificables
para todo el proyecto.

Debe cubrir como mínimo los siguientes principios:

1. Simplicidad ante todo.
   - Entre dos soluciones que satisfagan correctamente los requisitos, elegir la
     más simple.
   - No introducir complejidad, abstracciones ni infraestructura anticipadamente.
   - Toda complejidad adicional debe justificarse y registrarse en "Complexity Tracking".

2. Especificaciones primero y alcance gobernado por ellas.
   - Toda funcionalidad material debe seguir Specification-Driven Development:
     spec → plan → tasks → implementation.
   - No implementar comportamiento observable ni reglas de negocio que no estén
     definidos en una spec aprobada.
   - Las decisiones técnicas no deben alterar silenciosamente los requisitos.

3. Mercado peruano.
   - El producto está dirigido al mercado peruano.
   - El texto visible para el usuario y la terminología bancaria deben utilizar
     español de Perú.
   - PEN es la moneda por defecto.
   - Otras monedas solo pueden incorporarse mediante una spec explícita.

4. Clean Architecture y responsabilidades separadas.
   - Aplicar la Dependency Rule.
   - Las reglas de negocio y casos de uso deben permanecer independientes de UI,
     persistencia, servicios externos y frameworks.
   - La lógica de negocio no debe residir en Infrastructure ni en mecanismos de entrega.

5. Integridad financiera.
   - Los valores monetarios deben manejarse sin pérdida de precisión.
   - Las operaciones que modifiquen dinero deben preservar consistencia ante errores,
     concurrencia y solicitudes duplicadas.
   - Una solicitud repetida no debe aplicar su efecto financiero dos veces.
   - No deben quedar estados parciales.
   - Toda operación financiera debe ser trazable y auditable.

6. Seguridad y mínimo privilegio.
   - Un usuario solo puede consultar u operar recursos para los que está autorizado.
   - La propiedad y autorización deben verificarse en backend.
   - No confiar en información suministrada por el cliente para establecer autorización.
   - Minimizar datos sensibles.
   - No incluir secretos ni credenciales en código o logs.
   - Utilizar exclusivamente datos ficticios.

7. Comportamiento verificable.
   - Los requisitos y criterios de aceptación deben expresarse como comportamiento
     observable y verificable.
   - Preferir escenarios Dado/Cuando/Entonces y criterios medibles.
   - Las specs describen qué y por qué, no cómo.

8. Calidad automatizada.
   - Las reglas de negocio críticas y los casos de uso deben estar protegidos por
     pruebas automatizadas.
   - Las pruebas son obligatorias y deben aparecer como tareas.
   - Las capas internas deben poder verificarse sin depender innecesariamente de
     infraestructura externa.

9. Decisiones técnicas justificadas.
   - Frameworks, bases de datos, librerías, patrones adicionales a Clean Architecture
     e infraestructura deben decidirse durante planning.
   - No convertir decisiones tecnológicas en restricciones globales de la constitución
     salvo que se apruebe una enmienda explícita.

Usar lenguaje normativo:
- DEBE
- NO DEBE
- PUEDE

Cada principio debe incluir su racional.

## Governance

La constitución debe definir:

- La constitución prevalece sobre specs, planes y tasks.
- Ante un conflicto, el artefacto debe corregirse o la constitución debe enmendarse
  explícitamente; nunca ignorar el conflicto.
- Prioridad entre principios:
  1. Integridad financiera y Seguridad.
  2. Clean Architecture.
  3. Simplicidad.
- Toda enmienda debe incluir justificación.
- Utilizar versionado semántico:
  - MAJOR: eliminación o redefinición incompatible de un principio.
  - MINOR: nuevo principio o ampliación material.
  - PATCH: aclaraciones o cambios editoriales.
- Todo plan debe ejecutar un Constitution Check antes de continuar y repetirlo
  después del diseño.

No fijar como restricciones constitucionales tecnologías concretas como:

- .NET;
- ASP.NET Core;
- PostgreSQL;
- Entity Framework;
- FluentValidation;
- xUnit;
- UnitOfWork;
- librerías específicas;

salvo que exista una razón explícita para convertirlas en restricciones globales.

Estas decisiones deben permanecer en `/speckit.plan`.

Generar la constitución en español de Perú.

Usar inicialmente:

Version: 1.0.0
Ratified: 2026-09-20
Last Amended: 2026-09-20
```

Resultado consolidado de la constitución:

```text
I.   Simplicidad ante todo
II.  Especificaciones primero y alcance gobernado por ellas
III. Mercado peruano
IV.  Clean Architecture y responsabilidades separadas
V.   Integridad financiera
VI.  Seguridad y mínimo privilegio
VII. Comportamiento verificable
VIII. Calidad automatizada
IX.  Decisiones técnicas justificadas
```

---

# 001 — Consulta de productos bancarios

## `/speckit.specify`

```text
/speckit.specify

Crea la especificación funcional para la feature:

001-consulta-productos-bancarios

Contexto:
Se trata de una aplicación académica de banca digital para el mercado peruano.
La aplicación todavía no implementará autenticación. Existe un cliente ficticio
previamente contextualizado por la aplicación y el usuario no puede cambiar de
identidad durante esta feature.

Objetivo:
Permitir que el cliente consulte sus productos bancarios.

Alcance funcional:

1. Productos soportados
- Cuentas de ahorro.
- Tarjetas de débito.
- No existen otros tipos de productos en esta feature.

2. Moneda
- Todas las cuentas utilizan PEN.
- No se requiere soporte multimoneda.

3. Consulta de cuentas
El cliente debe poder:
- listar sus cuentas de ahorro;
- consultar el detalle de una cuenta.

Para cada cuenta mostrar:
- tipo de cuenta;
- número de cuenta enmascarado;
- saldo disponible;
- moneda;
- estado.

Solo debe mostrarse el saldo disponible.

4. Consulta de tarjetas de débito
El cliente debe poder:
- listar sus tarjetas de débito;
- consultar el detalle de una tarjeta.

Para cada tarjeta mostrar:
- número de tarjeta enmascarado;
- últimos cuatro dígitos;
- cuenta asociada;
- estado;
- fecha de vencimiento.

Cada tarjeta debe estar asociada exactamente a una cuenta.

5. Estados
Para cuentas y tarjetas considerar:
- ACTIVE / ACTIVA;
- BLOCKED / BLOQUEADA.

Los productos bloqueados deben continuar apareciendo en las consultas.
El hecho de estar bloqueado no debe ocultar el producto.

6. Propiedad y privacidad
- El cliente solo puede consultar productos que le pertenecen.
- Nunca debe obtener información de productos pertenecientes a otro cliente.
- El número completo de cuenta no debe exponerse.
- El número completo de tarjeta no debe exponerse.
- Nunca deben exponerse CVV ni PIN.
- La respuesta para un recurso que no pertenece al cliente debe respetar la
  estrategia de privacidad definida por la especificación.

7. Datos
- Utilizar datos ficticios previamente creados para demostración.
- Los datos deben persistir después de reiniciar la aplicación.
- No se requiere creación de productos desde esta feature.

Historias de usuario esperadas:
- Como cliente, quiero listar mis cuentas para conocer mis productos disponibles.
- Como cliente, quiero consultar el detalle de una cuenta para conocer su saldo
  disponible y estado.
- Como cliente, quiero listar mis tarjetas de débito para conocer las tarjetas
  asociadas a mis cuentas.
- Como cliente, quiero consultar el detalle de una tarjeta para conocer su estado,
  vencimiento y cuenta asociada.
- Como cliente, solo debo poder consultar productos que me pertenecen.

Criterios de aceptación:
- El cliente visualiza únicamente sus propias cuentas.
- El cliente visualiza únicamente sus propias tarjetas.
- Las cuentas bloqueadas siguen siendo visibles.
- Las tarjetas bloqueadas siguen siendo visibles.
- El saldo mostrado corresponde al saldo disponible.
- La moneda mostrada es PEN.
- Los números de cuenta se muestran enmascarados.
- Los números de tarjeta se muestran enmascarados.
- El detalle de una cuenta respeta propiedad y privacidad.
- El detalle de una tarjeta respeta propiedad y privacidad.
- Los datos ficticios continúan disponibles tras reiniciar la aplicación.
- Ninguna respuesta customer-facing expone información sensible innecesaria.

Edge cases:
- Cliente sin cuentas.
- Cliente sin tarjetas.
- Cuenta inexistente.
- Tarjeta inexistente.
- Cuenta perteneciente a otro cliente.
- Tarjeta perteneciente a otro cliente.
- Producto bloqueado.
- Identificador con formato inválido.

Fuera de alcance:
- Autenticación y autorización real.
- Cambio o selección de cliente.
- Creación de cuentas.
- Cierre de cuentas.
- Modificación de cuentas.
- Bloqueo o desbloqueo de productos.
- Emisión de tarjetas.
- Transferencias.
- Pagos.
- CCI.
- Operaciones interbancarias.

Genera una especificación orientada al negocio y al comportamiento observable.
No incluyas decisiones de framework, base de datos o implementación.
Incluye objetivo, historias de usuario, requisitos funcionales, reglas de negocio,
criterios de aceptación, edge cases, supuestos y fuera de alcance.
```

## `/speckit.plan`

```text
/speckit.plan

Genera el plan técnico para la feature activa `001-consulta-productos-bancarios`.

El plan debe cumplir la constitución del proyecto y mantener el alcance definido
en la especificación sin agregar funcionalidades nuevas.

Stack técnico obligatorio:
- .NET 10.
- C#.
- ASP.NET Core 10.
- Clean Architecture.
- DDD pragmático.
- REST APIs.
- API First / Contract First.
- OpenAPI 3.1.
- Scalar para visualizar/documentar el contrato.
- Entity Framework Core 10.
- PostgreSQL.
- Npgsql.
- Dependency Injection nativa de .NET.
- FluentValidation.
- xUnit.
- UnitOfWork explícita y delgada sobre DbContext.

Arquitectura:
Separar como mínimo:
- Domain
- Application
- Infrastructure
- Api

Aplicar estrictamente la Dependency Rule:
- Domain no depende de Application, Infrastructure ni Api.
- Application depende del Domain y define puertos/abstracciones.
- Infrastructure implementa persistencia e integraciones.
- Api actúa como composition root y mantiene controllers/endpoints delgados.

DDD:
- Mantener el Domain como C# puro.
- Modelar únicamente conceptos y reglas que aporten valor.
- Evitar DDD ceremonial.
- No introducir abstracciones sin responsabilidad real.

API First:
El contrato OpenAPI debe diseñarse antes de tasks e implementación.

Crear el contrato en:

specs/001-consulta-productos-bancarios/contracts/openapi/banking-products-v1.yaml

Este archivo será la fuente de verdad de la API para esta feature.

El contrato debe definir, según lo requerido por la spec:
- listado de cuentas;
- detalle de cuenta;
- listado de tarjetas de débito;
- detalle de tarjeta;
- requests/parámetros;
- responses;
- códigos HTTP;
- schemas;
- Problem Details / errores;
- ejemplos relevantes.

Scalar únicamente renderiza el contrato y no sustituye OpenAPI como fuente de verdad.

Cliente actual:
Como autenticación está fuera de alcance:
- definir una abstracción explícita para obtener el cliente ficticio actual;
- evitar dispersar un customerId hardcodeado por controllers/use cases;
- mantener esta decisión reemplazable en el futuro por autenticación real.

Persistencia:
- EF Core 10 con PostgreSQL/Npgsql.
- Persistir Customers, Accounts y DebitCards según sea necesario.
- Datos ficticios preexistentes para demostración.
- La información debe sobrevivir reinicios de la aplicación.
- Configurar relaciones e índices relevantes.

UnitOfWork:
- Usar una UnitOfWork explícita pero delgada sobre DbContext.
- No crear una abstracción de transacciones excesivamente compleja.
- No forzar UnitOfWork en consultas read-only que no requieren commit.
- No introducir Generic Repository salvo necesidad objetiva.
- Los repositories deben expresar necesidades del dominio/aplicación y no ser
  simples wrappers genéricos de EF Core.

Application:
Diseñar casos de uso orientados a capacidades, por ejemplo:
- obtener cuentas del cliente;
- obtener detalle de una cuenta;
- obtener tarjetas del cliente;
- obtener detalle de una tarjeta.

Mantener controllers/endpoints delgados.

Validation:
- FluentValidation para validación de requests/inputs.
- Las invariantes de dominio permanecen en Domain.
- No duplicar reglas sin necesidad.

Errores:
- Utilizar respuestas coherentes basadas en Problem Details.
- Respetar la privacidad cuando un producto no pertenece al cliente.
- No filtrar detalles internos de infraestructura.

Testing:
- xUnit.
- Unit tests para Domain y Application cuando corresponda.
- Integration tests contra PostgreSQL para queries, ownership y persistencia.
- Tests de API para contratos/códigos relevantes.
- Verificar productos bloqueados visibles.
- Verificar masking y privacidad.

Restricciones:
No introducir salvo necesidad demostrada:
- CQRS.
- MediatR.
- Event Bus.
- RabbitMQ.
- Kafka.
- Redis.
- microservicios.
- Generic Repository.
- autenticación.
- infraestructura distribuida.

El plan debe generar como mínimo:
- plan.md;
- research.md;
- data-model.md;
- contrato OpenAPI;
- quickstart.md.

Realizar Constitution Check antes y después del diseño.

El plan debe mantener la solución simple, verificable y consistente con la
especificación 001.
```

---

# 002 — Transferencias entre cuentas propias

## `/speckit.specify`

```text
/speckit.specify

Crea la especificación funcional para la feature:

002-transferencias-cuentas-propias

Contexto:
Esta feature extiende la banca digital académica definida en 001.
No existe autenticación real; se utiliza el mismo cliente ficticio contextualizado
por la aplicación.

Objetivo:
Permitir que el cliente transfiera dinero entre dos cuentas de ahorro de su
propiedad dentro del mismo banco.

Reglas funcionales:

1. Propiedad
- La cuenta origen debe pertenecer al cliente actual.
- La cuenta destino debe pertenecer al mismo cliente.
- Una cuenta perteneciente a otro cliente no puede utilizarse como destino de
  una transferencia entre cuentas propias.
- La cuenta origen y la cuenta destino deben ser diferentes.

2. Producto y moneda
- Solo se soportan cuentas de ahorro.
- Solo se soporta PEN.
- No existe conversión de moneda.

3. Cuenta origen
- Debe existir.
- Debe pertenecer al cliente actual.
- Debe estar habilitada según las reglas de negocio.
- Debe contar con saldo disponible suficiente.

4. Cuenta destino
- Debe existir.
- Debe pertenecer al cliente actual.
- Definir mediante aclaración si una cuenta destino BLOCKED puede recibir o no
  una transferencia.

5. Monto
- Debe ser mayor que cero.
- Debe respetar la precisión monetaria soportada para PEN.
- No puede superar el saldo disponible de la cuenta origen.

6. Flujo
El cliente debe poder proporcionar:
- cuenta origen;
- cuenta destino;
- monto;
- descripción opcional, si la spec final decide mantenerla.

Debe existir un resultado observable de la operación.

Cuando corresponda, el resultado debe incluir:
- identificador de operación/transferencia;
- fecha/hora;
- origen enmascarado;
- destino enmascarado;
- monto;
- moneda;
- resultado/estado.

7. Integridad financiera
Una transferencia exitosa debe:
- debitar exactamente el monto de la cuenta origen;
- acreditar exactamente el mismo monto en la cuenta destino;
- no cobrar comisión;
- conservar el dinero total entre ambas cuentas.

La operación debe ser atómica:
- o se aplican débito, crédito y registro de transferencia;
- o no se aplica ninguno.

No deben existir estados parciales.

8. Idempotencia
Una misma solicitud lógica no debe producir múltiples transferencias financieras.

La solución debe soportar idempotencia persistente:
- un retry de la misma solicitud no debe volver a debitar ni acreditar;
- el comportamiento debe mantenerse incluso ante reintentos concurrentes o reinicios.

9. Persistencia y trazabilidad
Una transferencia confirmada debe quedar persistida.

Registrar la información necesaria para trazabilidad, como:
- identificador;
- cliente;
- cuenta origen;
- cuenta destino;
- monto;
- moneda;
- estado;
- fecha/hora;
- clave de idempotencia cuando corresponda.

10. Errores de negocio
Contemplar al menos:
- fondos insuficientes;
- cuenta origen inexistente;
- cuenta destino inexistente;
- cuenta origen no pertenece al cliente;
- cuenta destino no pertenece al cliente;
- origen y destino iguales;
- cuenta origen bloqueada/no elegible;
- monto inválido.

Ante un rechazo:
- no cambiar balances;
- no generar efectos financieros parciales.

11. Límite con 003
Si la cuenta destino pertenece a otro cliente, la operación NO debe procesarse
silenciosamente como transferencia propia.
Ese escenario corresponde a `003-transferencias-terceros`.

Criterios de aceptación:
- Transferencia propia válida modifica ambos saldos exactamente por el monto.
- La suma de ambos saldos se conserva.
- Una operación rechazada no modifica ningún saldo.
- No se permite transferir más que el saldo disponible.
- No se permite monto cero o negativo.
- No se permite usar la misma cuenta como origen y destino.
- No se permite utilizar como propia una cuenta de otro cliente.
- Una misma solicitud idempotente no produce doble débito/crédito.
- El resultado exitoso es persistente y auditable.
- Los datos continúan consistentes después de reiniciar.

Preguntas de aclaración:
- ¿Una cuenta destino bloqueada puede recibir fondos?
- ¿Se requiere una etapa explícita de previsualización/confirmación antes de ejecutar?
- ¿Las transferencias rechazadas deben quedar registradas o solo las exitosas?
- ¿Qué formato debe tener el identificador visible de operación?
- ¿Qué zona horaria debe utilizarse para la fecha/hora mostrada? Considerar Perú.
- ¿La descripción es parte definitiva del contrato o solo opcional?

Fuera de alcance:
- Transferencias a terceros.
- Transferencias interbancarias.
- CCI.
- Transferencias internacionales.
- Conversión de moneda.
- Comisiones.
- Programación de transferencias.
- Autenticación real.

Genera una especificación orientada al comportamiento observable y reglas de negocio.
No definas frameworks ni detalles de persistencia.
```

## `/speckit.plan`

```text
/speckit.plan

Genera el plan técnico para la feature activa `002-transferencias-cuentas-propias`.

Reutiliza las decisiones técnicas establecidas en 001 y cumple la constitución.

Stack:
- .NET 10 / C#.
- ASP.NET Core 10.
- Clean Architecture.
- DDD pragmático.
- REST.
- API First / OpenAPI 3.1.
- Scalar.
- Entity Framework Core 10.
- PostgreSQL / Npgsql.
- FluentValidation.
- Dependency Injection nativa.
- xUnit.
- UnitOfWork explícita y delgada sobre DbContext.

No cambiar la arquitectura general definida en 001.

API First:
Diseñar/actualizar el contrato OpenAPI de 002 antes de tasks e implementación.

El contrato debe cubrir:
- previsualización si la spec aclarada la requiere;
- confirmación/ejecución de transferencia;
- Idempotency-Key si forma parte del contrato;
- request/response schemas;
- errores funcionales;
- Problem Details;
- ejemplos.

Application:
Definir un caso de uso explícito para transferencia entre cuentas propias,
por ejemplo `TransferBetweenOwnAccounts` o el nombre consistente con el lenguaje
del proyecto.

El caso de uso debe coordinar:
- resolución de cuenta origen;
- resolución de cuenta destino;
- validaciones de propiedad;
- reglas de elegibilidad;
- saldo;
- idempotencia;
- dominio;
- persistencia.

DDD:
No modificar balances mediante lógica anémica como:

account.Balance -= amount

Preferir comportamiento del dominio que proteja invariantes.

Las cuentas origen y destino pueden mantenerse como aggregates separados,
coordinados desde Application mediante una transacción de aplicación.

Modelar `Transfer`, `Money` y otros conceptos únicamente cuando aporten
comportamiento real.

UnitOfWork y atomicidad:
Para la operación write, UnitOfWork es obligatoria.

Debe confirmar en una única transacción local:
- débito de cuenta origen;
- crédito de cuenta destino;
- persistencia de Transfer.

Si cualquier parte falla:
- rollback completo;
- no debe quedar estado financiero parcial.

La UnitOfWork:
- debe ser delgada;
- debe reutilizar el mismo DbContext/repositorios;
- no debe exponer conceptos de EF Core/PostgreSQL al Domain.

Idempotencia:
Diseñar idempotencia persistente mediante `Idempotency-Key`.

Requisitos:
- persistir la clave o estrategia equivalente;
- constraint/índice único;
- mismo key + mismo request debe devolver un resultado consistente sin repetir
  el movimiento financiero;
- mismo key + payload diferente debe producir el conflicto definido por el contrato;
- debe ser segura ante concurrencia;
- debe sobrevivir reinicios.

No resolver idempotencia únicamente con memoria del proceso.

Concurrencia:
Prevenir overspending cuando múltiples transferencias compiten por el mismo saldo.

Preferir optimistic concurrency con un mecanismo real soportado por EF Core/PostgreSQL.

Investigar y documentar la estrategia concreta para PostgreSQL.

No usar locks en memoria porque la solución debe seguir siendo correcta con múltiples
instancias del proceso.

No realizar retries ciegos de operaciones financieras sin evaluar sus efectos.

Persistencia:
Reutilizar Account del diseño existente.
Persistir Transfer con datos auditables.

Evitar duplicar repositorios o DbContexts innecesariamente.

Testing:
Además de unit tests, incorporar integration tests reales contra PostgreSQL.

Considerar Testcontainers para pruebas de integración.

Cubrir como mínimo:
- transferencia exitosa;
- fondos insuficientes;
- origen = destino;
- cuenta de otro cliente;
- cuenta bloqueada;
- rollback/atomicidad;
- idempotencia;
- mismo Idempotency-Key concurrente;
- múltiples transferencias concurrentes contra el mismo saldo;
- prevención de overspending.

Los tests deben demostrar que:
- no hay doble débito;
- no hay doble crédito;
- no hay estados parciales;
- no puede gastarse más saldo del disponible.

No usar una base in-memory como sustituto de las pruebas de concurrencia/transacciones
de PostgreSQL.

Restricciones:
No introducir:
- microservicios;
- CQRS;
- MediatR;
- Event Bus;
- RabbitMQ;
- Kafka;
- Redis;
- distributed locks;
- arquitectura distribuida nueva.

No crear un Generic Repository salvo necesidad objetiva.

Mantener la implementación horizontalmente segura sin depender de locks de memoria.

Artefactos esperados:
- plan.md;
- research.md;
- data-model.md;
- contrato OpenAPI;
- quickstart.md.

Realizar Constitution Check antes y después del diseño.
```

---

# 003 — Transferencias a terceros

## `/speckit.specify`

```text
/speckit.specify

Crea la especificación funcional para la feature:

003-transferencias-terceros

Contexto:
Esta feature extiende las capacidades 001 y 002 de la banca digital académica.
Permite transferir desde una cuenta del cliente actual hacia una cuenta de otro
cliente del mismo banco.

Objetivo:
Permitir transferencias seguras a terceros dentro de Banca Digital Perú,
manteniendo integridad financiera, privacidad, idempotencia y trazabilidad.

Reglas funcionales:

1. Cuenta origen
- Debe existir.
- Debe pertenecer al cliente actual.
- Debe ser una cuenta de ahorro.
- Debe estar activa/elegible.
- Debe tener saldo disponible suficiente.

2. Cuenta destino
- Debe existir dentro del mismo banco.
- Debe ser una cuenta de ahorro.
- Debe pertenecer a un cliente diferente del cliente actual.
- La cuenta destino no debe ser la misma cuenta origen.
- Si el usuario proporciona una cuenta propia como destino, la operación no debe
  procesarse silenciosamente como transferencia a terceros.

3. Mercado y moneda
- Operación dentro del mismo banco.
- Solo PEN.
- No CCI.
- No transferencias interbancarias.
- No conversión de moneda.

4. Identificación del destino
Definir mediante aclaración cómo se identifica funcionalmente la cuenta destino,
preferentemente mediante el identificador funcional apropiado para el cliente
(por ejemplo número de cuenta), y no por un identificador técnico interno si eso
no corresponde a la experiencia de negocio.

5. Privacidad del destinatario
La información del tercero debe limitarse al mínimo necesario para confirmar
la operación.

No exponer:
- saldo del destinatario;
- otros productos del destinatario;
- identificadores técnicos internos innecesarios;
- datos privados no requeridos.

Definir mediante aclaración qué información mínima del destinatario puede mostrarse
para que el cliente confirme que eligió la cuenta correcta.

6. Monto
- Mayor que cero.
- Precisión compatible con PEN.
- No puede superar el saldo disponible de la cuenta origen.

7. Integridad financiera
Una transferencia exitosa debe:
- debitar exactamente el monto del origen;
- acreditar exactamente el mismo monto en el destino;
- no crear ni destruir dinero;
- no cobrar comisión dentro del alcance actual.

Debe ser atómica:
- débito;
- crédito;
- registro de Transfer;

se confirman conjuntamente o se revierten conjuntamente.

8. Idempotencia
La misma solicitud lógica no debe producir más de un movimiento financiero.

La idempotencia debe:
- funcionar ante retries;
- funcionar ante solicitudes concurrentes;
- persistir tras reinicios.

9. Concurrencia
La solución debe preservar las invariantes financieras cuando:
- varias transferencias compiten por el saldo de una misma cuenta origen;
- varias transferencias acreditan concurrentemente una misma cuenta destino.

No deben producirse:
- overspending;
- lost updates;
- dobles movimientos.

10. Persistencia y trazabilidad
Registrar la transferencia con información suficiente para auditoría:
- id;
- cliente ordenante;
- cuenta origen;
- cuenta destino;
- monto;
- moneda;
- estado;
- fecha/hora;
- idempotency key cuando aplique.

11. Errores de negocio
Contemplar al menos:
- origen inexistente;
- destino inexistente;
- origen no pertenece al cliente;
- destino pertenece al propio cliente;
- origen = destino;
- saldo insuficiente;
- origen bloqueado/no elegible;
- destino bloqueado si la decisión final lo prohíbe;
- monto inválido.

Un rechazo no debe modificar balances.

Criterios de aceptación:
- Una transferencia válida a un tercero debita y acredita exactamente el mismo monto.
- El dinero total se conserva.
- Una operación rechazada no modifica balances.
- No se permite gastar más saldo del disponible.
- No se permite transferir a una cuenta propia mediante el flujo de terceros.
- No se filtra información innecesaria del destinatario.
- La misma operación idempotente no se ejecuta dos veces.
- La operación exitosa queda persistida y es auditable.
- La solución mantiene consistencia ante concurrencia.

Preguntas de aclaración:
- ¿El destino se identifica por número de cuenta u otro identificador funcional?
- ¿Qué datos mínimos del destinatario se muestran antes de confirmar?
- ¿Una cuenta destino bloqueada puede recibir fondos?
- ¿Existe una etapa explícita de previsualización y luego confirmación?
- ¿Se persisten transferencias rechazadas?
- ¿Qué formato debe tener el identificador visible de operación?
- ¿La fecha/hora se presenta en zona horaria de Perú?
- ¿Qué debe ocurrir exactamente si el usuario ingresa una de sus propias cuentas
  como destino?

Fuera de alcance:
- CCI.
- Transferencias interbancarias.
- Transferencias internacionales.
- Conversión de moneda.
- Beneficiarios frecuentes.
- Comisiones.
- Programación de transferencias.
- Autenticación real.

Genera una especificación de negocio verificable.
No agregues decisiones de framework, infraestructura o implementación.
```

## `/speckit.plan`

```text
/speckit.plan

Genera el plan técnico para la feature activa `003-transferencias-terceros`.

Esta feature es una extensión incremental de 001 y 002.

Reutiliza deliberadamente la infraestructura y decisiones ya existentes.
NO diseñes un segundo motor de transferencias.

Baseline técnico existente:
- .NET 10 / C#.
- ASP.NET Core 10.
- Clean Architecture.
- DDD pragmático.
- REST.
- API First / OpenAPI 3.1.
- Scalar.
- Entity Framework Core 10.
- PostgreSQL / Npgsql.
- FluentValidation.
- xUnit.
- Dependency Injection nativa.
- UnitOfWork delgada sobre DbContext.

Reutilización obligatoria:
Reutilizar cuando corresponda:
- Account;
- Transfer;
- Money;
- UnitOfWork;
- repositorios;
- DbContext;
- estrategia de idempotencia;
- estrategia de concurrencia;
- error handling;
- infraestructura de testing.

NO crear:
- ThirdPartyTransferRepository si TransferRepository cubre correctamente la necesidad;
- ThirdPartyUnitOfWork;
- un segundo DbContext;
- una segunda estrategia de idempotencia;
- una segunda estrategia de concurrencia;
- infraestructura paralela para la misma responsabilidad.

No separar OwnTransfer y ThirdPartyTransfer como modelos/repositorios distintos
solo por decoración.

Introducir un tipo/clasificación de transferencia únicamente si existe una necesidad
real de comportamiento, persistencia o consulta.

Application:
Diseñar un caso de uso para transferencias a terceros que coordine:
- cuenta origen del cliente;
- resolución de cuenta destino;
- verificación de diferente propietario;
- validaciones;
- privacidad;
- saldo;
- idempotencia;
- dominio;
- UnitOfWork.

Resolución de destino:
Si la spec aclarada define número de cuenta como identificador funcional del destino:
- resolver el destino mediante ese valor;
- no obligar al consumidor a conocer el UUID interno de Account;
- mantener el UUID como detalle técnico interno.

Privacidad:
Diseñar explícitamente qué información mínima puede volver al cliente.

Evaluar un modelo como `RecipientDisplayInfo` o equivalente únicamente si aporta
una frontera clara.

No devolver:
- saldo del tercero;
- otros productos;
- customerId interno;
- información privada innecesaria.

API First:
Diseñar/actualizar el contrato OpenAPI antes de tasks e implementación.

Preferir una API coherente alrededor del recurso `/transfers` y del flujo ya
definido en 002, evitando endpoints RPC como:

/executeThirdPartyTransfer

salvo que la especificación justifique expresamente otra cosa.

Reutilizar convenciones de:
- preview;
- confirmation;
- errors;
- idempotency;
- responses;
- Problem Details.

Idempotencia:
Reutilizar la estrategia persistente de 002.

Mismo `Idempotency-Key` + mismo payload:
- no repetir movimiento;
- devolver resultado consistente según contrato.

Mismo key + payload diferente:
- producir el conflicto definido.

Mantener constraint/garantía de unicidad y seguridad ante concurrencia.

UnitOfWork y atomicidad:
La confirmación debe incluir en una única transacción:
- débito de cuenta origen;
- crédito de cuenta destino de tercero;
- persistencia de Transfer.

No deben quedar estados parciales.

Concurrencia:
Además de prevenir overspending en origen, la solución debe evitar lost updates
cuando múltiples transferencias acreditan concurrentemente la misma cuenta destino.

La estrategia debe ser válida con PostgreSQL y múltiples instancias de aplicación.

No utilizar locks en memoria.

No introducir infraestructura distribuida nueva.

Testing:
Reutilizar la base de pruebas de 002 y agregar cobertura específica de terceros.

Integration tests contra PostgreSQL para:
- transferencia exitosa;
- cuenta destino inexistente;
- cuenta destino de propio cliente;
- origen sin saldo suficiente;
- estados bloqueados según spec;
- atomicidad;
- idempotencia;
- retries concurrentes;
- concurrencia sobre origen;
- múltiples créditos concurrentes sobre un mismo destino;
- privacidad de la respuesta;
- ausencia de exposición de saldo/datos internos del destinatario.

Mantener pruebas de contrato/OpenAPI.

Modelo y persistencia:
Extender el modelo actual únicamente cuando sea necesario.
No duplicar schema ni tablas sin una razón funcional.

Analizar si `Transfer` existente necesita distinguir transferencias propias y a terceros.
Si no es necesario para el comportamiento o consulta, no agregar complejidad decorativa.

Restricciones:
No introducir:
- microservicios;
- CQRS;
- MediatR;
- RabbitMQ;
- Kafka;
- Redis;
- distributed locks;
- Event Bus;
- nueva base de datos;
- infraestructura distribuida adicional.

El resultado debe ser una extensión incremental del diseño existente.

Artefactos esperados:
- plan.md;
- research.md;
- data-model.md;
- contrato OpenAPI;
- quickstart.md.

Realizar Constitution Check antes y después del diseño.
Documentar explícitamente:
- qué componentes se reutilizan de 001/002;
- qué componentes nuevos son estrictamente necesarios;
- impacto en schema/contrato;
- riesgos de privacidad, idempotencia y concurrencia.
```

---

# Resumen de secuencia utilizada

```text
/speckit.constitution
        ↓
Constitución v1.0.0
        ↓

001-consulta-productos-bancarios
/speckit.specify
        ↓
/speckit.clarify
        ↓
/speckit.plan
        ↓
/speckit.tasks
        ↓
/speckit.analyze
        ↓
/speckit.implement

002-transferencias-cuentas-propias
/speckit.specify
        ↓
/speckit.clarify
        ↓
/speckit.plan
        ↓
/speckit.tasks
        ↓
/speckit.analyze
        ↓
/speckit.implement

003-transferencias-terceros
/speckit.specify
        ↓
/speckit.clarify
        ↓
/speckit.plan
        ↓
/speckit.tasks
        ↓
/speckit.analyze
        ↓
/speckit.implement
```
