<!--
Sync Impact Report
- Version change: [PLANTILLA SIN RATIFICAR] → 1.0.0
- Modified principles: N/A (primera ratificación; plantilla genérica reemplazada íntegramente)
- Added sections:
  - Preámbulo / Contexto
  - Core Principles (9 principios: I. Simplicidad ante todo; II. Especificaciones primero y
    alcance gobernado por ellas; III. Mercado peruano; IV. Clean Architecture y responsabilidades
    separadas; V. Integridad financiera; VI. Seguridad y mínimo privilegio; VII. Comportamiento
    verificable; VIII. Calidad automatizada; IX. Decisiones técnicas justificadas)
  - Governance (con orden de precedencia entre principios y política de versionado semántico)
- Removed sections: [SECTION_2_NAME]/[SECTION_2_CONTENT] y [SECTION_3_NAME]/[SECTION_3_CONTENT]
  de la plantilla genérica (se omiten porque solo podrían llenarse con decisiones técnicas o de
  proceso propias de specs/planes, fuera del alcance de esta constitución)
- Templates requiring updates:
  - .specify/templates/plan-template.md ⚠ pending manual review (verificar que el Constitution
    Check referencie los 9 principios por nombre)
  - .specify/templates/spec-template.md ⚠ pending manual review (verificar alineación con
    Principio VII, comportamiento verificable)
  - .specify/templates/tasks-template.md ⚠ pending manual review (verificar alineación con
    Principio VIII, pruebas obligatorias como tareas)
- Follow-up TODOs: ninguno
-->

# Constitución de Banca Digital Perú (Proyecto Académico)

Este proyecto es una aplicación académica de banca digital orientada al mercado peruano.
No procesa dinero real ni datos reales de personas: todo dato financiero, personal o
transaccional utilizado en specs, planes, pruebas y demostraciones DEBE ser ficticio.

## Core Principles

### I. Simplicidad ante todo
Entre dos soluciones que satisfagan correctamente los requisitos, se DEBE elegir la más
simple. NO DEBEN introducirse complejidad, abstracciones ni infraestructura de forma
anticipada. Toda complejidad adicional DEBE justificarse con una necesidad concreta y
registrarse en la sección "Complexity Tracking" del plan, indicando la alternativa más
simple descartada. La simplicidad opera dentro de los límites de los principios IV, V y VI;
nunca justifica incumplirlos.

**Racional**: la simplicidad reduce el costo de mantenimiento y el riesgo de defectos, pero
sin límites puede usarse para evadir controles de arquitectura, integridad financiera o
seguridad; por eso se subordina explícitamente a esos principios.

### II. Especificaciones primero y alcance gobernado por ellas
Toda funcionalidad material DEBE seguir Specification-Driven Development: spec (qué y por
qué), luego plan (cómo), luego tareas e implementación. "Material" es todo cambio que
altere comportamiento observable o reglas de negocio; refactors sin efecto observable,
documentación y actualización de dependencias no lo son. NO DEBE implementarse ninguna
funcionalidad, regla de negocio ni integración que no esté en una spec aprobada; una spec
está aprobada cuando su encabezado indica "Status: Approved" y no quedan marcadores
[NEEDS CLARIFICATION]. Toda necesidad nueva surgida en diseño o implementación DEBE
proponerse y especificarse antes de incorporarse. Las decisiones técnicas NO DEBEN alterar
silenciosamente los requisitos de la spec.

**Racional**: fijar el orden spec → plan → tareas evita que el alcance crezca o mute sin
trazabilidad ni acuerdo explícito, y mantiene la spec como fuente de verdad del qué y el
porqué.

### III. Mercado peruano
El producto está dirigido exclusivamente al mercado peruano. El texto visible para el
usuario y la terminología bancaria DEBEN estar en español de Perú; las specs y planes
también se redactan en español. El Sol peruano (PEN) es la moneda por defecto; cualquier
otra moneda DEBE definirse en una spec. Los conceptos propios del sistema financiero
peruano DEBEN usarse cuando las specs lo requieran.

**Racional**: un producto dirigido a un mercado específico debe reflejar su idioma, moneda
y convenciones financieras de forma consistente, evitando ambigüedad o localización
incompleta.

