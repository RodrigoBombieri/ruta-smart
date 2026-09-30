# ADR-0006 — Privacidad y extensión hidrológica opcional

Fecha: 2026-09-29. Estado: adoptada para diseño.

Contexto: los recorridos revelan hábitos; AlertaRío es un proyecto independiente y la proximidad hídrica no prueba afectación vial.

Decisión: análisis anónimo temporal, guardado local y cuenta opcional; sin tracking continuo. Hidrología fuera del MVP, mediante futuro contrato independiente de impactos y evidencia, sin compartir DB.

Alternativas rechazadas: exigir cuenta/GPS, guardar cada movimiento, consultar INA por cada viaje sin caso validado, vincular bases de ambos proyectos.

Consecuencias: futuras notificaciones/mode viaje requieren consentimiento y operación propios. El contrato futuro incluye geometría, validez, unidad/datum cuando corresponda, relación con infraestructura y fuente original.

Revisar privacidad antes de nuevas finalidades y datos hidrológicos antes de cambiar estados por esos eventos. SECURITY.md y VISION.md detallan límites.
