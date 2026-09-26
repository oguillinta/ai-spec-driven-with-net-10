# Digital Banking API — Colección de Postman

Colección de validación funcional del backend de **Banca Digital Perú** (proyecto académico SDD),
construida exclusivamente a partir de los contratos OpenAPI existentes y la implementación real de
las tres features del backend:

- `001-consulta-productos-bancarios` — [`banking-products-v1.yaml`](../specs/001-consulta-productos-bancarios/contracts/openapi/banking-products-v1.yaml)
- `002-transferencias-cuentas-propias` — [`own-account-transfers-v1.yaml`](../specs/002-transferencias-cuentas-propias/contracts/openapi/own-account-transfers-v1.yaml)
- `003-transferencias-terceros` — [`third-party-transfers-v1.yaml`](../specs/003-transferencias-terceros/contracts/openapi/third-party-transfers-v1.yaml)

No se inventó ningún endpoint, request DTO ni código HTTP: todo se verificó contra el código fuente
en `src/` (controllers, validators, error handling) y contra los datos ficticios realmente presentes
en la base de datos local (`banca_digital_pe`).

> **Nota de mantenimiento**: los valores de `local.postman_environment.json` reflejan el contenido
> real de la base de datos local al momento de generar esta colección, verificado por consulta
> directa (`psql`), no los valores originales de las migraciones de EF Core. Si alguien vuelve a
> editar manualmente los datos (o restablece la base de datos a partir de las migraciones), estos
> valores quedarán desactualizados otra vez — ver "Discrepancias y limitaciones", punto 7.

## Importación

1. Abrir Postman → **Import**.
2. Arrastrar (o seleccionar) ambos archivos de esta carpeta:
   - `digital-banking-api.postman_collection.json` (Postman Collection Schema v2.1)
   - `local.postman_environment.json`
3. Postman creará la colección **Digital Banking API** y el environment **Digital Banking API -
   Local**.

## Configuración

En la esquina superior derecha de Postman, seleccionar el environment **Digital Banking API -
Local** antes de ejecutar cualquier request.

### Backend

La variable `host` (por defecto `http://localhost:5285`) apunta al perfil `http` de
`src/BancaDigitalPeru.Api/Properties/launchSettings.json`. `baseUrl` se deriva de `host`
(`{{host}}/api/v1`) y es la que usan todas las requests de negocio; ninguna request tiene la URL
hardcodeada.

Para ejecutar la colección:

```bash
dotnet run --project src/BancaDigitalPeru.Api
```

Si se prefiere el perfil HTTPS (`https://localhost:7094`), editar la variable `host` en el
environment.

**Autenticación**: el proyecto no implementa autenticación (constitución, alcance de 001/002/003).
Por eso la colección se configuró explícitamente con `"auth": {"type": "noauth"}` a nivel de
colección y ninguna request agrega un header `Authorization`, API Key ni OAuth. `currentCustomerId`
es puramente informativo (el cliente actual lo resuelve el backend vía `DemoCustomer:CustomerId` en
`appsettings.json`, no hay forma de cambiarlo desde una request HTTP).

## Datos de prueba

**Origen de las migraciones** (definen el esquema y el seed *original*; ver punto 7 de
"Discrepancias" sobre por qué los valores actuales difieren de este seed original):

| Migración | Qué siembra |
|---|---|
| `InitialCreate` (001) | Cliente A y Cliente B |
| `AddAccounts` (001) | 3 cuentas de ahorro |
| `AddDebitCards` (001) | 3 tarjetas de débito |
| `AddTransfersAndAccountConcurrencyToken` (002) | Actualiza el estado de una cuenta de BLOQUEADA a ACTIVA |
| `AddThirdPartyTransferSupport` (003) | Actualiza los nombres de Cliente A/B a "Nombre Apellido" para que el enmascaramiento de destinatario tenga sentido |

**Estado real de los datos** (cliente actual: **Cliente A / María López Torres**,
`DemoCustomer:CustomerId` en `appsettings.json`), verificado por consulta directa a
`banca_digital_pe` — estos son los valores que usa la colección, **no** los de la migración original:

