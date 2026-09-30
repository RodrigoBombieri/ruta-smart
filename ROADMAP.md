# Roadmap de desarrollo

Orden base **0 → 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10 → 11 → 12 → 13 → 14**. Testing, seguridad y métricas empiezan en la primera implementación; las fases 11/12 son consolidación, no comienzo de calidad. No se autoriza saltar puertas de datos por haber completado código.

Archivos indicados debajo son **destinos futuros**, salvo documentos existentes. Estimaciones relativas XS=1, S=2, M=3, L=5, XL=8, XXL=13 puntos; no convertir puntos automáticamente a días.

## Fase 0 — Investigación (XL)

- Objetivo: aprobar alcance, evidencia y criterios de viabilidad.
- Tareas: revisar fuentes, licencias, competidores, presupuestos; entrevistar usuarios; preparar pedidos de acceso; definir corredores piloto.
- Dependencias: responsable de producto; respuesta de organismos para cerrar acceso.
- Archivos: `DATA-SOURCES.md`, `PRODUCT.md`, `docs/research/`, `DECISIONS.md`.
- Aceptación: matriz D/T/L sin supuestos ocultos; decisiones de continuidad documentadas; presupuesto objetivo definido. Esta entrega avanza investigación documental; **no cierra** permisos/entrevistas.
- Tests: revisión de links/evidencia y trazabilidad requisito→documento.
- Riesgos: no existe canal consumible; precios sin vigencia. Salida: convenio, fuente licenciada o reducción explícita del alcance.

## Fase 1 — PoC Routing (M)

- Objetivo: elegir proveedor por prueba argentina.
- Tareas: 12 casos de ROUTING; geometría completa, tiempos/tramos, referencias, snapping, costos; ensayo mínimo del renderer.
- Dependencias: F0, cuenta/condiciones aprobadas.
- Archivos: `poc/routing/`, `docs/research/routing-results.md`, ADR-0003.
- Aceptación: rutas válidas, fallos explícitos, derechos de uso compatibles y costos calculados por unidad.
- Tests: NoRoute, origen=destino, cruce internacional, viaje largo y decoder.
- Riesgos: desvíos irreales o límites de rutas largas. No desarrollar aplicación sobre un motor sin aprobar.

## Fase 2 — PoC datos oficiales (XXL)

- Objetivo: validar ingestión sostenible de las fuentes críticas.
- Tareas: muestras reales autorizadas, esquemas, estaciones/precios, alertas/revisiones, eventos/reapertura, geometría RN y cobertura RP; observar actualización durante al menos 7 días como meta.
- Dependencias: F0, accesos/licencias; ruta F1 para cruzar muestra.
- Archivos: `poc/providers/`, `docs/research/source-quality.md`, fixtures sanitizadas.
- Aceptación: D/T/L y calidad aceptables para cada función anunciada; distinguir vacío de error y snapshot truncado.
- Tests: T17–T19/T27, expiración y coordenadas inválidas.
- Riesgos: es la mayor incertidumbre. Si SMN/vialidad no son integrables, detener promesa de MVP completo y decidir beta reducida; ninguna pantalla sustituye esta puerta.

## Fase 3 — Geospatial Engine (XL)

- Objetivo: matching espacial sin falsos vínculos por proximidad.
- Tareas: SRID, índices, intervalos por edge, correlación RN/RP, precisión y corredores.
- Dependencias: F1/F2 con geometrías utilizables.
- Archivos: `src/RutaSmart.Infrastructure/Spatial/`, `tests/Spatial/`, migración inicial GIS.
- Aceptación: intersección exacta, huecos/tangencias y rutas con bucles correctos; cobertura desconocida explícita.
- Tests: T03/T04/T14–T16/T30 y planes SQL.
- Riesgos: referencia kilométrica ausente, geometrías desplazadas. No mapear un km textual a distancia de viaje.

## Fase 4 — Backend básico (L)

