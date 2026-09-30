# ADR-0003 — Routing sustituible y licencias compatibles

Fecha: 2026-09-29. Estado: provisional, sin proveedor contratado.

Contexto: geometría/duración son esenciales; autoalojar routing aumenta operación y las licencias pueden impedir combinar servicios.

Decisión propuesta: probar ORS alojado primero; contrastar GraphHopper/Mapbox; MapLibre como renderer preferido con tiles autorizados. OSRM/Valhalla propios sólo si costos/funcionalidad justifican operación.

Alternativas: Google Routes exige compatibilidad con sus políticas de visualización/cache; HERE necesita confirmar elegibilidad contractual. No elegir sólo por cuota gratis.

Consecuencias: contrato canónico y capability flags, con recomputación completa al cambiar motor. Interfaz no garantiza portabilidad legal de datos. No se fija producción hasta terminar corpus argentino, precio y derechos.

Evidencia/condiciones: ROUTING.md. Revisión obligatoria F1 y antes de beta.