| Variable | Valor | Descripción |
|---|---|---|
| `currentCustomerId` | `3f8c6b5a-7d21-4e9f-a634-2c1d8b7e9054` | Cliente A ("María López Torres") — cliente actual |
| `ownSourceAccountId` / `ownSourceAccountNumber` | `7d42ec1b-62fc-4e69-91b8-94764d7ea0d3` / `19100012345678` | Cuenta de ahorro de Cliente A, `****5678`, **575.00 PEN**, ACTIVA |
| `ownDestinationAccountId` / `ownDestinationAccountNumber` | `b634930c-74f1-49b8-a9df-e37a7b687950` / `19100087654321` | Segunda cuenta de ahorro de Cliente A, `****4321`, **2600.00 PEN**, ACTIVA |
| `debitCardId` | `9ab2869f-b77d-4614-ae94-944395d65781` | Tarjeta ACTIVA de Cliente A, termina en `4582`, asociada a `ownSourceAccountId` |
| `blockedDebitCardId` | `f67f06b5-895c-47ea-9552-e635dbbc98fc` | Tarjeta **BLOQUEADA** de Cliente A, termina en `9911`, asociada a `ownDestinationAccountId` |
| `thirdPartyDestinationAccountId` / `thirdPartyDestinationAccountNumber` | `42df0f86-b848-46bd-82d0-3d59ce1a7589` / `19100099887766` | Cuenta de ahorro de Cliente B ("Juan Pérez García"), `****7766`, **1625.00 PEN**, ACTIVA |
| `thirdPartyDebitCardId` | `3187c34b-beac-40a8-9028-4816bbc9fe45` | Tarjeta ACTIVA de Cliente B, termina en `7634`, asociada a `thirdPartyDestinationAccountId` |
| `unknownAccountId` | `99999999-…` | GUID válido que no existe en la base de datos |
| `invalidAccountId` | `not-a-valid-guid` | Cadena que no es un GUID |
| `nonexistentDestinationAccountNumber` | `00199999999999` | Número de cuenta con formato válido que no existe |
| `invalidDestinationAccountNumber` | `AB12CD34EFGH` | Número de cuenta con formato inválido (contiene letras) |

Los saldos `575.00`/`2600.00`/`1625.00` **no son los saldos originales del seed** (`2500.00`/
`800.00`/`1500.00`): son el resultado de operaciones de transferencia ya ejecutadas contra esta
base de datos antes de la edición manual. Se documentan tal como están porque son los valores
reales actuales, no porque sean "redondos" — cada vez que se ejecuten `02`/`03 > Success` o
`Idempotency`, volverán a cambiar (ver "Orden de ejecución").

Los importes (`validOwnTransferAmount=50.00`, `validThirdPartyTransferAmount=25.00`,
`insufficientFundsAmount=999999.00`, `invalidScaleAmount=10.123`) se eligieron deliberadamente
pequeños (para poder ejecutar la colección varias veces sin agotar los saldos sembrados) o
absurdamente grandes/imprecisos (para forzar el rechazo correspondiente sin ambigüedad).

## Orden de ejecución

| Carpeta | Modifica saldos | Puede ejecutarse independiente |
|---|---|---|
| `00 - Health / Setup` | No | Sí |
| `01 - Banking Products > Success` / `Error Cases` | No | Sí |
| `02 - Own Account Transfers > Business Errors` / `Validation Errors` | No (toda solicitud se rechaza antes de aplicar movimiento) | Sí |
| `02 - Own Account Transfers > Success` | **Sí** | Sí, pero deja los saldos de `ownSourceAccountId`/`ownDestinationAccountId` modificados para el resto de la sesión |
| `02 - Own Account Transfers > Idempotency` | **Sí** (una vez) | Sí — las requests dentro de la carpeta deben ejecutarse en orden entre sí |
| `03 - Third-Party Transfers > Business Errors` / `Validation Errors` | No | Sí |
| `03 - Third-Party Transfers > Success` | **Sí** | Sí, en orden interno |
| `03 - Third-Party Transfers > Privacy` | No | Los dos primeros requests sí; el tercero (`Confirmed third-party transfer result…`) requiere haber ejecutado antes `03 > Success` |
| `03 - Third-Party Transfers > Idempotency` | **Sí** (una vez) | Sí — en orden interno, independiente de `02 > Idempotency` (usa `secondIdempotencyKey`) |
| `90 - End-to-End Demo` | **Sí** (dos transferencias legítimas) | Debe ejecutarse completa y en orden con el **Collection Runner** |