### IV. Clean Architecture y responsabilidades separadas
La solución DEBE respetar la Dependency Rule: las dependencias del código apuntan solo
hacia capas internas. Las reglas de negocio y los casos de uso DEBEN permanecer
independientes de la interfaz de usuario, la persistencia, los servicios externos y los
frameworks. Las reglas de negocio NO DEBEN residir en mecanismos de entrega, persistencia
ni otra infraestructura, y cada componente DEBE tener una responsabilidad clara.

**Racional**: separar reglas de negocio de detalles de entrega e infraestructura permite
evolucionar o sustituir tecnología sin reescribir el dominio, y facilita la verificación
independiente exigida por el Principio VIII.

### V. Integridad financiera
Los valores monetarios DEBEN manejarse sin pérdida de precisión, con reglas de redondeo
definidas en la spec correspondiente. Las operaciones que modifiquen dinero DEBEN
preservar la consistencia ante errores, concurrencia y solicitudes duplicadas (una
solicitud repetida NO DEBE aplicar su efecto dos veces ni dejar estados parciales). Toda
operación que modifique dinero DEBE ser trazable y auditable.

**Racional**: aunque el proyecto es académico y no mueve dinero real, debe modelar con
rigor las garantías que un sistema bancario real exige, para que el aprendizaje sea
representativo del dominio financiero.

### VI. Seguridad y mínimo privilegio
Un usuario solo DEBE poder consultar u operar recursos para los que esté autorizado; el
acceso se deniega por defecto. La autorización y la propiedad de los recursos DEBEN
verificarse en el backend, sin confiar en datos del cliente. Los datos sensibles DEBEN
minimizarse, NUNCA DEBEN incluirse secretos ni credenciales en código fuente ni en logs, y
solo DEBEN usarse datos ficticios.

**Racional**: el mínimo privilegio y la verificación server-side son controles básicos e
irrenunciables en cualquier sistema que simule operaciones bancarias, incluso sin datos
reales de por medio.

### VII. Comportamiento verificable
Los requisitos y criterios de aceptación de cada spec DEBEN expresarse como comportamientos
observables y verificables (escenarios Dado/Cuando/Entonces y criterios de éxito
medibles), sin depender de detalles internos de implementación. Las specs describen qué y
por qué, no cómo.

**Racional**: expresar requisitos como comportamiento observable permite validar el
cumplimiento de forma objetiva y mantiene la spec desacoplada de decisiones de
implementación que pueden cambiar.

### VIII. Calidad automatizada
Las reglas de negocio críticas y los casos de uso DEBEN estar protegidos por pruebas
automatizadas; estas pruebas son obligatorias (no opcionales) y DEBEN aparecer como
tareas. Las capas internas DEBEN poder verificarse sin infraestructura externa; toda
excepción se registra en "Complexity Tracking".

**Racional**: exigir pruebas automatizadas como tareas explícitas evita que la
verificación de reglas de negocio críticas quede como trabajo implícito u opcional, y la
independencia de infraestructura externa hace las pruebas rápidas y confiables.

### IX. Decisiones técnicas justificadas
Frameworks, bases de datos, librerías, patrones adicionales a Clean Architecture,
infraestructura y otras tecnologías son decisiones de implementación: DEBEN definirse y
justificarse durante la planificación y NO DEBEN volverse restricciones globales salvo que
se incorporen mediante enmienda de esta constitución.

**Racional**: mantener las decisiones tecnológicas en el plan (y no en la constitución)
permite que evolucionen según cada spec sin requerir una enmienda constitucional, salvo
que se decida elevarlas a restricción de todo el proyecto.

## Governance

- La constitución prevalece sobre specs, planes y tareas. Un conflicto se resuelve
  corrigiendo el artefacto o enmendando la constitución de forma explícita, nunca
  ignorándolo.
- Ante conflicto entre principios, prevalece este orden: (1) Integridad financiera y
  Seguridad, (2) Clean Architecture, (3) Simplicidad.
- Toda enmienda se propone por escrito con justificación y actualiza la versión y la fecha
  de última enmienda.
- Versionado semántico: MAJOR al eliminar o redefinir un principio de forma incompatible;
  MINOR al agregar un principio o ampliarlo materialmente; PATCH para aclaraciones y
  redacción.
- Todo plan DEBE pasar el Constitution Check antes de continuar y volver a pasarlo tras el
  diseño.

**Version**: 1.0.0 | **Ratified**: 2026-09-20 | **Last Amended**: 2026-09-20
