# Frontend Implementation Plan: Consulta de productos bancarios del cliente

**Branch**: `001-consulta-productos-bancarios` | **Date**: 2026-09-25 | **Spec**: [../spec.md](../spec.md)

**Backend Plan**: [../plan.md](../plan.md) (no modificado por este documento)

**OpenAPI Contract (fuente de verdad)**: [../contracts/openapi/banking-products-v1.yaml](../contracts/openapi/banking-products-v1.yaml)

**Input**: Spec `001-consulta-productos-bancarios` (Status: Approved, sin `[NEEDS CLARIFICATION]`),
más un input de planificación frontend detallado que fija stack (Angular 22.x, Standalone
Components, Signals, Tailwind CSS 4.x, Vitest), Clean Architecture feature-first y las
restricciones de simplicidad, privacidad y accesibilidad descritas en este documento.

Este plan cubre **exclusivamente** la experiencia frontend de `001`. No amplía, reinterpreta ni
reduce el alcance funcional ya aprobado en `spec.md`. No modifica `spec.md`, `plan.md` (backend),
`research.md`, `data-model.md` ni los contratos OpenAPI existentes. No implementa código ni genera
`frontend/tasks.md`.

## Summary

Construir, en un proyecto Angular 22 independiente (`frontend/`, fuera de `src/`), la interfaz que
permite al cliente ficticio activo visualizar sus cuentas de ahorro y tarjetas de débito (HU1–HU4
de `spec.md`), con los productos bloqueados siempre visibles (FR-007/FR-014) y sin exponer nunca
productos de otro cliente (HU5, FR-015…FR-017, FR-022). El contrato OpenAPI existente
(`banking-products-v1.yaml`) es la única fuente de verdad de integración; los tipos TypeScript se
generan desde él (nunca a mano) y la app los consume a través de una capa de Infrastructure que
implementa puertos definidos en Domain. El estado de la feature se modela íntegramente con Angular
Signals (incluyendo los recursos asíncronos vía `resource()`/`rxResource()`), sin NgRx ni otra
librería de estado global. Tailwind CSS 4.x es el único mecanismo de estilos. La feature es de
solo lectura: no se diseña ninguna acción de escritura, bloqueo o edición.

## 1. Technical Context

**Language/Version**: TypeScript (versión que Angular 22 declare como peer dependency vía
`ng new`; no se fija un número aquí para evitar desincronización — se resuelve al inicializar el
proyecto en `frontend/tasks.md`).

**Framework**: Angular 22.x, último patch estable disponible al ejecutar `ng new` (sección 2 del
input de planificación). Standalone Components exclusivamente (sin `NgModule` nuevos).

**Primary Dependencies**:

- `@angular/core`, `@angular/common`, `@angular/router` — runtime Angular estándar.
- `@angular/common/http` (`HttpClient`, `provideHttpClient`, `withInterceptors`) — confinado a
  `infrastructure/` y `core/http/` (ver Dependency Rule).
- `rxjs` — dependencia transitiva obligatoria de Angular; usada únicamente en el borde
  HTTP/async (ver sección 10 "Signals and State Management").
- `tailwindcss` 4.x — único mecanismo de estilos (sección 13).
- `openapi-typescript` (devDependency) — genera tipos TypeScript desde el contrato OpenAPI
  existente (sección 9); no genera código runtime ni servicios Angular.
- **No** se agrega `@angular/forms` (no hay formularios en `001`, es de solo lectura), **no** se
  agrega Angular Material, Bootstrap, NgRx, Redux ni ninguna librería de componentes (sección 27).

**Testing**: Vitest, como runner de pruebas unitarias del proyecto Angular (sección 23). `TestBed`
de `@angular/core/testing` para componentes y para el store de la feature (que usa `inject()` y
`resource()`, por lo que requiere contexto de inyección de Angular aunque no dependa de
`HttpClient` real en las pruebas de Application — ver "Testing Strategy").

**Target Platform**: SPA servida como archivos estáticos, consumiendo la API REST de
`BancaDigitalPeru.Api` (`http://localhost:5285/api/v1` en desarrollo, según
`src/BancaDigitalPeru.Api/Properties/launchSettings.json`; URL configurable por entorno, ver
sección 18).

**Project Type**: Aplicación web frontend (SPA), consumidora de la API existente de `001`. No
introduce SSR, PWA ni microfrontends (sección 27).

**Performance Goals**: Sin objetivos cuantitativos propios más allá de lo que `spec.md` exige
(SC-005 es un objetivo de UX de una futura interfaz — esta feature **es** esa interfaz, y su
diseño de estados de UI explícitos, sección 16, es la forma en que se satisface). No se define un
presupuesto de bundle ni un SLA de latencia porque la spec no lo exige (Principio I).

**Constraints**: Solo lectura (refleja FR-021 del backend: la UI no debe inventar acciones de
escritura, sección 15); sin autenticación (fuera de alcance, igual que el backend); todos los
datos ficticios; el frontend nunca es la autoridad de autorización (sección 26); mensajes al
usuario en español de Perú (Principio III de la constitución).

**Scale/Scope**: Alcance de demostración académica — un cliente activo, un puñado de
cuentas/tarjetas de referencia (mismos datos que consume el backend). No se diseña paginación,
virtualización ni caché distribuida (Principio I).

## 2. Frontend Architecture

Clean Architecture aplicada de forma pragmática y **feature-first**, replicando en frontend la
misma Dependency Rule que ya rige el backend (`plan.md` backend, sección "Dependency Direction"),
adaptada a Angular:

```text
presentation  ──depends on──>  application  ──depends on──>  domain
infrastructure ──implements ports of──>  domain   (y es consumida por application vía DI)
```

- **Domain**: modelos de datos y contratos (puertos) que representan lo que el cliente necesita
  ver. Cero dependencias de paquetes externos (ni `@angular/*` ni `rxjs`).
- **Application**: el estado de la feature (Signals) y la orquestación de las llamadas a los
  puertos de Domain. Puede depender de `@angular/core` (para `signal`, `computed`, `resource`,
  `inject`, `InjectionToken`) porque Signals **es** el mecanismo de estado exigido por este plan
  (sección 11); **no** depende de `@angular/common/http` ni de `@angular/router` directamente.
- **Infrastructure**: la única capa que conoce `HttpClient`, los tipos generados desde OpenAPI y
  el mapeo DTO → Domain.
