# Diagramas de arquitectura — Banca Digital Perú

Diagramas en formato [Mermaid](https://mermaid.js.org/) que documentan la arquitectura real del
backend, construidos a partir del código fuente en `src/` y las migraciones de EF Core en
`src/BancaDigitalPeru.Infrastructure/Migrations/` — no son un diseño aspiracional, describen el
sistema tal como está implementado hoy (features `001`, `002` y `003`).

## Archivos

| Archivo | Diagrama | Qué muestra |
|---|---|---|
| [`01-architecture.mmd`](./01-architecture.mmd) | Arquitectura | Las 4 capas de Clean Architecture (Domain/Application/Infrastructure/Api), la Dependency Rule, y `Program.cs` como único Composition Root. |
| [`02-components.mmd`](./02-components.mmd) | Componentes | Controllers, casos de uso, validadores y repositorios, agrupados por feature (001/002/003) y resaltando el núcleo compartido entre 002 y 003. |
| [`03a-sequence-list-accounts.mmd`](./03a-sequence-list-accounts.mmd) | Secuencia | `GET /api/v1/accounts` — flujo de lectura simple (feature 001). |
| [`03b-sequence-confirm-transfer.mmd`](./03b-sequence-confirm-transfer.mmd) | Secuencia | `POST /api/v1/transfers` — la operación más compleja del sistema: idempotencia, clasificación propia/tercero y los dos tipos de conflicto (concurrencia optimista vía `xmin`, colisión de `Idempotency-Key`). Compartido por 002 y 003. |
| [`04-entity-relationship.mmd`](./04-entity-relationship.mmd) | Entidad-relación | Esquema real de la base de datos `banca_digital_pe` (`customers`, `accounts`, `debit_cards`, `transfers`), con claves únicas y el token de concurrencia. |

## Cómo visualizarlos

- **VS Code**: instalar la extensión "Markdown Preview Mermaid Support" o "Mermaid Preview" y abrir
  cualquier `.mmd` directamente, o pegar el contenido en un bloque ```` ```mermaid ```` dentro de un
  `.md`.
- **GitHub/GitLab**: pegar el contenido en un bloque de código ` ```mermaid ` dentro de cualquier
  `.md` del repositorio — ambos lo renderizan automáticamente al ver el archivo.
- **Mermaid Live Editor**: pegar el contenido en <https://mermaid.live> para exportarlo como SVG/PNG.
- **Mermaid CLI** (`@mermaid-js/mermaid-cli`, si se desea generar imágenes estáticas):
  ```bash
  npx -p @mermaid-js/mermaid-cli mmdc -i 01-architecture.mmd -o 01-architecture.svg
  ```

## Relación con el resto del repositorio

Estos diagramas son documentación derivada, no una fuente de verdad adicional:

- El contrato HTTP de cada feature sigue siendo el OpenAPI en
  `specs/*/contracts/openapi/*.yaml`.
- El modelo de datos detallado (invariantes, Value Objects) sigue documentado en
  `specs/*/data-model.md`.
- Si el código cambia de forma que invalide alguno de estos diagramas, se actualiza el `.mmd`
  correspondiente — no hay generación automática desde el código todavía.

Una versión navegable de estos mismos 4 diagramas (con leyendas de color y notas de diseño) está
publicada como Artifact: <https://claude.ai/artifact/KAfFCwbBS5fJTBDyoFneig>.
