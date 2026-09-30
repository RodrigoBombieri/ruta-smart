# ADR-0001 — Monolito modular

Fecha: 2026-09-29. Estado: adoptada para diseño.

Contexto: un desarrollador necesita integrar fuentes inestables y GIS sin multiplicar operación.

Decisión: .NET 10 LTS, módulos internos, PostgreSQL/PostGIS, API y jobs del mismo código. Separar proceso worker sólo si recursos/reinicios lo requieren.

Alternativas: microservicios, broker y Kubernetes aumentan despliegues/consistencia/monitoreo; no hay escala observada que los justifique. Redis diferido.

Consecuencias: transacciones y diagnóstico sencillos; requiere disciplina de límites internos. No implica un único archivo ni acoplar DTO externos al dominio.

Revisar cuando exista medición de cuello de botella, equipos independientes o necesidad concreta de aislamiento. Evidencia tecnológica en ARCHITECTURE.md.
