# Quickstart: Transferencias a cuentas de terceros

**Feature**: `003-transferencias-terceros`

Guía para verificar manualmente esta feature contra el contrato
[`third-party-transfers-v1.yaml`](./contracts/openapi/third-party-transfers-v1.yaml) (vista
previa) y el contrato ya existente
[`own-account-transfers-v1.yaml`](../002-transferencias-cuentas-propias/contracts/openapi/own-account-transfers-v1.yaml)
(confirmación y consulta, reutilizados sin cambios de endpoint — research.md §10), reutilizando el
entorno ya levantado para `001`/`002` (misma Api, misma base de datos `banca_digital_pe`).

## Prerrequisitos

- Entorno de `001`/`002` ya funcionando: PostgreSQL accesible, migraciones aplicadas, Api
  corriendo.
- Migración de `003` aplicada (índice único en `accounts.number`, ajuste de `display_name` de
  Cliente A/B): `dotnet ef database update --project src/BancaDigitalPeru.Infrastructure
  --startup-project src/BancaDigitalPeru.Api`.
- Datos de referencia tras la migración de `003` (ver data-model.md/research.md §9):
  - Cliente A (`a1111111-...`) — `display_name`: "María López Torres" — Cuenta A
    (`aaaaaaaa-1111-...`, número `00123456780001`, ACTIVA, S/ 2,500.00).
  - Cliente B (`b2222222-...`) — `display_name`: "Juan Pérez García" — Cuenta B1
    (`bbbbbbbb-1111-...`, número `00123456780003`, ACTIVA, S/ 700.00 — ajustar si el seed vigente
    difiere, ver spec §6, Ejemplo de referencia).

## 1. Transferencia exitosa a un tercero (CA1, CA2, CA10)

```bash
# Paso 1: vista previa (endpoint nuevo de esta feature)
curl -s -X POST http://localhost:5080/api/v1/third-party-transfer-previews \
  -H "Content-Type: application/json" \
  -d '{"sourceAccountId":"aaaaaaaa-1111-1111-1111-111111111111","destinationAccountNumber":"00123456780003","amount":300.00}'
# -> 200, guardar "previewReference"; verificar que la respuesta incluye
#    "destinationAccountMasked":"****0003" y "destinationCustomerDisplayName":"Juan P***",
#    y que NO incluye saldo ni ningún otro producto del Cliente B (CA10).

# Paso 2: confirmar (mismo endpoint que 002 — reutilizado, research.md §10)
curl -s -X POST http://localhost:5080/api/v1/transfers \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: demo-3rdparty-001" \
  -d '{"previewReference":"{previewReference}"}'
# -> 201, TransferResult con transferId y "destinationCustomerDisplayName":"Juan P***"

# Verificar saldos actualizados
curl -s http://localhost:5080/api/v1/accounts
```

**Esperado**: Cuenta A en S/ 2,200.00 (Cliente A); consultar `GET /api/v1/accounts` como Cliente B
(fuera del alcance de este script de un solo cliente activo) debería mostrar S/ 1,000.00 — validar
mediante la prueba de integración dedicada, que sí puede consultar ambas cuentas directamente
contra la base de datos. La suma total de ambos saldos antes y después es la misma (CA2).

## 2. Cuenta destino inexistente (CA7, CL2)

```bash
curl -s -i -X POST http://localhost:5080/api/v1/third-party-transfer-previews \
  -H "Content-Type: application/json" \
  -d '{"sourceAccountId":"aaaaaaaa-1111-1111-1111-111111111111","destinationAccountNumber":"99999999999999","amount":50.00}'
```

**Esperado**: `404` con `type: .../errors/destination-not-found` (revelador por diseño, a
diferencia del origen — research.md §7). La cuenta origen conserva su saldo.

## 3. Cuenta destino perteneciente al mismo cliente (CA8, CL6)

```bash
curl -s -i -X POST http://localhost:5080/api/v1/third-party-transfer-previews \
  -H "Content-Type: application/json" \
  -d '{"sourceAccountId":"aaaaaaaa-1111-1111-1111-111111111111","destinationAccountNumber":"00123456780002","amount":50.00}'
```

**Esperado**: `422` con `type: .../errors/destination-is-own-account`. La operación no se procesa
mediante esta funcionalidad (permanece gobernada por `002`).

## 4. Saldo insuficiente (CA3)

```bash
curl -s -i -X POST http://localhost:5080/api/v1/third-party-transfer-previews \
  -H "Content-Type: application/json" \
  -d '{"sourceAccountId":"aaaaaaaa-1111-1111-1111-111111111111","destinationAccountNumber":"00123456780003","amount":999999.00}'
```