- Objetivo: vertical slice autenticada/anónima con snapshot.
- Tareas: ASP.NET Core, DB, contratos, validación, ownership, idempotencia, vehicle/trip save y jobs durables mínimos.
- Dependencias: F3, OpenAPI revisada.
- Archivos: `src/RutaSmart.Api/`, `Core/`, `Infrastructure/`, `tests/Api/`.
- Aceptación: creación/lectura/borrado y aislamiento de usuarios, errores documentados.
- Tests: T10/T22–T24, migraciones y expiración.
- Riesgos: auth innecesariamente compleja; mantener proveedor OIDC y guardado local opcional.

## Fase 5 — Fuel Engine (L)

- Objetivo: consumo, autonomía y candidatos útiles sin prometer precios actuales.
- Tareas: unidades, reserva/inicio, precios nulos/manuales, estaciones por corredor; plan factible multietapa como SHOULD separado.
- Dependencias: F3/F4, inventario aprobado F2.
- Archivos: `Core/Fuel/`, `Infrastructure/Fuel/`, `tests/Fuel/`.
- Aceptación: distingue consumo/compras, precio/stock y candidatos/recomendación validada.
- Tests: T06/T07/T13/T15/T26/T31.
- Riesgos: ausencia de estaciones actualizadas; degradar explícitamente plan y no inventar alcance.

## Fase 6 — Weather Route Matching (XL)

- Objetivo: intersectar alertas con ventanas de paso.
- Tareas: timeline, ingesta autorizada, cancelación, polígonos/zonas; complemento forecast en lote si aprobado.
- Dependencias: F2/F3/F4, pausas relevantes de F5.
- Archivos: `Core/Timeline/`, `Core/Weather/`, adaptadores y `tests/Weather/`.
- Aceptación: horarios e intervalos de fuente separados, horizonte/cobertura explícitos.
- Tests: T03–T05/T08/T17/T20/T21/T30.
- Riesgos: no hay feed o precisión insuficiente; no sustituir alerta SMN por forecast externo.

## Fase 7 — Road Conditions (XL)

- Objetivo: avisos viales aplicables y trazables.
- Tareas: fuente normalizada, vía/sentido/categoría, ubicación incierta, conflictos, cortes sin fin y reaperturas.
- Dependencias: F2/F3/F4.
- Archivos: `Core/RoadConditions/`, adaptadores y `tests/RoadConditions/`.
- Aceptación: ningún falso corte por vía paralela, no inferir reapertura por fallo de fuente.
- Tests: T09/T16–T19/T27.
- Riesgos: texto no georreferenciado y operación editorial inviable; acotar corredores.

## Fase 8 — Trip Assessment (L)

- Objetivo: integrar razones y suficiencia sin score mágico.
- Tareas: tabla de decisiones, ruleVersion, snapshots, presupuestos y DTO completo.
- Dependencias: F5/F6/F7.
- Archivos: `Core/Assessment/`, `tests/Assessment/`, `docs/openapi.json`.
- Aceptación: jamás favorable con fuente crítica faltante; corte conocido visible aunque falte clima.
- Tests: T01/T02/T08/T12/T28/T29/T31, determinismo.
- Riesgos: estado general oculta incertidumbre; textos y coverage siempre juntos.

## Fase 9 — Flutter MVP (XL)

- Objetivo: flujo usable de punta a punta.
- Tareas: formularios, vehículo/guardados, cards, estados parciales, accesibilidad, auth opcional y deep links.
- Dependencias: F4/F8; mapa inicial de F1.
- Archivos: `mobile/rutasmart/lib/`, `test/`, `integration_test/`.
- Aceptación: usuario completa flujo sin cuenta/GPS y comprende fuente/fecha.
- Tests: widgets con nulos/errores; journey y navegación instalada/no instalada.
- Riesgos: UI compleja; test temprano con 5 personas, sin certificar demanda por esa muestra.

## Fase 10 — Mapas (M)

