# Quickstart: Transferencias entre cuentas propias

**Feature**: `002-transferencias-cuentas-propias`

Guía para verificar manualmente esta feature contra el contrato
[`own-account-transfers-v1.yaml`](./contracts/openapi/own-account-transfers-v1.yaml), reutilizando
el entorno ya levantado para `001` (misma Api, misma base de datos `banca_digital_pe`).

## Prerrequisitos

- Entorno de `001` ya funcionando (ver `specs/001-consulta-productos-bancarios/quickstart.md`):
  PostgreSQL accesible, migraciones de `001` aplicadas, Api corriendo.
- Migraciones de `002` aplicadas (tabla `transfers`, `xmin` como concurrency token en `accounts`):
  `dotnet ef database update --project src/BancaDigitalPeru.Infrastructure --startup-project src/BancaDigitalPeru.Api`.
- Datos de referencia del Cliente A (spec `001` §6): Cuenta A (`aaaaaaaa-1111-...`, ACTIVA,
  S/ 2,500.00) y Cuenta B (`aaaaaaaa-2222-...`, ACTIVA/BLOQUEADA según el seed vigente).
  Para esta feature, verificar/ajustar el seed para que ambas cuentas de referencia estén
  **ACTIVAS** con saldos que permitan los escenarios siguientes (p. ej. Cuenta A con S/ 2,500.00 y
  Cuenta B con S/ 800.00, ambas ACTIVAS — ver spec §6, Ejemplo de referencia).

## 1. Transferencia exitosa (CA1, CA2)

```bash
# Paso 1: vista previa
curl -s -X POST http://localhost:5080/api/v1/transfer-previews \
  -H "Content-Type: application/json" \
  -d '{"sourceAccountId":"aaaaaaaa-1111-1111-1111-111111111111","destinationAccountId":"aaaaaaaa-2222-2222-2222-222222222222","amount":300.00}'
# -> 200, guardar "previewReference" de la respuesta

# Paso 2: confirmar (reemplazar {previewReference} y usar una clave nueva)
curl -s -X POST http://localhost:5080/api/v1/transfers \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: demo-key-001" \
  -d '{"previewReference":"{previewReference}"}'
# -> 201, TransferResult con transferId

# Verificar saldos actualizados
curl -s http://localhost:5080/api/v1/accounts
```

**Esperado**: Cuenta A en S/ 2,200.00, Cuenta B en S/ 1,100.00 (spec §6). La suma total de ambos
saldos antes y después es la misma (CA2).

## 2. Saldo insuficiente (CA3)

```bash
curl -s -X POST http://localhost:5080/api/v1/transfer-previews \
  -H "Content-Type: application/json" \
  -d '{"sourceAccountId":"aaaaaaaa-2222-2222-2222-222222222222","destinationAccountId":"aaaaaaaa-1111-1111-1111-111111111111","amount":999999.00}'
```

**Esperado**: `422` con `detail` indicando saldo insuficiente; los saldos de ambas cuentas no
cambian.

## 3. Solicitud duplicada (CA10)

Repetir exactamente el Paso 2 del escenario 1 (mismo `Idempotency-Key: demo-key-001`, mismo
`previewReference` u otro que decodifique a los mismos `sourceAccountId`/`destinationAccountId`/
`amount`).

**Esperado**: `200` (no `201`) con el mismo `transferId` que la primera vez. Los saldos de la
Cuenta A y B **no** vuelven a moverse una segunda vez.

Repetir con el mismo `Idempotency-Key: demo-key-001` pero un `amount` distinto en la vista previa
usada.

**Esperado**: `409` con `type: .../errors/idempotency-conflict`.

## 4. Atomicidad ante error (CL7)

Este escenario requiere forzar un fallo técnico durante `SaveChangesAsync` (p. ej. desconectar la
base de datos justo antes de confirmar, o cubrirlo mediante la prueba de integración dedicada —
ver `tasks.md`, Testing Strategy). Verificación manual alternativa: tras cualquier intento de
transferencia que resulte en `4xx`/`5xx`, volver a consultar `GET /api/v1/accounts` y confirmar que
ningún saldo cambió respecto del estado previo al intento.

## 5. Concurrencia (sección 12 del plan)

```bash
# Ejecutar dos confirmaciones casi simultáneas contra la misma cuenta origen,
# cada una con su propia vista previa y su propio Idempotency-Key, por un importe
# que individualmente es válido pero que sumado excede el saldo disponible.
(curl -s -X POST http://localhost:5080/api/v1/transfers \
  -H "Content-Type: application/json" -H "Idempotency-Key: concurrent-a" \
  -d '{"previewReference":"{previewReferenceA}"}' &)
(curl -s -X POST http://localhost:5080/api/v1/transfers \
  -H "Content-Type: application/json" -H "Idempotency-Key: concurrent-b" \
  -d '{"previewReference":"{previewReferenceB}"}' &)
wait
```

**Esperado**: como máximo una de las dos responde `201`; la otra responde `409`
(`.../errors/concurrency-conflict`) o, si su vista previa ya no es válida al revalidar, `422`
(saldo insuficiente). El saldo final de la cuenta origen nunca queda negativo. La verificación
automatizada y determinística de este escenario vive en `IntegrationTests` (ver `tasks.md`), no en
este script manual (una prueba de carrera con `curl` no garantiza el mismo timing en cada
ejecución).

## 6. Consultar el resultado (US4, CA11, CA12)

```bash
curl -s http://localhost:5080/api/v1/transfers/{transferId}
```

**Esperado**: `200` con `transferId`, `completedAt`, cuentas enmascaradas, importe y `status:
COMPLETED`. Reiniciar la Api y repetir la consulta: el resultado debe ser idéntico (CA12).

## 7. Conformidad con el contrato

La verificación formal de conformidad con `own-account-transfers-v1.yaml` se implementa como parte
de `IntegrationTests` (ver `plan.md`, Testing Strategy), no como paso manual permanente.
