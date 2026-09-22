# Research: Consulta de productos bancarios del cliente

**Feature**: `001-consulta-productos-bancarios` | **Date**: 2026-09-21

**Purpose**: El Technical Context de `plan.md` no tiene puntos `NEEDS CLARIFICATION`: el stack
completo (.NET 10, ASP.NET Core 10, PostgreSQL, EF Core 10, FluentValidation, xUnit, Scalar,
OpenAPI 3.1) fue mandado explícitamente en el input de planificación. Este documento registra,
en su lugar, las decisiones de diseño técnico que ese input dejó abiertas y que deben resolverse
antes de Phase 1 (Design & Contracts), junto con su justificación y las alternativas descartadas.

## 1. Identificadores de recursos (AccountId, DebitCardId, CustomerId)

**Decision**: Usar `Guid` (UUID v4) como identificador interno de `Customer`, `Account` y
`DebitCard`, expuesto tal cual en las rutas REST (`/accounts/{accountId}`).

**Rationale**: FR-022 exige que un identificador ajeno no permita distinguir "no existe" de "no
es tuyo". Un `Guid` no es enumerable secuencialmente (a diferencia de un entero autoincremental),
lo que refuerza en profundidad la protección contra enumeración exigida por el Principio VI de la
constitución (mínimo privilegio) además del filtrado por propietario en el repositorio.

**Alternatives considered**: enteros autoincrementales (rechazado: facilita enumeración de
identificadores válidos, aunque el filtro por cliente ya los bloquee, no es defensa en
profundidad); el número de cuenta/tarjeta enmascarado como identificador de negocio (rechazado:
el número completo nunca debe transportarse ni usarse como clave pública, ver FR-018).

## 2. Contexto del cliente actual sin autenticación

**Decision**: Definir `ICurrentCustomerProvider` en Application
(`Task<CustomerId> GetCurrentCustomerIdAsync(CancellationToken)`). La implementación en
Infrastructure resuelve el `CustomerId` de demostración desde configuración
(`appsettings.json` → sección `DemoCustomer:CustomerId`, un GUID fijo que corresponde al
"Cliente A" sembrado en la base de datos). El Composition Root (Api) inyecta esta
implementación; los use cases y los controllers nunca reciben ni aceptan un `customerId` desde
la request HTTP.

**Rationale**: Cumple la sección 9 del input de planificación: los casos de uso no confían en un
`customerId` proporcionado por el cliente HTTP, y la abstracción puede sustituirse en una
feature futura (autenticación real) por una implementación que lea el `CustomerId` del
`ClaimsPrincipal` sin tocar Application ni Domain.

**Alternatives considered**: Middleware que inyecta un header `X-Customer-Id` fijo (rechazado:
seguiría pareciendo una entrada controlada por el cliente HTTP, contradiciendo la sección 9);
valor hardcodeado directamente en el use case (rechazado: no sería sustituible sin modificar
Application, violando la preparación para autenticación futura).

## 3. Enmascaramiento de números de cuenta y de tarjeta

**Decision**: `AccountNumber` y `CardNumber` son Value Objects en Domain que almacenan el número
completo internamente pero solo exponen una propiedad `Masked` (`****` + últimos 4 dígitos). Solo
esa propiedad `Masked` viaja hacia Application/Api; el valor completo nunca se serializa en una
respuesta HTTP.

**Rationale**: Implementa FR-018/FR-019/FR-002/FR-004 (clarificado el 2026-09-20: mismo formato
de enmascaramiento para cuentas y tarjetas) en un único lugar del dominio, evitando que la lógica
de enmascaramiento se duplique o se olvide en un DTO de salida.

**Alternatives considered**: enmascarar en la capa de presentación (Api) a partir del número
completo (rechazado: exige que el número completo circule por Application y sea serializable por
error; mayor superficie de fuga de datos sensibles, contradice el Principio VI).

## 4. Mapeo de Value Objects con EF Core 10

**Decision**: Usar `OwnsOne` (owned types) para `Money` (columnas `Balance_Amount` /
`Balance_Currency`) y para `ExpirationDate` (columnas `Expiration_Month` / `Expiration_Year`).
Usar `HasConversion` (value converters) para `AccountNumber`/`CardNumber` (columna `string` con
el número completo cifrado/enmascarado a nivel de aplicación, no de columna) y para los enums de
dominio (`AccountStatus`, `CardStatus`, `CurrencyCode`, `AccountType`), persistidos como `string`
para legibilidad directa en PostgreSQL.

**Rationale**: Es el mecanismo estándar y soportado por EF Core 10 para modelar Value Objects sin
que Domain dependa de EF Core (las configuraciones `IEntityTypeConfiguration<T>` viven en
Infrastructure, no en Domain). Persistir enums como `string` evita el riesgo de que un cambio de
orden en el enum corrompa datos existentes (riesgo real con conversión ordinal por defecto).