- **Presentation**: componentes standalone, páginas, rutas y pipes de formato/etiquetado. Depende
  de Application (consume sus Signals) y nunca llama `HttpClient` directamente.

Esta es la misma Dependency Rule del backend trasladada a un runtime distinto: igual que
`BancaDigitalPeru.Domain` no conoce ASP.NET Core ni EF Core, `domain/` en frontend no conoce
Angular ni HTTP.

**Nota de nomenclatura**: los nombres de carpetas y archivos de código se mantienen en inglés
(`banking-products`, `account-card`, etc.), consistente con el resto del código del proyecto y con
la estructura literal pedida en el input de planificación. Esto es independiente del Principio III
de la constitución, que rige el **texto visible para el usuario** (rutas legibles, labels,
mensajes), no los identificadores de código.

## 3. Dependency Rule

| Capa | Puede depender de | NO puede depender de |
|---|---|---|
| `domain/` | Nada externo (TypeScript puro) | `@angular/*`, `rxjs`, `HttpClient`, Router, Tailwind, DTOs de la API, Infrastructure |
| `application/` | `domain/`, `@angular/core` (Signals, `resource`/`rxResource`, `inject`, `InjectionToken`) | `@angular/common/http`, `@angular/router`, Infrastructure (solo a través de los puertos de `domain/`) |
| `infrastructure/` | `domain/`, `application/` (tokens de inyección), `@angular/common/http`, `rxjs`, tipos generados desde OpenAPI | Presentation, Tailwind |
| `presentation/` | `application/` (Signals expuestos), `domain/` (tipos), `@angular/router`, Tailwind | `HttpClient` directo, tipos generados desde OpenAPI, `infrastructure/` directo |

**Regla explícita para `resource()`/`rxResource()`**: viven en `application/` porque son la forma
en que Angular convierte un flujo asíncrono (que en el borde HTTP es un `Observable` de RxJS) en
Signals. Esto es una excepción justificada y consciente: `application/` importa `rxjs` **solo**
para tipar el retorno de los puertos de `domain/` (`Observable<T>`) y para pasarlo a `rxResource`,
nunca para lógica de negocio adicional. Se documenta como decisión explícita (no como violación)
porque `rxjs` es una dependencia transitiva obligatoria de Angular —no una librería nueva— y evita
una capa de conversión Observable→Promise que no aportaría valor real (Principio I). Ver
"Complexity Tracking" para el registro formal de esta decisión.

Los puertos (`AccountRepository`, `DebitCardRepository`) se **declaran** en `domain/` como
interfaces TypeScript puras (sin `InjectionToken`, sin imports de Angular). El `InjectionToken`
que permite inyectarlos vía DI de Angular se define en `application/` (que sí puede importar
`@angular/core`), junto al store que los consume. `infrastructure/` los implementa y los registra
en el `Provider` del token. Así `domain/` permanece 100% framework-free, igual que
`BancaDigitalPeru.Domain` en el backend.

## 4. Project Structure

```text
/
├── frontend/                     # Proyecto Angular — este plan
├── src/                          # Backend .NET (no tocado)
├── tests/                        # Pruebas backend (no tocado)
├── specs/
│   └── 001-consulta-productos-bancarios/
│       ├── spec.md                       # No modificado
│       ├── plan.md                       # No modificado (plan backend)
│       ├── research.md                   # No modificado
│       ├── data-model.md                 # No modificado
│       ├── contracts/openapi/
│       │   └── banking-products-v1.yaml  # No modificado — fuente de verdad frontend+backend
│       └── frontend/
│           └── plan.md                   # Este archivo
├── .specify/
└── ...
```

```text
frontend/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── config/            # API_BASE_URL, environment wiring
│   │   │   └── http/              # interceptors funcionales transversales
│   │   ├── shared/
│   │   │   └── ui/                # (vacío en 001 — ver sección 5)
│   │   └── features/
│   │       └── banking-products/  # única feature de este plan
│   ├── environments/
│   │   ├── environment.ts             # development
│   │   └── environment.production.ts  # production
│   └── styles.css                 # entrada de Tailwind CSS 4.x
├── package.json                   # incluye script "generate:api-types"
├── tailwind.config.ts
└── vitest.config.ts (o configuración equivalente del builder de Angular CLI)
```

No se crea ninguna carpeta vacía solo para calzar con un diagrama (sección 28 del input): `shared/`
se declara en la estructura pero permanece sin contenido propio hasta que exista una segunda
feature (`002`/`003` frontend) que demuestre una necesidad real de reutilización (ver sección 5).

## 5. Feature Structure

```text
features/banking-products/
├── domain/
│   ├── models/
│   │   ├── money.model.ts
│   │   ├── account.model.ts
│   │   ├── account-status.model.ts
│   │   ├── debit-card.model.ts
│   │   ├── card-status.model.ts
│   │   └── card-expiration.model.ts
│   └── repositories/
│       ├── account.repository.ts       # interface AccountRepository (puerto)
│       └── debit-card.repository.ts    # interface DebitCardRepository (puerto)
│
├── application/
│   └── state/
│       └── banking-products.store.ts   # único store de la feature (ver sección 7)
│
├── infrastructure/
│   ├── api/
│   │   └── generated/
│   │       └── banking-products.types.ts   # generado desde el OpenAPI — NUNCA editado a mano
│   ├── repositories/
│   │   ├── http-account.repository.ts
│   │   └── http-debit-card.repository.ts
│   └── mappers/
│       ├── account.mapper.ts
│       └── debit-card.mapper.ts
│
└── presentation/
    ├── pages/
    │   ├── banking-products-page/          # ruta '' — HU1 + HU3 (vista combinada, sección 15)
    │   ├── account-detail-page/            # ruta 'cuentas/:accountId' — HU2
    │   └── debit-card-detail-page/         # ruta 'tarjetas/:debitCardId' — HU4
    ├── components/
    │   ├── account-list/
    │   ├── account-card/
    │   ├── debit-card-list/
    │   ├── debit-card-card/
    │   ├── product-status-badge/           # ACTIVA/BLOQUEADA — reusado por cards y detalle
    │   └── query-state/                    # loading/empty/error genérico de la feature (sección 16)
    ├── pipes/
    │   ├── money.pipe.ts                   # PEN, es-PE, 2 decimales (SC-004)
    │   ├── product-status-label.pipe.ts    # ACTIVE/BLOCKED -> "Activa"/"Bloqueada"
    │   └── card-expiration.pipe.ts         # {month, year} -> "MM/YY"
    └── routes.ts                            # rutas standalone lazy-loadeadas de la feature
```

