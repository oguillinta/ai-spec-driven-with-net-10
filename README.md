# Banca Digital Perú

API REST académica de banca digital para el mercado peruano, construida con **.NET 10** y **Clean
Architecture**, siguiendo un flujo de **Spec-Driven Development (SDD)** con
[GitHub Spec Kit](https://github.com/github/spec-kit): cada capacidad nace como una especificación
funcional aprobada, pasa por un plan técnico y un contrato OpenAPI *antes* de escribirse una sola
línea de código, y se implementa recién cuando ambos están validados.

> Proyecto exclusivamente académico. **No procesa dinero real ni datos reales de personas**: todo
> cliente, cuenta, tarjeta, saldo e identificador es ficticio (Principio de la
> [constitución del proyecto](.specify/memory/constitution.md)).

## Qué hace

El sistema modela tres capacidades incrementales de un banco digital, cada una en su propia
especificación bajo [`specs/`](specs/):

| Feature | Qué permite | Contrato OpenAPI |
|---|---|---|
| **001** · Consulta de productos bancarios | Listar y consultar el detalle de las cuentas de ahorro y tarjetas de débito del cliente actual. Solo lectura. | [`banking-products-v1.yaml`](specs/001-consulta-productos-bancarios/contracts/openapi/banking-products-v1.yaml) |
| **002** · Transferencias entre cuentas propias | Vista previa + confirmación de una transferencia entre dos cuentas del mismo cliente, con idempotencia y concurrencia optimista. | [`own-account-transfers-v1.yaml`](specs/002-transferencias-cuentas-propias/contracts/openapi/own-account-transfers-v1.yaml) |
| **003** · Transferencias a terceros | Igual que 002, pero hacia la cuenta de otro cliente del mismo banco, identificada por número de cuenta, con enmascaramiento del destinatario. | [`third-party-transfers-v1.yaml`](specs/003-transferencias-terceros/contracts/openapi/third-party-transfers-v1.yaml) |

No hay autenticación: el "cliente actual" se resuelve en el servidor vía configuración
(`DemoCustomer:CustomerId`), nunca a partir de un dato enviado por el cliente HTTP — decisión
explícita para mantener el alcance académico sin sacrificar el rigor de autorización server-side.

## Stack técnico

- **.NET 10** / C# 13 · ASP.NET Core 10 (Web API)
- **Entity Framework Core 10** + Npgsql sobre **PostgreSQL 17**
- **FluentValidation** (solo validación de forma/sintaxis de la entrada HTTP)
- **OpenAPI 3.1** Contract-First, servido y explorado con **Scalar**
- **xUnit** + `Testcontainers.PostgreSql` para pruebas de integración
- Sin autenticación, sin frontend (API-only)

## Arquitectura

Cuatro proyectos en `src/`, con la Dependency Rule apuntando siempre hacia adentro:

```text
BancaDigitalPeru.Api            → Composition Root (controllers, contratos HTTP, validación, manejo de errores)
BancaDigitalPeru.Application    → casos de uso + abstracciones (puertos), sin conocer HTTP ni EF Core
BancaDigitalPeru.Domain         → entidades y value objects en C# puro, sin dependencias de framework
BancaDigitalPeru.Infrastructure → EF Core, repositorios, migraciones, proveedores concretos
```

Los diagramas completos (arquitectura, componentes por feature, secuencia de una transferencia con
idempotencia/concurrencia, y el modelo entidad-relación) están en
[`docs/diagrams/`](docs/diagrams/) en formato Mermaid, listos para verse en VS Code, GitHub o
[mermaid.live](https://mermaid.live).

## Cómo ejecutar

### Prerrequisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 17 (local o vía Docker)

### 1. Levantar PostgreSQL

```bash
docker compose up -d
```

Esto crea `banca-digital-pe-db` (PostgreSQL 17, base de datos `banca_digital_pe`) en el puerto
`5432`. Si prefieres una instancia local ya instalada, basta con que exista una base de datos
llamada `banca_digital_pe`.

### 2. Configurar la cadena de conexión (nunca en `appsettings.json`)

`appsettings.json` solo tiene un placeholder (`Password=CHANGE_ME`). La contraseña real se
configura vía [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), que
sobrescribe ese valor en `Development`:

```bash
cd src/BancaDigitalPeru.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=banca_digital_pe;Username=postgres;Password=devpassword"
```

### 3. Aplicar migraciones (crea el esquema y siembra los datos ficticios de referencia)

```bash
dotnet ef database update --project src/BancaDigitalPeru.Infrastructure --startup-project src/BancaDigitalPeru.Api
```

### 4. Ejecutar la API

```bash
dotnet run --project src/BancaDigitalPeru.Api
```

- API: `http://localhost:5285/api/v1` (perfil `http`) o `https://localhost:7094/api/v1` (perfil `https`)
- Documentación interactiva (Scalar, solo en `Development`): `http://localhost:5285/scalar`
- Contratos OpenAPI estáticos: `/openapi/v1.yaml`, `/openapi/transfers-v1.yaml`, `/openapi/third-party-transfers-v1.yaml`

## Probar la API

- **Colección de Postman** lista para importar, con 87 requests cubriendo casos exitosos, de
  error, privacidad, idempotencia y un flujo end-to-end de demo: ver [`postman/README.md`](postman/README.md).
- **Manual paso a paso** por feature: `specs/*/quickstart.md`.

## Pruebas automatizadas

```bash
# Domain y Application: rápidas, sin infraestructura externa
dotnet test tests/BancaDigitalPeru.Domain.UnitTests
dotnet test tests/BancaDigitalPeru.Application.UnitTests

# Integración: requieren Docker (Testcontainers levanta su propio PostgreSQL efímero)
dotnet test tests/BancaDigitalPeru.IntegrationTests
```

## Estructura del repositorio

```text
specs/            Especificaciones SDD (spec.md, plan.md, research.md, data-model.md,
                  quickstart.md, tasks.md, contrato OpenAPI) — una carpeta por feature
src/              Código fuente (4 proyectos, Clean Architecture)
tests/            Domain.UnitTests · Application.UnitTests · IntegrationTests
docs/diagrams/    Diagramas Mermaid (arquitectura, componentes, secuencia, entidad-relación)
postman/          Colección Postman + environment + guía de uso
.specify/         Constitución del proyecto y configuración de Spec Kit
```

## Flujo Spec-Driven Development

Cada feature siguió, en orden, `/speckit.specify` → `/speckit.clarify` → `/speckit.plan` →
`/speckit.tasks` → `/speckit.analyze` → `/speckit.implement`, gobernado por la
[constitución del proyecto](.specify/memory/constitution.md) (v1.0.0): simplicidad ante todo,
alcance gobernado por la spec aprobada, Clean Architecture, integridad financiera (idempotencia +
atomicidad + concurrencia), seguridad por defecto, y pruebas automatizadas obligatorias para toda
regla de negocio crítica.
