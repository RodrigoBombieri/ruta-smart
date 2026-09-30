# RutaSmart Argentina

**¿Qué condiciones voy a encontrar si hago este viaje?**

Repositorio de investigación y diseño, entregado el 30 de septiembre de 2026, con fuentes revisadas el 29 de septiembre. No contiene una aplicación implementada. RutaSmart analiza viajes por carretera; la navegación se delega a Waze o Google Maps.

## Comenzar por aquí

1. [Plan maestro de desarrollo](PLAN-MAESTRO.md): propuesta completa y orden de implementación.
2. [Fuentes y estado de verificación](DATA-SOURCES.md): evidencia, accesos, licencias y bloqueantes.
3. [Roadmap](ROADMAP.md) y [backlog](BACKLOG.md): trabajo ejecutable y criterios de aceptación.
4. [Decisiones](DECISIONS.md): decisiones adoptadas para el diseño y pendientes antes de desarrollar.

La factibilidad es **condicional**: el acceso automatizado autorizado a alertas oficiales y eventos viales debe validarse. No se promete cobertura nacional completa ni precios de surtidor actuales. La obligación de reporte de la Resolución 314/2016 fue derogada en 2025; ver la evidencia en DATA-SOURCES.

## Documentación

| Documento | Contenido |
|---|---|
| [VISION](VISION.md) | Problema, diferenciación, usuarios y negocio futuro |
| [PRODUCT](PRODUCT.md) | MVP, UX, competencia, mapas y prioridades |
| [ARCHITECTURE](ARCHITECTURE.md) | Módulos, adaptadores, trabajos y diagramas |
| [DOMAIN](DOMAIN.md) | Entidades, persistencia, invariantes y ER |
| [DATA-SOURCES](DATA-SOURCES.md) | Investigación oficial y registro de verificación |
| [ROUTING](ROUTING.md) | Siete motores, costos, límites y PoC |
| [GEOSPATIAL](GEOSPATIAL.md) | Corredores, matching, índices y precisión |
| [TRIP-ANALYSIS](TRIP-ANALYSIS.md) | Timeline, clima, vialidad, combustible y evaluación |
| [API](API.md) | Contrato REST y errores |
| [OpenAPI](docs/openapi.json) | Contrato propuesto, no API desplegada |
| [SECURITY](SECURITY.md) | Seguridad, privacidad y retención |
| [TESTING](TESTING.md) | Casos críticos y estrategia sin dependencias externas |
| [DEPLOYMENT](DEPLOYMENT.md) | Hosting, CI/CD, operación y observabilidad |
| [ROADMAP](ROADMAP.md) | Fases 0–14 con puertas de aceptación |
| [BACKLOG](BACKLOG.md) | Épicas, features, historias y tareas |
| [DECISIONS](DECISIONS.md) | Estado de decisiones y ADR |
| [GLOSSARY](GLOSSARY.md) | Vocabulario común |
| [ADR](docs/adr/README.md) | Fundamentos de decisiones |
| [Evidencia](docs/research/VERIFICATION.md) | Qué se comprobó y qué falta probar |
| [Fixtures](docs/fixtures/README.md) | Datos sintéticos para futuros tests |

## Convenciones

- **Oficial** describe procedencia, no exactitud ni vigencia garantizadas.
- **Externo** procede de un proveedor o comunidad ajenos al organismo público.
- **Calculado** resulta de una regla reproducible.
- **Estimado** incorpora supuestos e incertidumbre; puede ser también calculado.
- **D** = documentación verificada; **T** = respuesta técnica verificada; **L** = reutilización autorizada para el uso concreto. Son controles independientes.
- Las cuotas internas, umbrales, TTL y objetivos de rendimiento son propuestas de RutaSmart, salvo atribución explícita al proveedor.
- Los archivos y directorios futuros mencionados en roadmap no existen todavía. No se generaron proyectos .NET/Flutter, migraciones ni infraestructura ejecutable.

No hay instalación ni comando para ejecutar la aplicación en esta etapa. La próxima actividad es cerrar la validación de fuentes y la PoC de routing, no construir pantallas de producción.
