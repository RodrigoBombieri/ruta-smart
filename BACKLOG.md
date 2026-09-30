# Backlog priorizado

Historias y tareas de diseño, no issues publicados. Complejidad relativa 1/2/3/5/8/13; reevaluar después de PoC. `G/W/T` significa Given/When/Then. Toda historia terminada requiere documentación, pruebas apropiadas y métricas sin PII.

| Épica | Feature / historia | Prioridad / tamaño | Tareas concretas | Acceptance criteria G/W/T | Dependencia |
|---|---|---|---|---|---|
| E01 Evidencia | US01 Como responsable quiero conocer permisos y cobertura | MUST / 8 | Catalogar D/T/L; revisar términos; pedir acceso bajo autorización; definir gate | Given una fuente candidata, When se habilita, Then existe evidencia de acceso/reuso y alcance, no sólo URL pública | F0 |
| E01 Evidencia | US02 Quiero saber si precios/estaciones son utilizables | MUST / 5 | Obtener CSV autorizado; perfilar edades/coordenadas/duplicados; informe provincial | Given lote real, When se audita, Then hay métricas reproducibles y cuarentena; no se llama actual a precio viejo | US01 |
| E02 Ruta | US03 Como viajero quiero elegir localidades sin confusiones | MUST / 3 | GeoRef adapter, cache, UI de provincia y pin confirmado | Given dos localidades homónimas, When busco, Then puedo elegir provincia/punto y ver precisión | US01 |
| E02 Ruta | US04 Quiero obtener un recorrido evaluable | MUST / 5 | PoC proveedor; decoder; capability checks; ruta versionada | Given origen/destino distintos soportados, When analizo, Then recibo línea completa, metros, segundos y fuente | US03 |
| E02 Ruta | US05 Quiero entender que un viaje no es soportado | MUST / 2 | NoRoute, same point, país/ferry, errores de snapping | Given recorrido que sale del país, When valido, Then no se presenta como ruta doméstica cubierta | US04 |
| E03 GIS | US06 Quiero conocer RN/RP atravesadas | MUST / 8 | Ingesta red; match por vía/sentido; concurrentes; cobertura | Given rutas de igual número con distinta jurisdicción, When correlaciono, Then no se mezclan y hay tramos desconocidos visibles | US01/04 |
| E03 GIS | US07 Quiero sólo avisos relevantes | MUST / 8 | Corredores, intersección por edge, huecos/bucles, índices | Given alerta cercana sin intersección, When analizo, Then no aparece como afectación del recorrido | US04 |
| E04 Tiempo | US08 Quiero relacionar hora de paso y condiciones | MUST / 5 | Acumulados, pauses, zona, bandas, horizonte | Given alerta vencida antes de llegada temprana, When evalúo, Then no se muestra activa para mi paso | US07 |
| E04 Tiempo | US09 Quiero editar pausas previstas | SHOULD / 3 | Formulario pausa, validate distance, nueva versión | Given pausa agregada, When recalculo, Then todos los avisos posteriores usan la nueva ETA | US08 |
| E05 Clima | US10 Quiero alertas oficiales intersectadas | MUST / 8 | Feed autorizado, áreas/revisiones, validez, enlace | Given alerta válida cruzada, When analizo, Then aparece fenómeno/nivel/tramo/ventana y fuente | US01/07/08 |
| E05 Clima | US11 Quiero pronóstico durante el recorrido | SHOULD / 5 | Proveedor aprobado, celdas/lotes, cache, unidades | Given forecast parcial, When analizo, Then se muestran partes cubiertas y faltantes sin extrapolar | US08 |
| E05 Clima | US12 Quiero ver cuándo falta información | MUST / 3 | Coverage por dimensión, reloj, stale strategy | Given timeout y snapshot vencido, When analizo, Then nunca obtengo favorable por lista vacía | US10 |
| E06 Vialidad | US13 Quiero saber cortes/restricciones aplicables | MUST / 8 | Adapter autorizado, km calibrados, sentido/vehículo, reapertura | Given corte sobre vía paralela, When analizo, Then no bloquea mi ruta; si aplica, se explica | US01/06/08 |
| E06 Vialidad | US14 Quiero distinguir reporte viejo o ambiguo | MUST / 5 | Frescura, precisión, conflictos, fin desconocido | Given cierre viejo sin reapertura, When actualizo, Then se informa vigencia no confirmada, nunca abierto inventado | US13 |
| E07 Vehículo | US15 Quiero guardar consumo y tanque | MUST / 2 | Formulario manual, snapshot, local/cloud, validación | Given consumo 7,5 y tanque 47, When guardo, Then persisten unidades y puedo corregirlos sin alterar informes previos | US04 |
| E07 Combustible | US16 Quiero calcular litros/costo | MUST / 3 | Fórmulas, referencia manual, precios nulos, redondeo | Given 1.200 km y 7,5 L/100, When calculo, Then son 90 L; sin precio costo null | US15/02 |
| E07 Combustible | US17 Quiero ver estaciones útiles para autonomía | MUST / 5 | Inicio/reserva, producto, corredor, progreso, antigüedad | Given estación incompatible, When busco, Then no es candidata apta para mi combustible | US07/15/02 |
| E07 Combustible | US18 Quiero recomendaciones de carga explicadas | SHOULD / 8 | Grafo factible acotado, accesos/matriz, lookahead, ranking | Given estación barata con gran desvío o callejón sin siguiente carga, When rankeo, Then no gana por precio ni genera plan inviable | US17/04 |
| E08 Evaluación | US19 Quiero un resumen entendible | MUST / 5 | Reglas versionadas, razones, cobertura, DTO | Given corte y clima caído, When evalúo, Then veo adversas más cobertura parcial, no sólo uno de los dos | US10/13/16 |
| E08 API | US20 Quiero evitar duplicados y filtraciones | MUST / 5 | Idempotencia, auth/owner, límites, Problem Details | Given key/body repetidos, When reintento, Then un análisis; dado trip ajeno, Then 404 | US04 |
| E09 Mobile | US21 Quiero analizar sin registrarme ni habilitar GPS | MUST / 5 | Sesión anónima, búsqueda manual, formulario, UI fallos | Given permiso GPS denegado, When ingreso localidades, Then completo el flujo | US03/19/20 |
| E09 Mobile | US22 Quiero guardar y recuperar viajes | MUST / 3 | Local save, cloud opt-in, expiración, reanálisis | Given snapshot vencido, When lo abro, Then veo fecha y acción de actualización, no informe actual | US21 |
| E09 Mapas | US23 Quiero localizar alertas y estaciones | MUST / 3 | Widget, layers, selección, atribución y fallback | Given mapa caído, When consulto, Then las tarjetas conservan información y cobertura | US07/21 |
| E09 Mapas | US24 Quiero un mapa sin saturación | SHOULD / 3 | Clustering, filtros, límites y accesibilidad | Given muchos puntos, When alejo zoom, Then agrupación legible y cortes accesibles en listado | US23 |
| E09 Navegación | US25 Quiero seguir en mi navegador | MUST / 2 | Deep links allowlist, encoding, fallback | Given Waze no instalado, When abro, Then hay alternativa web; se avisa ruta posiblemente distinta | US21 |
| E10 Seguridad | US26 Quiero borrar mis datos | MUST / 3 | Purga, tombstones, backups, UI, exportación razonable | Given solicitud de borrado, When se procesa, Then deja de ser accesible y restore no resucita datos | US20/22 |
| E10 Operación | US27 Quiero conocer salud de fuentes | MUST / 3 | Métricas/tabla, last-success vs source-time, runbooks | Given ingesta exitosa de dato viejo, When miro salud, Then distingo acceso sano y frescura deficiente | US01/12/14 |
| E10 Operación | US28 Quiero limitar gasto y recuperarme | MUST / 5 | Budgets, circuit breakers, backups, restore, deploy/rollback | Given presupuesto agotado, When hay más requests, Then degradación explícita; restore cumple objetivo ensayado | US20/27 |
| E11 Calidad | US29 Quiero resultados confiables frente a casos límite | MUST / 8 | Corpus T01–T31, mocks, integración PostGIS, Flutter, carga | Given corpus versionado, When CI corre sin Internet, Then resultados deterministas y críticos verdes | Todas MUST implementadas |
| E12 Evolución | US30 Quiero cambios antes de salir | COULD / 5 | Scheduler, opt-in FCM, diff/dedupe, quiet hours | Given revisión relevante nueva en viaje seguido, When reanaliza, Then un aviso útil con informe actualizado | US19/22/27 |
| E12 Evolución | US31 Quiero contexto hidrológico válido | NOT NOW / 13 | Contrato AlertaRío/INA, vínculo infraestructura/umbral, experto | Given río cercano sin vínculo vial, When evalúo, Then no invento inundación/corte | Acceso y validación especializada |
| E12 Evolución | US32 Quiero consumir motor desde una empresa | NOT NOW / 8 | Tenancy, claves, metering, SLA/licencias, B2B schema | Given tercero autorizado, When analiza, Then recibe mismas razones trazables con aislamiento | MVP productivo validado |

## Cortes de entrega

1. Investigación: US01–02 y PoC US04. No anunciar MVP todavía.
2. Slice técnico: US03–08/15–17/20 con mocks claramente identificados.
3. MVP completo: todas MUST con fuentes reales habilitadas; SHOULD elegidas según capacidad, sin disfrazar candidatos como recomendaciones.
4. Beta: US29, comprensión y operación antes de expansión territorial.
5. Evolución: US30–32 sólo con evidencia de demanda y capacidad.

Las historias grandes se dividen al cerrar F2 por fuente/corredor y test observable. Evitar comprometer en sprint una historia bloqueada por una licencia o un feed no disponible.
