# Plan maestro

Estado revisado el **9 de octubre de 2026**. RutaSmart prepara un informe antes de viajar; la navegación se delega a Google Maps o Waze.

## Implementado

- Flutter web y proyecto Android: planificación, vehículo, informes, mapa y guardado local.
- API .NET: sesiones anónimas, validación, versiones, idempotencia y borrado.
- PostgreSQL/PostGIS: informes, geometrías y snapshots de fuentes; expiración de sesiones a 24 horas.
- GeoRef, routing OSRM de prueba, alertas CAP del SMN y catálogo de estaciones de Energía.
- Consumo, reserva, costos opcionales, horarios con pausas y cruce espacial/temporal de alertas.
- Pruebas automatizadas, CI y compilación web/Android.

## Pendiente, en orden

| Prioridad | Entrega | Condición de cierre |
|---|---|---|
| 1 | Estado vial | Fuente utilizable y correspondencia verificable con vía, tramo y sentido |
| 2 | Routing nacional | Proveedor apto para el uso previsto y corpus argentino validado |
| 3 | Plan de carga | Desvíos por carretera, autonomía entre paradas y confirmación del usuario |
| 4 | Cuenta opcional | OIDC, sincronización, conflictos y borrado de cuenta |
| 5 | Operación y beta | Dispositivos reales, comprensión del informe, restauración de backups y medición de rendimiento |

El pronóstico complementario y la calibración de ETA amplían el análisis después de estabilizar estas funciones. Hidrología, notificaciones y flotas quedan fuera de esta entrega.

## Criterios del producto

1. No presentar falta de datos como condiciones favorables.
2. Mostrar fecha, fuente e incertidumbre junto al resultado.
3. Separar demostraciones sintéticas de información real.
4. Conservar la fecha declarada de los precios; no garantizar stock ni acceso.
5. No anunciar cobertura nacional ni producción hasta validarlas.

La versión local es utilizable, pero **el MVP completo aún no está terminado**. El resultado de las verificaciones está en [TESTING.md](TESTING.md).
