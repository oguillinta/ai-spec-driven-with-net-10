# Quickstart: Consulta de productos bancarios del cliente

**Feature**: `001-consulta-productos-bancarios`

Guía para levantar el entorno local y verificar manualmente que la feature cumple los criterios
de aceptación de `spec.md` (CA1–CA12) contra el contrato
[`banking-products-v1.yaml`](./contracts/openapi/banking-products-v1.yaml). No sustituye a las
pruebas automatizadas (`tasks.md`/`Testing Strategy`), es la guía de validación manual end-to-end.

## Prerrequisitos

- .NET 10 SDK instalado.
- Docker (para PostgreSQL local y para las pruebas de integración con Testcontainers).
- Puerto `5432` libre (o ajustar el connection string) y puerto HTTP de la Api libre (por
  convención `5080`/`5081`, a confirmar en `appsettings.Development.json` cuando exista).

## 1. Levantar PostgreSQL local

```bash
docker run --name banca-digital-pe-db -e POSTGRES_PASSWORD=devpassword \
  -e POSTGRES_DB=banca_digital_pe -p 5432:5432 -d postgres:17
```

## 2. Aplicar migraciones y datos ficticios de referencia

```bash
dotnet ef database update --project src/BancaDigitalPeru.Infrastructure --startup-project src/BancaDigitalPeru.Api
```

La migración inicial DEBE sembrar los datos de referencia descritos en `spec.md` §6:

- **Cliente A**: 2 cuentas de ahorro (una `ACTIVE` con S/ 2,500.00, una `BLOCKED` con S/ 800.00) y
  2 tarjetas de débito (una `ACTIVE` asociada a la primera cuenta, una `BLOCKED` asociada a la
  segunda).
- **Cliente B**: al menos una cuenta y una tarjeta propias, usadas únicamente para verificar que
  el Cliente A no puede acceder a ellas (CA4, CA8).

El `CustomerId` del Cliente A debe coincidir con el valor configurado en
`DemoCustomer:CustomerId` (ver `research.md` §2) para que la Api lo resuelva como "cliente
actual".

## 3. Ejecutar la Api

```bash
dotnet run --project src/BancaDigitalPeru.Api
```

Documentación interactiva (Scalar, sirviendo el contrato estático
`banking-products-v1.yaml`) disponible en `Development` en `/scalar/v1`.

## 4. Escenarios de validación manual (mapeados a los criterios de aceptación)

Reemplazar `{accountId}` / `{debitCardId}` por los GUIDs reales sembrados en el paso 2.

| # | Comando | Resultado esperado | Criterio verificado |
|---|---|---|---|
| 1 | `curl http://localhost:5080/api/v1/accounts` | 200, array con las 2 cuentas del Cliente A (una `ACTIVE`, una `BLOCKED`), cada una con `maskedNumber`, `balance` (PEN, 2 decimales) y `status` | CA1, CA3, CA11 |
| 2 | `curl http://localhost:5080/api/v1/accounts/{accountIdCuenta1DelClienteA}` | 200, detalle con los mismos campos que el listado | CA2 |
| 3 | `curl http://localhost:5080/api/v1/accounts/{accountIdDeUnaCuentaDelClienteB}` | 404, `ProblemDetails` genérico ("Recurso no encontrado") | CA4 |
| 4 | `curl http://localhost:5080/api/v1/accounts/00000000-0000-0000-0000-000000000000` | 404, **mismo** `ProblemDetails` genérico que el paso 3 (cuerpo indistinguible) | FR-022, SC-002 |
| 5 | `curl http://localhost:5080/api/v1/debit-cards` | 200, array con las 2 tarjetas del Cliente A, `maskedNumber` con solo los últimos 4 dígitos visibles, `accountId` apuntando a una cuenta del propio Cliente A | CA5, CA7, CA9, CA10 |
| 6 | `curl http://localhost:5080/api/v1/debit-cards/{debitCardIdDelClienteA}` | 200, detalle con los mismos campos que el listado | CA6 |
| 7 | `curl http://localhost:5080/api/v1/debit-cards/{debitCardIdDeUnaTarjetaDelClienteB}` | 404, mismo `ProblemDetails` genérico que los pasos 3/4 | CA8 |
| 8 | Reiniciar el contenedor de PostgreSQL (`docker restart banca-digital-pe-db`) y repetir el paso 1 de esta tabla | Mismos datos, mismos saldos y estados que antes del reinicio | CA12 |

## 5. Casos límite (opcional, requiere datos adicionales de seed)

- Sembrar un Cliente C sin cuentas ni tarjetas y apuntar `DemoCustomer:CustomerId` a su id:
  `GET /accounts` y `GET /debit-cards` deben devolver `200` con `[]` (CL1, CL2).
- Verificar que una cuenta con saldo `0.00` sigue apareciendo en el listado (CL3).
- Verificar que un cliente con todos sus productos `BLOCKED` los sigue viendo con ese estado
  (CL4).

## 6. Conformidad con el contrato

Antes de dar la feature por completa, validar que las respuestas reales cumplen
`banking-products-v1.yaml` (gate de la sección 6 del plan):

```bash
# Ejemplo con un validador de esquemas OpenAPI (p. ej. Spectral o un test de conformidad en
# IntegrationTests que compare la respuesta real contra el schema del contrato).
```

La verificación formal de conformidad se implementa como parte de `IntegrationTests`
(`Testing Strategy` en `plan.md`), no como paso manual permanente.