**Alternatives considered**: serializar los Value Objects como JSON en una sola columna
(rechazado: dificulta consultas/índices y no aporta valor para este modelo simple); Data
Annotations de EF Core en las entidades de Domain (rechazado explícitamente por la sección 10 del
input de planificación — el Domain debe permanecer C# puro).

## 5. Modelo de dominio vs. modelo de persistencia

**Decision**: Usar una única representación compartida: las entidades de Domain
(`Account`, `DebitCard`, `Customer`) son también las entidades mapeadas por EF Core en
Infrastructure. No se introduce un modelo de persistencia separado (p. ej. `AccountRecord` +
mapeo manual) para esta feature.

**Rationale**: Esta feature es de solo lectura y sus invariantes (estado válido, tarjeta asociada
a una cuenta del mismo cliente, número enmascarado) ya quedan protegidos por los constructores/
factories del Domain; un modelo de persistencia paralelo añadiría una capa de mapeo sin proteger
ninguna invariante adicional. Es la alternativa más simple que satisface los requisitos
(Principio I de la constitución), y la separación de capas (Principio IV) se mantiene igualmente
porque el *mapeo* EF Core vive en Infrastructure, no en Domain — el Domain no depende de EF Core
en ningún momento, solo es *conocido* por una configuración externa a él.

**Alternatives considered**: modelo de persistencia separado desde el inicio (rechazado por
prematuro: no hay hoy una necesidad concreta que lo justifique, ver Principio I; se documenta
como candidato a introducir en `002`/`003` si las escrituras concurrentes lo requieren).

## 6. Estrategia de error HTTP para "no existe" vs. "no es tuyo"

**Decision**: Ambos casos devuelven **HTTP 404 Not Found** con el mismo cuerpo
`application/problem+json` genérico (`title: "Recurso no encontrado"`), producido por un único
tipo de resultado de aplicación (`NotFound`) que los repositorios generan de forma natural: sus
métodos de detalle (`GetByIdForCustomerAsync`) filtran en la misma consulta por
`CustomerId` **y** por el identificador del recurso, de modo que "no existe" y "existe pero es de
otro cliente" son indistinguibles ya en el resultado `null` devuelto por el repositorio.

**Rationale**: Resuelve FR-022 y SC-002 (clarificados el 2026-09-20) sin necesitar lógica
adicional en Application ni en Api para "unificar" dos caminos de error distintos — el diseño de
la consulta ya los colapsa en uno solo. Alineado con el Principio VI (denegar por defecto,
minimizar exposición).

**Alternatives considered**: consultar primero por existencia y luego por propiedad, devolviendo
404 vs. 403 según el caso (rechazado explícitamente por la clarificación de la spec: revelaría si
el identificador ajeno corresponde a un producto real).

## 7. Generación y publicación del contrato OpenAPI con Scalar

**Decision**: El archivo `contracts/openapi/banking-products-v1.yaml` es el contrato
versionado y fuente de verdad (Contract First). En Api, el documento se sirve tal cual (archivo
estático embebido/leído desde disco) en un endpoint de documentación, y Scalar
(`Scalar.AspNetCore`) se configura para consumir ese documento estático como su única fuente,
habilitado al menos en `Development`. No se usa la generación automática de OpenAPI de ASP.NET
Core (`Microsoft.AspNetCore.OpenApi`) para producir el contrato a partir de atributos de los
controllers, precisamente para que el contrato no pueda derivar silenciosamente del código.

**Rationale**: Cumple la sección 7 del input de planificación ("Scalar NO DEBE convertirse en una
fuente alternativa del contrato"; "el archivo OpenAPI versionado debe permanecer como fuente de
verdad") y el gate de la sección 6 ("la implementación todavía no ha comenzado" hasta que el
contrato exista). Evita Swashbuckle/Swagger UI, que no aporta nada que Scalar no cubra ya.

**Alternatives considered**: generar el OpenAPI desde los controllers y usarlo como contrato
(rechazado explícitamente: invierte la secuencia obligatoria Contract First de la sección 5).

## 8. Pruebas de integración contra PostgreSQL real

**Decision**: Las pruebas de integración usan una instancia PostgreSQL real levantada mediante
`Testcontainers.PostgreSql` (contenedor Docker efímero por sesión de pruebas), no un proveedor
in-memory de EF Core.

**Rationale**: Cumple explícitamente la sección 16 ("preferir una instancia PostgreSQL compatible
con el entorno productivo en lugar de sustituirla por un provider in-memory con comportamiento
diferente"). `Testcontainers.PostgreSql` es la forma estándar de obtener esa instancia real de
forma reproducible en CI sin depender de un servidor PostgreSQL preexistente. Es una dependencia
nueva, pero justificada por un requisito explícito del plan (no hay alternativa que use Postgres
real sin gestionar un contenedor).

**Alternatives considered**: proveedor `Microsoft.EntityFrameworkCore.InMemory` (rechazado
explícitamente por el input); PostgreSQL compartido y persistente para pruebas (rechazado: no es
reproducible ni aislado entre ejecuciones/CI).

## 9. Value Object `Money` y moneda

**Decision**: `Money` es un Value Object (`decimal Amount`, `CurrencyCode Currency`) con
redondeo/formato a 2 decimales impuesto en su construcción. `CurrencyCode` es un enum con un único
miembro (`PEN`) en esta versión, documentado como intencionalmente cerrado hasta que una spec
futura requiera otra moneda (RB2 / FR-005).

**Rationale**: Money como VO evita "primitive obsession" en el saldo (RB3: dos decimales
obligatorios) y dejaría la puerta abierta a más monedas sin rediseñar `Account`; usar un enum en
vez de una tabla de monedas es la alternativa más simple que satisface el alcance actual
(Principio I).

**Alternatives considered**: modelar `Currency` como entidad de catálogo en base de datos
(rechazado por prematuro: la spec fija PEN como único valor soportado, RB2).
