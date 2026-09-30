# ADR-0004 — Sin datos no significa sin advertencias

Fecha: 2026-09-29. Estado: adoptada; integraciones pendientes.

Contexto: publicaciones oficiales no siempre ofrecen API documentada; disponibilidad, frescura y cobertura son independientes.

Decisión: habilitar fuentes con D/T/L, versionar ingestas, no scraping base, separar condición conocida de suficiencia. Sin cobertura crítica no se muestra favorable; corte conocido no se oculta por otra fuente caída.

Alternativas rechazadas: usar endpoints internos sin contrato, vaciar cache tras fallo, inferir reapertura por ausencia y presentar forecast externo como alerta oficial.

Consecuencias: la viabilidad del MVP completo queda condicionada a feeds sostenibles. Beta reducida debe declararse como tal. Mantener fuente, timestamps y precisiones en cada evidencia.

Evidencia en DATA-SOURCES.md. Revisar cada alta/cambio de fuente y periódicamente antes de release.