**Para la demo en vivo**: usar `90 - End-to-End Demo` con el Collection Runner, en orden, de
principio a fin. Antes de una presentación, se recomienda reiniciar la base de datos (ver abajo)
para partir siempre de los saldos documentados arriba.

**Cómo restablecer datos**: el proyecto no expone ningún endpoint de reset. El mecanismo real
disponible es el de EF Core: `dotnet ef database drop` seguido de `dotnet ef database update`
(o eliminar y recrear el contenedor/instancia de PostgreSQL) desde `src/BancaDigitalPeru.Api`,
que vuelve a aplicar todas las migraciones — incluido el seed de datos ficticios — desde cero.

## Idempotencia

- `idempotencyKey` se genera una única vez (con `{{$guid}}` vía `pm.variables.replaceIn`, en un
  pre-request script) al ejecutar la **primera** request de `02 - Own Account Transfers >
  Idempotency`, y se reutiliza en el resto de esa carpeta para demostrar: (a) que reenviar la misma
  clave con el mismo `previewReference` devuelve el mismo `transferId` sin duplicar el movimiento
  (`200`, no un segundo `201`), y (b) que reutilizar la misma clave con un `previewReference`
  distinto produce `409` (`idempotency-conflict`) según el contrato.
- `secondIdempotencyKey` sigue exactamente el mismo patrón dentro de `03 - Third-Party Transfers >
  Idempotency`, deliberadamente **separada** de `idempotencyKey` para que ambas carpetas de
  idempotencia puedan ejecutarse sin interferir entre sí (ver "Independencia entre escenarios" en
  el propio input de planificación).
- `90 - End-to-End Demo` genera su propia `e2eIdempotencyKey` (paso 4b) y la reutiliza en el paso 11
  para la demostración de "no doble débito" del paso 12.

## Expected Results

### 01 - Banking Products

- **Success** (6 requests): listar cuentas, detalle de cada cuenta propia, listar tarjetas, detalle
  de tarjeta activa, detalle de tarjeta bloqueada (sigue visible, RF-014). Valida: 200, JSON,
  campos obligatorios del schema, moneda PEN, enmascaramiento (`****NNNN` para cuentas, `**** ****
  **** NNNN` para tarjetas), ausencia de CVV/PIN.
- **Error Cases** (6 requests): formato de id inválido (400), id inexistente (404), id de otro
  cliente (404, **idéntico** al anterior — FR-022/HU5), para cuentas y para tarjetas.

### 02 - Own Account Transfers

- **Success** (7 requests): flujo completo preview → confirm → verificación de balances → consulta
  por id. Verifica `sourceAfter = sourceBefore - amount`, `destinationAfter = destinationBefore +
  amount`, moneda PEN, `status = COMPLETED`.
- **Business Errors** (7 requests): fondos insuficientes (con verificación de balance
  inalterado), origen = destino, destino ajeno, origen ajeno, cuenta inexistente. Todos `404` o
  `422` según corresponda — nunca se asumió un código.
- **Validation Errors** (6 requests): escala decimal no soportada (400), importe cero/negativo
  (**422**, no 400 — es una regla de Application/Domain, documentado explícitamente en cada
  request), `sourceAccountId` malformado en el cuerpo (400 con la forma **automática** de ASP.NET
  Core, distinta del `ProblemDetails` propio — ver "Discrepancias"), `previewReference` faltante
  (400), header `Idempotency-Key` faltante (400).
- **Idempotency** (10 requests): replay exitoso sin duplicar el movimiento, conflicto por clave
  reutilizada con payload distinto (409).

### 03 - Third-Party Transfers