**Esperado**: `422` con `detail` indicando saldo insuficiente; ningún saldo cambia.

## 5. Cuenta origen bloqueada (CA9, CL4)

Con una cuenta propia BLOQUEADA como origen (ver seed de `001`/`002`):

```bash
curl -s -i -X POST http://localhost:5080/api/v1/third-party-transfer-previews \
  -H "Content-Type: application/json" \
  -d '{"sourceAccountId":"{cuentaBloqueadaId}","destinationAccountNumber":"00123456780003","amount":10.00}'
```

**Esperado**: `422`, mismo `type: .../errors/transfer-rejected` que una cuenta destino bloqueada.

## 6. Solicitud duplicada (CA11, CL9)

Repetir exactamente el Paso 2 del escenario 1 (mismo `Idempotency-Key: demo-3rdparty-001`, mismo
`previewReference`).

**Esperado**: `200` (no `201`) con el mismo `transferId`. Los saldos no vuelven a moverse.

## 7. Atomicidad ante error (CA12, CL11)

Igual que en `002` (mismo mecanismo, sin cambios — research.md §0): la verificación determinista
vive en `IntegrationTests` (forzar un fallo técnico antes de `SaveChangesAsync` y comprobar que
ningún saldo cambió). Verificación manual alternativa: tras cualquier intento que resulte en
`4xx`/`5xx`, volver a consultar `GET /api/v1/accounts` y confirmar que ningún saldo cambió.

## 8. Concurrencia de saldo — overspending del ordenante (CL10, sección 16 del plan)

```bash
# Igual que en 002: dos confirmaciones casi simultáneas contra la misma cuenta origen,
# cada una con su propia vista previa (de terceros) y su propio Idempotency-Key, por un
# importe que individualmente es válido pero que sumado excede el saldo disponible.
(curl -s -X POST http://localhost:5080/api/v1/transfers \
  -H "Content-Type: application/json" -H "Idempotency-Key: concurrent-3rdparty-a" \
  -d '{"previewReference":"{previewReferenceA}"}' &)
(curl -s -X POST http://localhost:5080/api/v1/transfers \
  -H "Content-Type: application/json" -H "Idempotency-Key: concurrent-3rdparty-b" \
  -d '{"previewReference":"{previewReferenceB}"}' &)
wait
```

**Esperado**: como máximo una de las dos responde `201`; la otra responde `409`
(`.../errors/concurrency-conflict`) o `422` (saldo insuficiente al revalidar). El saldo final de
la cuenta origen nunca queda negativo. Verificación determinista en `IntegrationTests`.

## 9. Concurrencia de saldo — créditos concurrentes al mismo destinatario (sección 16 del plan,
nuevo respecto de `002`)

```bash
# Dos ordenantes distintos (dos cuentas origen distintas, ambas del mismo o de clientes
# distintos) transfieren simultáneamente al mismo número de cuenta destino.
(curl -s -X POST http://localhost:5080/api/v1/third-party-transfer-previews \
  -H "Content-Type: application/json" \
  -d '{"sourceAccountId":"{cuentaOrigenA}","destinationAccountNumber":"00123456780003","amount":20.00}' &)
(curl -s -X POST http://localhost:5080/api/v1/third-party-transfer-previews \
  -H "Content-Type: application/json" \
  -d '{"sourceAccountId":"{cuentaOrigenB}","destinationAccountNumber":"00123456780003","amount":30.00}' &)
wait
# Confirmar ambas casi simultáneamente con sus respectivas referencias e Idempotency-Key propios.
```

**Esperado**: el saldo final de la cuenta destino refleja **ambos** créditos aplicados (no se
pierde ninguna actualización — research.md §2). La verificación determinista y repetible de este
escenario vive en `IntegrationTests` (dos `DbContext` reales contra el mismo Postgres, no en este
script manual).

## 10. Consultar el resultado (CA13, CA14)

```bash
curl -s http://localhost:5080/api/v1/transfers/{transferId}
```

**Esperado**: `200` con `transferId`, `completedAt`, cuentas enmascaradas,
`destinationCustomerDisplayName` (presente porque es una transferencia a un tercero), importe y
`status: COMPLETED`. Reiniciar la Api y repetir la consulta: el resultado debe ser idéntico
(CA14).

## 11. Conformidad con el contrato y privacidad de la respuesta

La verificación formal de conformidad con `third-party-transfers-v1.yaml` (vista previa) y con el
`TransferResult` ampliado de `own-account-transfers-v1.yaml` (confirmación/consulta), incluyendo
que ninguna respuesta contiene saldo, otros productos ni identificadores internos del
destinatario, se implementa como parte de `IntegrationTests` (ver `plan.md`, Testing Strategy), no
como paso manual permanente.
