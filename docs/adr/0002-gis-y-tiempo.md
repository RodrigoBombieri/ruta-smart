# ADR-0002 — Intersección espacial y temporal

Fecha: 2026-09-29. Estado: adoptada para diseño.

Contexto: un evento cercano puede no afectar la ruta y uno intersectado puede vencer antes del paso.

Decisión: PostGIS para prefiltro/cálculo exacto; línea original con edges ordenados y acumulados; matching temporal por intervalo de exposición y banda ETA. Mantener geometría visual simplificada separada.

Alternativas descartadas: filtro por provincia; radio único para todos los eventos; ETA proporcional a distancia total como método preciso; proyección global que pierde pasadas de un bucle.

Consecuencias: mayor necesidad de fixtures geométricas, CRS correcto y tiempos por tramo; fallback explícito si faltan. Cercanía de estación no confirma acceso.

Revisar tolerancias y bandas tras PoC/validación de campo. Referencias y algoritmos en GEOSPATIAL.md y TRIP-ANALYSIS.md.