- **Success** (5 requests): preview (con nombre de destinatario enmascarado y número de cuenta
  enmascarado) → confirm → verificación del saldo del **ordenante** → consulta por id (incluye
  `destinationCustomerDisplayName`).
- **Business Errors** (7 requests): fondos insuficientes, destino inexistente (`404
  destination-not-found`, deliberadamente distinto de "cuenta ajena" — HU3/FR-024), destino es una
  cuenta propia (`422 destination-is-own-account`), origen ajeno (`404 not-found` genérico),
  formato de número de cuenta inválido (400).
- **Validation Errors** (4 requests): escala decimal, importe cero/negativo (422, misma nota que en
  002), `sourceAccountId` malformado (discrepancia de forma, ver abajo).
- **Privacy** (3 requests): la vista previa nunca expone saldo/otros productos/`customerId` interno
  del destinatario ni el número completo de su cuenta; no se puede ver el detalle de la cuenta del
  destinatario aunque se conozca su id; el resultado de la transferencia confirmada tampoco filtra
  esos datos.
- **Idempotency** (8 requests): mismo patrón que 002, con `secondIdempotencyKey`.

### 90 - End-to-End Demo

15 requests que reproducen, en orden, los 12 pasos pedidos para una demostración en vivo: consulta
de productos → transferencia propia → verificación de balances → transferencia a un tercero →
verificación → intento con fondos insuficientes (rechazado, balance inalterado) → repetición con la
misma `Idempotency-Key` → verificación de que no hubo doble débito.

## Discrepancias y limitaciones

Documentadas aquí en vez de modificar código, specs o contratos, tal como exige el encargo:

1. **No existe actualmente ninguna cuenta sembrada en estado `BLOCKED`.** La única cuenta bloqueada
   sembrada en `001` (`AddAccounts`, `aaaaaaaa-2222-…`) fue actualizada a `ACTIVE` por la migración
   `AddTransfersAndAccountConcurrencyToken` de `002`, para poder demostrar transferencias entre
   cuentas propias con ambas cuentas activas. Consecuencia: **no fue posible crear** las requests
   "cuenta origen bloqueada" / "cuenta destino bloqueada" para 002 ni 003 a nivel de esta colección
   HTTP (sí existe una tarjeta bloqueada, `blockedDebitCardId`, usada en `01 - Banking Products`).
   Esta regla de negocio sigue estando implementada y cubierta por los tests automatizados del
   backend (`Application.UnitTests`), simplemente no es demostrable con los datos actualmente
   sembrados en la base de datos real. No se creó un seed nuevo para resolverlo, conforme a la
   restricción explícita de no crear seeds nuevos.
2. **El saldo final del destinatario en una transferencia a terceros no es observable desde esta
   colección.** El backend no expone ningún endpoint que permita consultar el saldo de una cuenta
   perteneciente a otro cliente (eso sería, precisamente, la fuga de privacidad que 001/003
   prohíben), y no existe autenticación ni forma de "actuar como" otro cliente vía HTTP dentro de
   una misma instancia en ejecución — el cliente actual lo fija `DemoCustomer:CustomerId` en la
   configuración del servidor, no una request. Por lo tanto, `03 - Third-Party Transfers > Success`
   solo verifica `sourceAfter = sourceBefore - amount`; la verificación de
   `destinationAfter = destinationBefore + amount` para terceros no se incluyó porque no existe una
   forma legítima de observarla sin violar el contrato de privacidad.
3. **`sourceAccountId`/`destinationAccountId` malformados en el CUERPO de una solicitud de vista
   previa no producen el `ProblemDetails` personalizado del resto de la API.** Esos campos están
   tipados como `Guid` en los contratos C# (`TransferPreviewRequest`, `ThirdPartyTransferPreviewRequest`),
   por lo que un valor no-GUID falla en el *model binding* automático de ASP.NET Core antes de
   llegar a FluentValidation. La respuesta sigue siendo `400`, pero con el `ValidationProblemDetails`
   por defecto del framework (un diccionario `errors`), no con el `type`/`title`/`detail` que usa el
   resto de los errores 400 de la API (los cuales sí pasan por `ApiProblemDetails`/
   `TransferOutcomeMapping`). Las requests correspondientes (`[400] … malformed sourceAccountId (not
   a GUID)` en 02 y 03) documentan esto explícitamente en su descripción y solo comprueban la forma
   real de la respuesta (presencia de `errors`, `status: 400`), no la forma del `ProblemDetails`
   propio. Este comportamiento no se modificó: se documenta tal como existe hoy.