**Simplificación deliberada frente a la estructura de referencia de la sección 28**: se omite
`application/use-cases/`. Cada una de las cuatro capacidades que Presentation necesita (listar
cuentas, detalle de cuenta, listar tarjetas, detalle de tarjeta) es una llamada directa 1:1 a un
método de un puerto de `domain/repositories/`, sin ninguna regla de negocio, transformación o
decisión de autorización adicional en el cliente (la autorización ya la resuelve el backend,
Principio VI). Envolver cada llamada en una clase `GetCustomerAccountsUseCase` solo indirectaría
una única línea (`return this.repo.getAll()`), exactamente el tipo de clase ceremonial que la
sección 7 del input y el Principio I piden evitar. Las cuatro operaciones viven como métodos del
propio `BankingProductsStore` en `application/state/`. Si una futura iteración de `001` introduce
una regla de negocio real en el cliente (por ejemplo, ordenar u ocultar productos según un criterio
propio de UI no derivable del backend), esa lógica se extraería entonces a un caso de uso
explícito — no antes.

## 6. Domain Strategy

Domain frontend es intencionalmente delgado: modela **formas de datos de solo lectura**, no
invariantes de negocio. Todas las invariantes financieras y de privacidad (RB2, RB3, RB6, RB7,
FR-018, FR-019, FR-022) ya están garantizadas por el backend y por el contrato OpenAPI — el
frontend no las reimplementa, solo las **muestra fielmente**.

```typescript
// domain/models/money.model.ts
export type Currency = 'PEN';
export interface Money {
  readonly amount: number;
  readonly currency: Currency;
}

// domain/models/account-status.model.ts
export type AccountStatus = 'ACTIVE' | 'BLOCKED';

// domain/models/account.model.ts
export interface Account {
  readonly id: string;
  readonly accountType: 'SAVINGS';
  readonly maskedNumber: string;   // ya enmascarado por el backend — ver nota abajo
  readonly balance: Money;
  readonly status: AccountStatus;
}

// domain/models/card-status.model.ts
export type CardStatus = 'ACTIVE' | 'BLOCKED';

// domain/models/card-expiration.model.ts
export interface CardExpiration {
  readonly month: number; // 1-12
  readonly year: number;
}

// domain/models/debit-card.model.ts
export interface DebitCard {
  readonly id: string;
  readonly maskedNumber: string;
  readonly last4Digits: string;
  readonly accountId: string;
  readonly status: CardStatus;
  readonly expiration: CardExpiration;
}
```

**Sobre el enmascaramiento (RB7/FR-018/FR-019/SC-003)**: el contrato OpenAPI garantiza que
`maskedNumber`/`last4Digits` son los únicos campos que la API expone para números de
cuenta/tarjeta — el número completo **nunca** llega al navegador (research.md backend §3). El
frontend, por lo tanto, no implementa ninguna lógica de enmascaramiento propia; `domain/` modela
`maskedNumber` como un `string` opaco que se muestra tal cual. Reimplementar el enmascaramiento en
el cliente sería lógica redundante y una superficie de fallo adicional para una garantía que el
backend ya cumple de forma centralizada.

**Puertos** (interfaces, sin `InjectionToken` — ver sección 3):

```typescript
// domain/repositories/account.repository.ts
export interface AccountRepository {
  getAll(): Observable<Account[]>;
  getById(accountId: string): Observable<Account | null>;
}

// domain/repositories/debit-card.repository.ts
export interface DebitCardRepository {
  getAll(): Observable<DebitCard[]>;
  getById(debitCardId: string): Observable<DebitCard | null>;
}
```

`getById` devuelve `Account | null` (no lanza) para el caso 404 genérico (FR-022): Infrastructure
mapea un 404 HTTP a `null`, nunca a una excepción distinguible de "no autorizado" — así Application
no tiene ninguna forma de reconstruir, ni por accidente, la distinción entre "no existe" y "es de
otro cliente" que el backend deliberadamente colapsó (ver sección 17, Error Handling).

## 7. Application Strategy

Un único store por feature, `BankingProductsStore` (`application/state/banking-products.store.ts`),
`providedIn` a nivel de las rutas de la feature (no global — ver sección 12). Expone únicamente
Signals de solo lectura y métodos de comando:

```typescript
// application/state/banking-products.store.ts (forma conceptual, no código final)
export const ACCOUNT_REPOSITORY = new InjectionToken<AccountRepository>('AccountRepository');
export const DEBIT_CARD_REPOSITORY = new InjectionToken<DebitCardRepository>('DebitCardRepository');

@Injectable()
export class BankingProductsStore {
  #accountRepository = inject(ACCOUNT_REPOSITORY);
  #debitCardRepository = inject(DEBIT_CARD_REPOSITORY);

  // HU1 — listado de cuentas
  accounts = rxResource({ loader: () => this.#accountRepository.getAll() });

  // HU2 — detalle de cuenta; `id` es un Signal (típicamente del route param)
  accountDetail(id: Signal<string>) {
    return rxResource({
      request: () => ({ id: id() }),
      loader: ({ request }) => this.#accountRepository.getById(request.id),
    });
  }

  // HU3 — listado de tarjetas
  debitCards = rxResource({ loader: () => this.#debitCardRepository.getAll() });

  // HU4 — detalle de tarjeta
  debitCardDetail(id: Signal<string>) {
    return rxResource({
      request: () => ({ id: id() }),
      loader: ({ request }) => this.#debitCardRepository.getById(request.id),
    });
  }
}
```