- Objetivo: visualizar sin saturación ni violaciones de licencia.
- Tareas: provider widget, tiles/atribución, capas, selección, filtros, clustering y rendimiento móvil.
- Dependencias: F1/F3/F9 y licencia de tiles.
- Archivos: `mobile/rutasmart/lib/features/map/`, pruebas y ADR de mapas.
- Aceptación: línea/alertas/estaciones legibles, lista alternativa, atribuciones visibles.
- Tests: memoria/polilínea larga, huecos, zoom y fuente de tiles caída.
- Riesgos: renderer/plugin incompatible, costos por tiles/MAU.

## Fase 11 — Testing integral (L)

- Objetivo: cerrar comportamiento crítico, fallos y regresiones.
- Tareas: corpus T01–T31, carga sintética, contrato, seguridad, dispositivos y restauración.
- Dependencias: F8–F10; tests existentes de cada fase.
- Archivos: `tests/`, `mobile/.../integration_test/`, informe QA.
- Aceptación: casos críticos aprobados, sin errores impeditivos, presupuestos externos respetados.
- Tests: matriz completa de TESTING y fault injection.
- Riesgos: mocks demasiado optimistas; contrastar contratos con muestras autorizadas separadamente.

## Fase 12 — Observabilidad (M)

- Objetivo: diagnosticar frescura, fallos y costo.
- Tareas: consolidar métricas iniciales, dashboard, alertas y runbooks; ensayo de caída/recuperación.
- Dependencias: F4 en adelante, F11 para cierre.
- Archivos: `ops/observability/`, `ops/runbooks/`, DEPLOYMENT.
- Aceptación: detectar proveedor vencido y job frenado sin mirar logs con PII.
- Tests: fallos sintéticos, métricas de cache/costo, verificación de redacción de logs.
- Riesgos: alert fatigue y gasto de logs; agrupar y limitar cardinalidad.

## Fase 13 — Beta (L)

- Objetivo: validar utilidad/cobertura en corredores elegidos.
- Tareas: staging/beta, consentimiento, pruebas de comprensión, incidencias, costo real y análisis de discrepancias.
- Dependencias: F11/F12, permisos de fuentes/privacidad resueltos.
- Archivos: `docs/beta/`, checklist de release y revisión de ADR.
- Aceptación: usuarios distinguen datos faltantes, costos operativos sostenibles, no falsos favorables en corpus/incidentes revisados.
- Tests: journeys reales controlados por personas detenidas; no tests de conducción riesgosa.
- Riesgos: sesgo regional y demanda no demostrada. Expandir cobertura por evidencia, no por cantidad de usuarios.

## Fase 14 — Producción (L)

- Objetivo: lanzamiento operable con promesa acotada y verificable.
- Tareas: cotización final, publicación móvil, backups/restores, rollback, soporte, límites y revisión de datos/licencias.
- Dependencias: beta aceptada, presupuesto y responsable operativo.
- Archivos: `ops/deploy/`, `docs/releases/`, registro de soporte.
- Aceptación: restore ensayado, SLO medido, fuentes habilitadas, estado público y política de privacidad accesibles.
- Tests: smoke tras deploy, rollback, borrado de cuenta y expiración.
- Riesgos: operador único y proveedor cambia contrato; mantener límites y modo degradado, no SLA comercial aún no financiado.

## Estimación y camino crítico

Estimación orientativa propia: **18–28 semanas de trabajo efectivo para una persona experimentada**, suponiendo acceso a datos y dedicación sostenida; sumar 25–35% de contingencia si las PoC revelan deuda de calidad. No incluye espera de convenios, asesoría especializada, costos de tiendas ni expansión a iOS. No es compromiso de entrega; recalibrar al terminar F2 y F8.

Mayor complejidad/incertidumbre: datos oficiales y referencia vial; después GIS/tiempo. Menor: formularios, consumo aritmético y deep links. Camino crítico: acceso/legalidad → muestra real → matching → evaluación honesta → validación de usuario. No priorizar pulido visual antes de resolverlo.