4. **`amount = 0` y `amount` negativo no son errores de validación (400) sino de negocio (422).**
   `TransferPreviewRequestValidator`/`ThirdPartyTransferPreviewRequestValidator` (FluentValidation)
   solo verifican la precisión decimal (máximo 2 decimales); la regla "el importe debe ser mayor
   que cero" la aplican los casos de uso de Application/Domain, devolviendo `TransferRejected`
   (`422`). Las requests correspondientes se dejaron en la carpeta "Validation Errors" (por
   coincidir con la organización pedida) pero su título y sus assertions reflejan el código real
   (`422`), no `400`, y su descripción explica la razón.
5. **No se incluyó ningún caso de error `500`.** El backend no expone ningún endpoint ni mecanismo
   de prueba soportado para forzar una excepción no controlada; crear uno artificialmente habría
   requerido modificar código fuente, fuera del alcance de este encargo.
6. **No existe un endpoint de *health check* dedicado.** `00 - Health / Setup` usa
   `GET /openapi/v1.yaml` (servido únicamente en `Development`, ver `Program.cs`) como verificación
   de conectividad, y `GET /accounts`/`GET /debit-cards` como verificación de integridad del seed.
7. **Los datos de la base de datos local fueron editados manualmente**, con nuevos GUIDs y números
   de cuenta/tarjeta distintos de los sembrados originalmente por las migraciones (`aaaaaaaa-…`,
   `00123456780001`, etc.). Esto rompió temporalmente el backend en ejecución: `DemoCustomer:CustomerId`
   en `appsettings.json` seguía apuntando al `CustomerId` original, que ya no existía en la tabla
   `customers`, por lo que `GET /accounts`/`GET /debit-cards` devolvían `200 OK` con `[]` (sin
   error) para el cliente configurado. Se corrigió actualizando `appsettings.json` al nuevo
   `CustomerId` real, y esta colección se regeneró con los valores actuales de la base de datos
   (tabla en la sección "Datos de prueba"). **Los `IntegrationTests` del backend no se vieron
   afectados** por esta edición manual: usan `Testcontainers.PostgreSql`, que levanta una instancia
   de PostgreSQL efímera y propia por ejecución, aplicando las migraciones (con su seed *original*)
   desde cero — nunca se conectan a `banca_digital_pe`. Por eso esta colección se actualizó para
   reflejar los datos reales de la base de datos local, mientras que los tests de integración
   siguen usando (correctamente) los GUIDs originales de las migraciones y no requieren cambios.

## Calidad y validación de la colección

- JSON válido (`JSON.parse` sin errores) y conforme a Postman Collection Schema v2.1
  (`https://schema.getpostman.com/json/collection/v2.1.0/collection.json`).
- 87 requests en total, agrupadas en las 5 carpetas de primer nivel pedidas.
- Todas las variables `{{...}}` referenciadas en la colección están declaradas en
  `local.postman_environment.json` o son variables dinámicas nativas de Postman (`{{$guid}}`);
  verificado automáticamente al generar la colección (el script de generación aborta si detecta una
  variable sin declarar).
- Ninguna URL usa `http://localhost` directamente: todas usan `{{baseUrl}}` o `{{host}}`.
- Ningún identificador de cuenta/tarjeta/cliente está hardcodeado fuera del environment.
- Un test a nivel de colección (se ejecuta después de cada request) verifica que ninguna respuesta
  filtre detalles internos (`StackTrace`, excepciones de Npgsql/EF Core, `at BancaDigitalPeru.`),
  reforzando el Principio VI de la constitución en las 87 requests sin repetir el assertion en cada
  una.
- Las comparaciones de saldo usan una función auxiliar `toCents()` (redondeo a centavos enteros)
  para evitar comparaciones frágiles de punto flotante, tal como exige el encargo.