Cada `resource()`/`rxResource()` expone de forma nativa `.value()`, `.error()`, `.isLoading()` y
`.status()` como Signals — esto cubre los estados exigidos por la sección 16 sin necesidad de un
wrapper genérico propio (`AsyncState<T>` u equivalente), que se descartó explícitamente por no
aportar nada que Angular no dé ya de fábrica (Principio I). El estado "empty" (colección vacía,
distinto de "loading" o "error") se deriva con `computed()` sobre `.value()` — nunca como un
Signal mutable independiente (sección 11: "un valor derivable debe representarse mediante
`computed()`").

`accountDetail`/`debitCardDetail` reciben el id como `Signal<string>` (no como `string` plano)
para que, si el usuario navega de un detalle a otro sin pasar por el listado (cambia el parámetro
de ruta), `rxResource` vuelva a ejecutar el loader automáticamente por su `request` reactivo, sin
lógica manual de "resetear y volver a pedir".

## 8. Infrastructure/API Strategy

Responsabilidades exclusivas de `infrastructure/`:

- **Cliente HTTP**: `HttpAccountRepository` y `HttpDebitCardRepository`, servicios `@Injectable()`
  que implementan los puertos de `domain/repositories/` usando `HttpClient` inyectado.
- **DTOs externos**: los tipos generados en `infrastructure/api/generated/banking-products.types.ts`
  (sección 9) — nunca se importan directamente en `presentation/` ni en `application/`.
- **Mappers**: `infrastructure/mappers/account.mapper.ts` y `debit-card.mapper.ts` convierten
  `components["schemas"]["AccountSummary" | "AccountDetail"]` → `Account` de Domain (y análogo
  para tarjetas). En `001` el mapeo es una copia estructural directa (los DTOs y los modelos de
  Domain tienen los mismos campos — ver `data-model.md` backend), pero se mantiene como función
  explícita y testeada para que un cambio futuro del contrato (p. ej. un campo interno adicional
  que Domain no deba conocer) no se propague por accidente a Presentation.
- **Manejo del 404 genérico**: `getById` atrapa la respuesta `404` de `HttpErrorResponse` y la
  convierte en `Observable<null>` (no re-lanza), preservando en el cliente la misma
  indistinguibilidad "no existe" / "es de otro cliente" que exige FR-022 — Infrastructure es la
  única capa que toca el objeto `HttpErrorResponse` crudo.

```typescript
// infrastructure/repositories/http-account.repository.ts (forma conceptual)
@Injectable()
export class HttpAccountRepository implements AccountRepository {
  #http = inject(HttpClient);

  getAll(): Observable<Account[]> {
    return this.#http
      .get<components['schemas']['AccountSummary'][]>('/accounts')
      .pipe(map((dtos) => dtos.map(toAccount)));
  }

  getById(accountId: string): Observable<Account | null> {
    return this.#http.get<components['schemas']['AccountDetail']>(`/accounts/${accountId}`).pipe(
      map(toAccount),
      catchError((err: HttpErrorResponse) => (err.status === 404 ? of(null) : throwError(() => err))),
    );
  }
}
```

Las URLs (`/accounts`, `/debit-cards/${id}`, etc.) son **relativas**; el prefijo `API_BASE_URL`
(`/api/v1` + host configurado por entorno) lo añade un interceptor funcional centralizado (sección
17/22), no cada repositorio.

## 9. OpenAPI Integration

El contrato existente `contracts/openapi/banking-products-v1.yaml` es la fuente de verdad para
ambos lados (ya lo es para el backend — ver `plan.md` backend, "API First Strategy"). El frontend
**no** lo duplica ni lo reinterpreta: genera tipos TypeScript directamente desde él.

**Herramienta elegida**: [`openapi-typescript`](https://openapi-ts.dev). Justificación (comparando
con la alternativa evaluada y descartada):

| Criterio | `openapi-typescript` | `openapi-generator-cli` (template `typescript-angular`) |
|---|---|---|
| Código generado | Solo tipos TypeScript (`paths`, `components["schemas"]`), cero código runtime | Servicios Angular completos **y `NgModule`** |
| Compatibilidad con sección 10 (solo Standalone, sin `NgModule` nuevos) | Compatible — no genera ningún módulo | **Incompatible** — el generador oficial para Angular predata la API standalone y produce `NgModule` |
| Dependencias adicionales en runtime | Ninguna (solo TypeScript types, se borran al compilar) | Un cliente HTTP generado propio, adicional al `HttpClient` que ya se usa |
| Control sobre el mapeo a Domain | Total — Infrastructure escribe sus propios repositorios/mappers tipados contra los DTOs generados | El cliente generado ya decide su propia forma de llamada, hay que envolverlo igual para respetar Domain |
| Dependencia de build | Node/TypeScript puro (`npx`) | Requiere JAR/Java o Docker para el generador |

`openapi-typescript` se instala como `devDependency` y se invoca con un script de `package.json`:

```json
{
  "scripts": {
    "generate:api-types": "openapi-typescript ../specs/001-consulta-productos-bancarios/contracts/openapi/banking-products-v1.yaml -o src/app/features/banking-products/infrastructure/api/generated/banking-products.types.ts"
  }
}
```

- Apunta al **archivo del contrato en el repositorio** (no al endpoint servido por el backend en
  ejecución), para que la generación sea reproducible sin necesidad de levantar la API — coherente
  con que el contrato, no el servidor, es la fuente de verdad (sección 9 del input).
- El archivo generado **se versiona en git** (evita que compilar el frontend dependa de ejecutar
  codegen como prerrequisito) pero lleva la cabecera automática de `openapi-typescript` que indica
  "no editar a mano"; cualquier PR que edite ese archivo manualmente debe rechazarse en revisión.
- Regenerar es un único comando (`npm run generate:api-types`) que se vuelve a ejecutar cada vez
  que `banking-products-v1.yaml` cambie — se registrará como tarea explícita en un futuro
  `frontend/tasks.md`, no en este plan.
- Los tipos generados solo se importan dentro de `infrastructure/`; nunca cruzan hacia
  `application/` o `presentation/` (Dependency Rule, sección 3).

## 10. Signals and State Management

Signals es el único mecanismo de estado de esta feature (sección 11/12 del input); no se introduce
NgRx, Redux ni ninguna librería de estado global (sección 27), porque cuatro consultas de solo
lectura sin interacción entre sí no presentan ningún problema que una librería de estado global
resuelva mejor que Signals + `resource()`.

**Frontera Signals/RxJS** (diagrama exigido por la sección 12):

```text
HttpClient (Observable)
        ↓
infrastructure/repositories/*  (Observable<Domain model>, ya mapeado y con 404→null)
        ↓
application/state/banking-products.store.ts  (rxResource: Observable -> Signal)
        ↓
Signals readonly (.value / .error / .isLoading / .status, + computed() derivados)
        ↓
presentation/  (componentes standalone, solo leen Signals — cero subscribe() manual)
```

- `signal()` se usa solo donde exista estado mutable genuino de UI que no derive de una petición
  HTTP (en `001` no hay ninguno: no hay filtros, tabs ni selección persistente más allá de la
  navegación por rutas — por eso el store de esta feature no declara ningún `signal()` propio,
  solo los `resource()` y sus `computed()` derivados).
- `computed()` para todo valor derivado: `isEmpty = computed(() => accounts.status() === 'resolved' && accounts.value()!.length === 0)`.
- `effect()` no se utiliza en `001`: no hay ningún efecto secundario real (logging, sincronización
  con almacenamiento externo, etc.) que justificarlo — introducirlo "por si acaso" violaría
  Principio I y la propia sección 11 ("usar `effect()` solamente para efectos secundarios
  reales").
- No hay `subscribe()` manual en ningún componente: `rxResource` administra la suscripción y su
  cancelación (incluyendo cuando cambia el `id` reactivo de un detalle) sin código adicional.

## 11. Component Strategy

Todos los componentes son Standalone (`standalone: true` es el default desde Angular 19+, no se
declara explícitamente salvo que la versión final de Angular 22 lo requiera). Responsabilidades:

| Componente | Responsabilidad | Consume |
|---|---|---|
| `BankingProductsPage` | Página raíz de la feature (ruta `''`): compone `AccountList` + `DebitCardList` en una sola vista (sección 15 — cuentas y tarjetas visibles juntas) | `BankingProductsStore.accounts`, `.debitCards` |
| `AccountList` | Itera cuentas, delega el estado de carga/vacío/error a `QueryState`, renderiza `AccountCard` por cada una | Signal `accounts` (input) |
| `AccountCard` | Presenta tipo, número enmascarado, saldo, moneda y estado de una cuenta; enlaza a su detalle | `Account` (input), `MoneyPipe`, `ProductStatusBadge` |
| `AccountDetailPage` | Ruta `cuentas/:accountId`; resuelve el id de ruta como Signal y pide `accountDetail(id)` al store | `BankingProductsStore.accountDetail` |
| `DebitCardList` | Análogo a `AccountList` para tarjetas | Signal `debitCards` (input) |
| `DebitCardCard` | Presenta número enmascarado, últimos 4 dígitos, cuenta asociada, estado y vencimiento | `DebitCard` (input), `CardExpirationPipe`, `ProductStatusBadge` |
| `DebitCardDetailPage` | Ruta `tarjetas/:debitCardId` | `BankingProductsStore.debitCardDetail` |
| `ProductStatusBadge` | Badge visual ACTIVA/BLOQUEADA reutilizado por cards y páginas de detalle (cuentas y tarjetas comparten el mismo vocabulario de estado) | `AccountStatus \| CardStatus` (input), `ProductStatusLabelPipe` |
| `QueryState` | Envoltorio de presentación para loading/empty/error de un `resource()` (sección 16), parametrizado por proyección de contenido (`ng-content`/`@if`) | Signals de estado (input) |

No se crean componentes adicionales sin una razón de reutilización o legibilidad concreta (sección
14): por ejemplo, no se separa un componente `AccountBalance` aparte de `AccountCard` porque no se
reutiliza en ningún otro lugar de `001`.

`QueryState` y `ProductStatusBadge` se mantienen dentro de `features/banking-products/presentation/`
y **no** se mueven a `shared/ui/` todavía (sección 5: "no mover componentes a shared/
anticipadamente"), aunque es previsible que `002`/`003` (transferencias) necesiten un patrón de
loading/empty/error equivalente. Esa promoción a `shared/` se evaluará cuando exista una segunda
feature real que lo necesite, no de forma anticipada.

## 12. Routing

```typescript
// presentation/routes.ts
export const BANKING_PRODUCTS_ROUTES: Routes = [
  {
    path: '',
    providers: [
      { provide: ACCOUNT_REPOSITORY, useClass: HttpAccountRepository },
      { provide: DEBIT_CARD_REPOSITORY, useClass: HttpDebitCardRepository },
      BankingProductsStore,
    ],
    children: [
      { path: '', component: BankingProductsPage },                 // HU1 + HU3
      { path: 'cuentas/:accountId', component: AccountDetailPage },  // HU2
      { path: 'tarjetas/:debitCardId', component: DebitCardDetailPage }, // HU4
    ],
  },
];
```

Montaje lazy en el router raíz de la app:

```typescript
{ path: 'productos', loadChildren: () => import('./features/banking-products/presentation/routes')
    .then((m) => m.BANKING_PRODUCTS_ROUTES) }
```

- Rutas en español (`/productos`, `/productos/cuentas/:accountId`,
  `/productos/tarjetas/:debitCardId`) — texto visible para el usuario, Principio III.
- Los `:accountId`/`:debitCardId` de la URL son los mismos identificadores internos (UUID) que ya
  expone el contrato OpenAPI, explícitamente documentados como "no es el número de cuenta/tarjeta
  real" (`banking-products-v1.yaml`, parámetros `AccountId`/`DebitCardId`). Usarlos tal cual en la
  URL no expone ningún dato sensible (sección 20): el propio backend ya los diseñó como opacos y
  no correlacionables con el número real de cuenta/tarjeta.
- Los providers de repositorios y el store se registran a nivel de las rutas de la feature (no en
  el `ApplicationConfig` raíz), manteniendo la feature autocontenida y evitando que su estado o sus
  dependencias HTTP existan fuera de cuando el usuario navega dentro de `/productos` (alineado con
  el lazy loading de la sección 10 del input).

## 13. Tailwind / Design System

Tailwind CSS 4.x es el único mecanismo de estilos (sección 17). No se agrega Angular Material,
Bootstrap ni otro framework de componentes en `001` (sección 27); si una futura feature demuestra
una necesidad concreta de un sistema de componentes más amplio, se evaluará y justificará
explícitamente en su propio plan, no aquí.

**Design tokens** (definidos una sola vez en `tailwind.config.ts` / `@theme` de Tailwind 4, no
repetidos como clases arbitrarias dispersas):

| Token | Uso |
|---|---|
| `color-surface` / `color-surface-muted` | Fondo de tarjetas de producto vs. fondo de página |
| `color-status-active` / `color-status-blocked` | Colores consistentes del badge ACTIVA/BLOQUEADA en cuentas y tarjetas |
| `color-feedback-error` / `color-feedback-empty` | Estados de error y vacío (sección 16) |
| `spacing-card-gap`, `spacing-page-gutter` | Separaciones repetidas entre tarjetas y márgenes de página |
| `radius-card` | Radio de borde consistente para `AccountCard`/`DebitCardCard` |
| `font-size-balance` | Tamaño tipográfico distintivo para el saldo disponible (dato más relevante de una cuenta) |

`ProductStatusBadge` es el único punto donde se traduce `status` a color — evita que el color de
"bloqueada" se repita como clase arbitraria en cada componente que muestra un producto.

## 14. Loading / Empty / Error UX

Cada uno de los cuatro `resource()` del store expone `.status()` con valores que cubren
exactamente los cinco estados exigidos por la sección 16 (`idle`/`loading`/`resolved`/`local`/
`error`, mapeados así):

| Estado de la sección 16 | Origen en `resource()` | Presentación |
|---|---|---|
| initial | `status() === 'idle'` (antes de la primera carga) | `QueryState` no renderiza contenido ni spinner |
| loading | `isLoading()` | `QueryState` muestra un indicador de carga acotado (nunca un spinner sin salida: `resource()` siempre resuelve a `resolved` o `error`, no hay estado "colgado" posible) |
| success (con datos) | `status() === 'resolved'` y colección no vacía / detalle no nulo | `QueryState` proyecta el contenido (`AccountList`, `AccountCard`, etc.) |
| empty | `status() === 'resolved'` y colección vacía (`computed()`) | Mensaje explícito ("Aún no tienes cuentas de ahorro registradas." / "Aún no tienes tarjetas de débito registradas.") — cubre "Cliente sin cuentas"/"Cliente sin tarjetas" (Edge Cases de `spec.md`) |
| error | `status() === 'error'` | Mensaje en español de Perú derivado del `ApiError` normalizado (sección 17), nunca el `HttpErrorResponse` crudo |

Para el detalle de un producto inexistente/ajeno (`getById` → `null`, FR-022), `AccountDetailPage`/
`DebitCardDetailPage` tratan `value() === null` como un estado distinto de "error": es el mismo
mensaje genérico "Este producto no existe o no está disponible." tanto si el producto no existe
como si pertenece a otro cliente — nunca se muestra un mensaje distinto para cada caso, porque el
backend ya garantiza (y el frontend debe preservar) que ambos son indistinguibles.

## 15. Responsive Strategy

Enfoque mobile-first con Tailwind (sección 18): el layout de `AccountList`/`DebitCardList` es una
columna única en mobile y una grilla de 2–3 columnas en tablet/desktop (`grid-cols-1
md:grid-cols-2 lg:grid-cols-3`, usando los breakpoints estándar de Tailwind). `AccountCard`/
`DebitCardCard` mantienen saldo, número enmascarado y estado siempre visibles sin truncar en el
ancho mínimo soportado; el estado (`ProductStatusBadge`) nunca se oculta en pantallas pequeñas, ya
que es información crítica (FR-007/FR-014). No se diseña una versión "solo desktop": el mismo
componente responde a los tres tamaños de referencia (mobile/tablet/desktop) sin variantes
separadas por dispositivo.

## 16. Accessibility

- HTML semántico: `AccountCard`/`DebitCardCard` son `<article>`; `ProductStatusBadge` es un
  `<span>` con texto visible (nunca solo color, para no depender de percepción cromática); los
  enlaces a detalle son `<a>` (vía `routerLink`), nunca un `<div>` con `(click)`.
  - Jerarquía de encabezados: `BankingProductsPage` define `<h1>` (p. ej. "Mis productos"),
    secciones de cuentas/tarjetas usan `<h2>`, cada tarjeta de producto puede usar `<h3>` para su
    identificador enmascarado.
  - Foco visible en todos los elementos interactivos (enlaces de detalle, botón "volver") —
    Tailwind no remueve el `outline` por defecto; si se ajusta visualmente, se reemplaza por un
    estilo de foco igualmente perceptible, nunca se elimina sin reemplazo.
  - `aria-live="polite"` en el contenedor de `QueryState` para que lectores de pantalla anuncien
    transiciones loading→success/error sin gestos adicionales del usuario.
  - Contraste de color conforme a WCAG AA como mínimo para los tokens de `color-status-active`/
    `color-status-blocked` sobre `color-surface`.
  - `aria-*` se usa únicamente donde el HTML semántico no basta (p. ej. `aria-live` arriba); no se
    usan atributos ARIA para sustituir un elemento nativo apropiado (sección 19).

## 17. Error Handling

**Interceptors funcionales** (`core/http/`, registrados vía `provideHttpClient(withInterceptors([...]))`):

1. `apiBaseUrlInterceptor` — antepone `API_BASE_URL` (sección 18) a toda petición relativa emitida
   por `infrastructure/repositories/*`, para que estas no necesiten conocer el host/puerto.
2. `problemDetailsInterceptor` — normaliza cualquier respuesta de error `application/problem+json`
   (el `ProblemDetails` que ya expone el backend) a un tipo `ApiError { status, title, detail }` de
   Infrastructure, para que ningún componente de Presentation reciba un `HttpErrorResponse` crudo
   (sección 22). Los `4xx` esperados (`400`, `404`) preservan el `detail` que el backend ya redacta
   en español de Perú (fuente única de verdad del mensaje, sin duplicarlo en el frontend); un
   `5xx`/error de red se reemplaza por un mensaje genérico propio del frontend ("Ocurrió un error
   inesperado. Intenta nuevamente más tarde.") por si el cuerpo de la respuesta no está disponible.

**Evaluado y descartado para `001`**: un interceptor de *correlation id* (mencionado como ejemplo
en la sección 22 del input). Ninguna FR/SC de `spec.md` exige trazabilidad end-to-end
cliente-servidor, y el contrato `ProblemDetails` actual no define ningún campo de correlación que
el backend consuma o devuelva. Añadirlo ahora sería infraestructura anticipada sin necesidad
concreta (Principio I). La lista de interceptors queda estructurada de forma que agregar uno nuevo
en el futuro (p. ej. si `002`/`003` lo requieren) no obliga a tocar los ya existentes.

Ningún interceptor contiene reglas de negocio (sección 22): ambos son puramente técnicos
(prefijo de URL, normalización de forma de error).

## 18. Configuration

`API_BASE_URL` se resuelve vía los archivos de entorno estándar de Angular CLI
(`fileReplacements`), nunca hardcodeado en un componente o servicio:

```typescript
// environments/environment.ts (development)
export const environment = { apiBaseUrl: 'http://localhost:5285/api/v1' };

// environments/environment.production.ts
export const environment = { apiBaseUrl: '/api/v1' }; // mismo origen que el despliegue, a confirmar en tasks
```

`core/config/api-config.ts` expone un `InjectionToken<string>` (`API_BASE_URL`) inicializado desde
`environment.apiBaseUrl`, consumido únicamente por `apiBaseUrlInterceptor`. No se incluye ningún
secreto, credencial ni cadena de conexión de backend en el código fuente del frontend (el frontend
no tiene ni necesita credenciales, igual que el backend no expone las suyas — Principio VI).

## 19. Security Considerations

El frontend **no** es una frontera de autorización (sección 26, refleja el Principio VI del
backend): toda verificación de propiedad ya ocurre en el servidor; ocultar o no un enlace en la UI
nunca sustituye esa verificación. Consecuencias concretas para `001`:

- No se cachea ninguna respuesta de cuentas/tarjetas en `localStorage`/`sessionStorage`; los datos
  viven únicamente en los Signals del store mientras la feature está activa (se descartan al salir
  de la ruta lazy, porque el store se provee a nivel de ruta — sección 12).
- No hay ningún token, credencial ni identificador de cliente que el frontend deba almacenar: no
  existe autenticación en `001` (igual que en el backend).
- El enmascaramiento de números de cuenta/tarjeta no es una medida de seguridad del frontend: es
  una garantía que ya cumple el backend (sección 6); el frontend simplemente no tiene forma de
  romperla porque nunca recibe el número completo.
- El 404 genérico de FR-022 se preserva íntegro hasta la UI (sección 14/17): en ningún punto del
  frontend se reconstruye o se infiere si un identificador ajeno "existe pero no es tuyo".

## 20. Testing Strategy

Vitest en todo el proyecto Angular, ejecutado sobre `TestBed` donde se requiera contexto de
inyección (Application y Presentation).

- **Domain**: `001` no introduce reglas ni invariantes propias en Domain (sección 6) — solo tipos
  estructurales. No se escriben pruebas ceremoniales sobre interfaces sin comportamiento
  (Principio I). Si una futura iteración añade una función pura a Domain (p. ej. una regla de
  ordenamiento), se cubrirá entonces con pruebas unitarias puras sin `TestBed`.
- **Application** (`banking-products.store.ts`): se instancia con `TestBed.runInInjectionContext`
  o un `TestBed.configureTestingModule` mínimo, proveyendo **fakes** de `AccountRepository`/
  `DebitCardRepository` (implementaciones en memoria del puerto de Domain, sin `HttpClient` ni
  `HttpTestingController`) vía los `InjectionToken` de la sección 7. Casos cubiertos:
  - `accounts` resuelve con las cuentas devueltas por el fake, incluyendo una `BLOCKED` (HU1 AS1,
    AS2).
  - `accounts` resuelve a colección vacía cuando el fake no tiene cuentas (Edge Case "Cliente sin
    cuentas").
  - `accountDetail(id)` resuelve al valor esperado para un id existente (HU2 AS1) y a `null`
    cuando el fake simula un 404 (Edge Cases "Producto inexistente"/"Producto de otro cliente" —
    mismo resultado para ambos, verificando explícitamente que no hay forma de distinguirlos desde
    Application).
  - `accountDetail(id)` reacciona cuando el Signal `id` cambia (navegación entre detalles sin
    recarga completa de página).
  - Los cuatro casos análogos para `debitCards`/`debitCardDetail` (HU3, HU4).
  - Estado `error` cuando el fake simula un fallo distinto de 404 (p. ej. 500).
- **Infrastructure**: pruebas de `http-account.repository.ts`/`http-debit-card.repository.ts` con
  `HttpTestingController` (`provideHttpClientTesting`), verificando: la URL relativa invocada, el
  mapeo DTO→Domain (`account.mapper.ts`/`debit-card.mapper.ts`), y que un `404` real se traduce a
  `null` sin propagar la excepción (FR-022 a nivel de borde HTTP).
- **Presentation**: `TestBed` + `ComponentFixture` (sin librerías adicionales de testing — se
  evalúa Angular Testing Library como mejora futura opcional, no incorporada por defecto en `001`
  para no sumar una dependencia sin necesidad probada, Principio I). Casos:
  - `AccountList`/`DebitCardList` renderizan loading, luego la lista, con una cuenta/tarjeta
    `BLOCKED` visible e identificada (HU1 AS2, HU3 AS2 — "los productos bloqueados continúan
    visibles").
  - Estado vacío visible y comprensible cuando no hay cuentas/tarjetas (Edge Cases).
  - Estado de error visible con mensaje en español, sin exponer el cuerpo técnico del error.
  - Navegación: un click/Enter sobre `AccountCard`/`DebitCardCard` navega a la ruta de detalle
    correcta (verificado con `RouterTestingHarness` o equivalente).
  - `AccountDetailPage`/`DebitCardDetailPage` muestran el mismo mensaje genérico de "no
    disponible" tanto para un id inexistente como para un id simulado de otro cliente (HU5 AS1,
    AS2 — verificación explícita de indistinguibilidad también en la capa visual).
  - `MoneyPipe` formatea siempre con exactamente 2 decimales y símbolo de Soles (SC-004).
  - `last4Digits` de una tarjeta terminada en 4582 es visible tal cual en `DebitCardCard` (HU3
    AS3).

**Trazabilidad** (sección 24 del input): cada Acceptance Scenario de `spec.md` queda cubierto por
al menos una prueba de Application o de Presentation listada arriba; ningún test introduce un
criterio de aceptación que no exista ya en `spec.md`.

## 21. Performance

- `resource()`/`rxResource()` evitan peticiones duplicadas: mientras un recurso está `loading` o
  `resolved` para el mismo `request`, no se dispara una segunda petición idéntica; navegar de ida
  y vuelta entre el listado y un detalle no genera llamadas redundantes al listado.
- `@for` (control flow de Angular, reemplazo de `*ngFor`) con `track item.id` en
  `AccountList`/`DebitCardList`, evitando recrear el DOM de tarjetas no cambiadas.
- Carga por feature vía lazy loading (sección 12): el bundle de `banking-products` (incluidos sus
  tipos generados y componentes) no se descarga hasta que el usuario navega a `/productos`.
- Se evalúa (no se decide en este plan, por no ser un requisito de `spec.md`) adoptar detección de
  cambios zoneless (`provideZonelessChangeDetection`), coherente con un diseño 100% basado en
  Signals; la decisión final y su viabilidad con la versión exacta de Angular 22 disponible se
  confirma en `frontend/tasks.md`.
- No se implementa paginación, virtualización de listas ni caché HTTP adicional (sección 21/25):
  el volumen de datos de referencia (un puñado de cuentas/tarjetas por cliente) no lo justifica
  (Principio I).

## 22. Constitution Check

| Ítem (sección 29 del input) | Evaluación | Estado |
|---|---|---|
| El frontend no amplía la spec 001 | Cuatro pantallas/rutas, una por HU (HU1–HU4); ninguna acción de escritura, bloqueo o edición | PASS |
| Se mantiene simplicidad | Sin `use-cases/` ceremoniales (sección 5), sin `AsyncState` propio (sección 7), sin librería de estado global, sin componentes UI adicionales (sección 27) | PASS |
| Clean Architecture tiene una Dependency Rule clara | Sección 3, tabla explícita por capa | PASS |
| Domain no depende de Angular infrastructure | Sección 6: cero imports de `@angular/*`/`rxjs` en `domain/` | PASS |
| HttpClient permanece en Infrastructure | Sección 8: único lugar que inyecta `HttpClient` | PASS |
| Presentation no llama directamente HttpClient | Sección 11: Presentation solo lee Signals de `application/` | PASS |
| OpenAPI continúa siendo la fuente de verdad del contrato | Sección 9: tipos generados desde `banking-products-v1.yaml`, nunca al revés | PASS |
| Standalone Components se utilizan exclusivamente | Sección 11: sin `NgModule` nuevos | PASS |
| Signals son el mecanismo principal de estado | Sección 10 | PASS |
| No se introduce estado global innecesario | Store `provided` a nivel de ruta de la feature, no en el `ApplicationConfig` raíz (sección 12); sin NgRx/Redux | PASS |
| Los productos de terceros nunca se muestran | El backend ya filtra por cliente actual (FR-015); el frontend no añade ningún parámetro de cliente ni selector — consume exactamente lo que la API entrega | PASS |
| Datos sensibles permanecen enmascarados | Sección 6: el frontend nunca solicita ni recibe el número completo | PASS |
| Productos BLOQUEADOS continúan visibles | Sección 11/14: `ProductStatusBadge` siempre renderizado, ningún filtro por `status` en `AccountList`/`DebitCardList` | PASS |
| Existen loading/empty/error states | Sección 14, tabla completa de los 5 estados | PASS |
| Existen tests automatizados | Sección 20, Domain/Application/Infrastructure/Presentation | PASS |
| Tailwind se usa consistentemente | Sección 13: design tokens centralizados, sin clases arbitrarias repetidas | PASS |
| La UI es responsive y accesible | Secciones 15 y 16 | PASS |
| Toda complejidad adicional está justificada | Sección 23 (Complexity Tracking) | PASS |

**Constitution Check: PASS.** Ninguna decisión de este plan requiere una excepción a los
principios I–IX de `.specify/memory/constitution.md`; las únicas desviaciones frente a la
estructura de referencia del input (sección 28) son simplificaciones documentadas (secciones 5 y
7), no violaciones.

## 23. Risks and Trade-offs

| Riesgo/Trade-off | Impacto | Mitigación |
|---|---|---|
| Angular 22 es una versión muy reciente; APIs como `resource()`/`rxResource()` o el flag exacto de Vitest en `ng new` pueden diferir levemente de lo asumido aquí | Medio — podría requerir ajustar nombres exactos de API al ejecutar `ng new` | Este plan describe el comportamiento e intención (Signals-first, sin `NgModule`, Vitest como test runner), no una versión de API congelada; `frontend/tasks.md` confirmará los nombres exactos disponibles en el patch instalado |
| `openapi-typescript` genera tipos, no un cliente — Infrastructure debe escribir manualmente cada repositorio HTTP | Bajo — más código manual que un generador de cliente completo | Aceptado conscientemente: es el precio de evitar `NgModule` generados (sección 9); el volumen es pequeño (4 operaciones) |
| El store usa `rxjs` en el borde `application/infrastructure` (sección 3) | Bajo — una dependencia adicional a lo estrictamente "Signals puro" en Application | Documentado como decisión explícita (Complexity Tracking); `rxjs` es dependencia transitiva obligatoria de Angular, no una librería nueva |
| Sin librería de testing de componentes (Angular Testing Library) | Bajo — pruebas de Presentation algo más verbosas con `TestBed` puro | Aceptado por Principio I; se reevaluará si la suite de Presentation crece significativamente en `002`/`003` |
| `shared/ui` queda vacío en `001` | Bajo — puede sentirse como una carpeta "preparada mas no usada" | Documentado explícitamente en sección 5 como decisión consciente, no como carpeta olvidada |

## 24. Complexity Tracking

> Igual que en el plan backend, se documentan aquí únicamente decisiones que, sin violar la
> constitución, podrían parecer una desviación de "la solución más simple posible" y por tanto
> requieren justificación explícita (Principio I).

| Decisión | Por qué es necesaria ahora | Alternativa más simple descartada |
|---|---|---|
| `application/` importa `rxjs` (vía `rxResource`) además de `@angular/core` | Es el mecanismo nativo de Angular para convertir el `Observable` que devuelve `HttpClient` (a través de los puertos de Domain) en Signals sin gestionar `subscribe()`/`unsubscribe()` manualmente; evita una capa de conversión Observable→Promise que no aportaría ningún valor de negocio | Puertos de Domain basados en `Promise<T>` en vez de `Observable<T>`, usando `resource()` (variante Promise) en vez de `rxResource()` — descartada porque forzaría una conversión adicional en Infrastructure (`firstValueFrom`) solo para volver a convertir a Signal, perdiendo además la cancelación nativa de RxJS que `HttpClient` ya provee |
| Se declara `shared/ui/` en la estructura aunque quede vacío en `001` | El input de planificación (sección 3/28) fija esa ubicación para la futura reutilización entre `001`/`002`/`003`; crear la carpeta sin contenido dreal documenta la intención sin forzar una promoción prematura de componentes | Omitir `shared/` por completo hasta que exista contenido — descartada porque el propio input fija esta carpeta como parte de la estructura raíz esperada del proyecto Angular, y su ausencia total podría leerse como una desviación no intencional de esa estructura |

Ninguna otra fila aplica: no se introduce NgRx, Redux, microfrontends, Module Federation, SSR, PWA,
WebSockets, Service Workers, Angular Material, ni ningún patrón fuera de lo solicitado en el input
de planificación.

---

**Siguiente paso** (fuera del alcance de este documento): generar `frontend/tasks.md` a partir de
este plan, únicamente cuando se solicite explícitamente — este plan no implementa código ni crea
tareas.
