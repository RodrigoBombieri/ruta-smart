# Plan maestro

Estado revisado el **10 de octubre de 2026**. RutaSmart prepara un informe antes de viajar; la navegación se delega a Google Maps o Waze.

## Implementado

- Flutter web y proyecto Android: planificación, vehículo, informes, mapa y guardado local.
- API .NET: sesiones anónimas, validación, versiones, idempotencia y borrado.
- PostgreSQL/PostGIS: informes, geometrías y snapshots de fuentes; expiración de sesiones a 24 horas.
- GeoRef, routing OSRM de prueba, alertas CAP del SMN y catálogo de estaciones de Energía.
- Consumo, reserva, costos opcionales, horarios con pausas y cruce espacial/temporal de alertas.
- Pruebas automatizadas, CI y compilación web/Android.
- Routing nacional: ocho recorridos regionales y aviso de tránsito internacional.
- Vialidad: partes públicos con coincidencia aproximada por ruta nacional/provincia.
- Plan de carga por carretera: reserva por etapa, confirmación y nuevo informe con pausas.

## Pendiente, en orden

| Prioridad | Entrega | Condición de cierre |
|---|---|---|
| 1 | Precisión vial | Georreferenciar tramo y sentido; hoy la coincidencia es aproximada |
| 2 | Routing productivo | Proveedor con capacidad y condiciones para el uso previsto |
| 3 | Cobertura de cargas | Ampliar selección y verificar operación/accesos en campo |
| 4 | Cuenta opcional | OIDC, sincronización, conflictos y borrado de cuenta |
| 5 | Operación y beta | Dispositivos reales, comprensión del informe, restauración de backups y medición de rendimiento |

El pronóstico complementario y la calibración de ETA amplían el análisis después de estabilizar estas funciones. Hidrología, notificaciones y flotas quedan fuera de esta entrega.

## Criterios del producto

1. No presentar falta de datos como condiciones favorables.
2. Mostrar fecha, fuente e incertidumbre junto al resultado.
3. Separar demostraciones sintéticas de información real.
4. Conservar la fecha declarada de los precios; no garantizar stock ni acceso.
5. Diferenciar alcance nacional de pruebas, disponibilidad por carretera y producción.

Las tres funciones de esta etapa están implementadas con los límites anteriores. La beta productiva sigue pendiente. El resultado de las verificaciones está en [TESTING.md](TESTING.md).
