# Routing y mapas: selección y prueba de concepto

Revisión: 2026-09-29. **Recomendación provisional: ORS alojado para PoC**, condicionado a plan vigente, derechos de cache/uso comercial y desempeño argentino. Si no supera esas puertas, comparar GraphHopper y Mapbox antes de decidir. OSRM propio es alternativa operativa posterior, no requisito inicial. Ningún proveedor fue probado con credenciales en esta investigación.

## Comparativa

Todas las opciones siguientes permiten obtener ruta y duración. No hay evidencia experimental en esta etapa para ordenar su precisión en Argentina. Los motores OSM dependen de la calidad del extracto/perfil; los comerciales requieren comprobar disponibilidad de funcionalidades por país y producto.

| Opción | Geometría / instrucciones / matriz | Costo y límites documentados | Self-hosting y dependencia | Evaluación |
|---|---|---|---|---|
| OpenRouteService | Geometría completa JSON/GeoJSON; instrucciones; Matrix | Restricciones públicas: conducción 6.000 km; alternativas 100 km; avoid areas 150 km; matriz estándar 3.500 pares por request, dinámica 25. Cuotas de cuenta y precio actuales pendientes | Backend abierto autoalojable; dependencia baja-media | Primer candidato; las restricciones de alternativas/evitación importan para viajes largos |
| OSRM | polyline/polyline6/GeoJSON full; steps; Table | Software abierto; infraestructura y operación a cargo propio. Límites configurables; demo sin compromiso productivo aquí verificado | Sí; dependencia baja, operación de grafo alta | Buen motor autoalojado si se justifica; no usar demo como backend de beta |
| GraphHopper | Puntos codificados/GeoJSON, instrucciones; Matrix comercial | Free 500 créditos/día, no comercial. Basic observado €69/mes, 5.000 créditos/día, 30 ubicaciones, 1 req/s y 100 créditos/min; IVA aparte | Motor abierto; funciones y SLA del SaaS no equivalen al motor OSS | Alternativa clara con costo fijo; créditos no equivalen siempre a requests |
| Valhalla | Shape, instrucciones y matriz de tiempo/distancia | Motor abierto; costo de infraestructura; límites del despliegue, sin cuota SaaS universal | Sí; dependencia baja, operación alta | Potente; complejidad no justificada para el primer MVP |
| Google | Routes API: polyline, steps y Compute Route Matrix | Essentials: 10.000 eventos gratis/mes por SKU; siguiente tramo USD 5/1.000. Matriz factura elementos. Otras opciones activan otro SKU | No motor autoalojable; dependencia alta por condiciones de datos/mapa | Usar Routes para evaluación nueva; Directions aparece como legacy |
| HERE | Routing y Matrix; geometría e instrucciones según producto | Limited sin medio de pago: 1.000 requests/día; routing car 10 RPS; matriz 1 RPS. Tarifa monetaria no confirmada | Sin self-host equivalente asumido; contrato comercial | Restricciones de usos excluidos exigen confirmar elegibilidad; no seleccionar sin ello |
| Mapbox | polyline/polyline6/GeoJSON, steps, annotations; Matrix | Directions: 100.000 requests/mes gratis; siguiente tramo USD 2/1.000. Documentación: 300 req/min, driving hasta 10.000 km; Matrix hasta 25 coordenadas o 10 con tráfico | Sin motor SaaS equivalente autoalojable asumido; dependencia media-alta | Buen candidato económico, sujeto a derechos de visualización/persistencia |

Fuentes primarias: [ORS restricciones](https://openrouteservice.org/restrictions/), [ORS directions](https://giscience.github.io/openrouteservice/api-reference/endpoints/directions/), [planes ORS/HeiGIT](https://openrouteservice.org/plans/), [OSRM API](https://project-osrm.org/docs/v5.24.0/api/), [GraphHopper pricing](https://www.graphhopper.com/pricing/), [GraphHopper routing](https://docs.graphhopper.com/openapi/routing), [Valhalla oficial](https://github.com/valhalla/valhalla), [Google precios](https://developers.google.com/maps/billing-and-pricing/pricing), [Google billing](https://developers.google.com/maps/documentation/routes/usage-and-billing), [HERE límites y exclusiones](https://www.here.com/get-started/pricing/rps-limits-excluded-use-cases), [Mapbox precios](https://www.mapbox.com/pricing), [Mapbox Directions](https://docs.mapbox.com/api/navigation/directions/), [Mapbox Matrix](https://docs.mapbox.com/api/navigation/matrix/).

Los importes son los publicados observados, no cotizaciones ni presupuesto final. No se adopta la antigua cuota ORS 2.000/40 encontrada en staging como condición vigente del plan nuevo. El portal actual redirigió a HeiGIT y no expuso el detalle en esta lectura.

## Compatibilidad legal y reemplazo

[Google Routes policies](https://developers.google.com/maps/documentation/routes/policies) exige Google Map cuando se muestran sus resultados en un mapa y restringe cache. Por ello **Google Routes + MapLibre no es el stack base aprobado**. No persistir su geometría como si fuera un activo libre. Las [condiciones de Mapbox](https://www.mapbox.com/legal/product-terms) y las [de ORS](https://openrouteservice.org/terms-of-service/) requieren revisión del contrato efectivo y uso concreto antes de producción.

Una interfaz no elimina licencias. Cada adaptador declara capacidades: geometría completa, tiempos por tramo, matriz, referencias viales, tráfico, restricciones de países, retención máxima y renderers permitidos. Si cambia el proveedor, se recalcula ruta, timeline y assessment; no se mezcla duración de A con geometría de B. Mantener proveedor, versión/perfil, instante, hash e instrucciones de atribución.

## Contrato mínimo de routing

Entrada: dos puntos confirmados, perfil automóvil, preferencias soportadas y salida si el motor realmente la usa. Salida: línea ordenada completa, distancia en metros, duración en segundos, extremos snapped, desplazamiento de snapping, tramos con offsets y tiempos cuando estén disponibles, referencias viales opcionales y limitaciones.

No denominar tráfico en vivo a tiempos estáticos. No obtener la ruta oficial recorrida por un vehículo a partir de una ruta propuesta. Matriz sólo devuelve costos; para aprobar desvíos hace falta validar geometría/conectividad de candidatos finales.

## PoC comparativa y criterio de elección

Set propuesto de 12 trayectos: Urdinarrain–Gualeguaychú; Urdinarrain–Bariloche; Buenos Aires–Rosario; Rosario–Córdoba; Córdoba–Mendoza; Neuquén–Bariloche; Bariloche–Esquel; Salta–Cafayate; Corrientes–Posadas; Río Gallegos–Ushuaia; un acceso rural identificado manualmente y un par desconectado sintético. Estos son casos de evaluación, no rutas ya calculadas. No reutilizar los 1.450 km del ejemplo del usuario como dato real.

Para cada candidato capturar: éxito, distancia, duración, puntos, referencia RN/RP, latencia, costo por request/elemento, timestamp del grafo si existe, desvíos y restricciones. Revisar mapas y publicaciones de autoridad donde estén permitidos; desacuerdo entre motores no demuestra cuál es correcto. Excluir licencias que impidan el producto antes de puntuar.

Puerta propuesta: sin saltos geométricos ni cruces prohibidos en todos los casos aceptados; geometría completa y perfil identificables; errores explícitos en casos imposibles; p95 de routing objetivo <5 s en benchmark controlado; costos dentro del presupuesto acordado. Comparar precisión por checklist humano, no por parecido de ETA entre proveedores. Una variación de distancia >15% entre motores es disparador de revisión, no rechazo automático.

Territorio: el MVP analiza itinerarios íntegramente dentro de Argentina. Río Gallegos–Ushuaia puede requerir Chile/ferry: detectarlo y devolver no soportado, nunca ocultar la salida del país. Verificar la línea completa contra territorio, no sólo extremos. Vías rurales, estacionales y accesos privados deben quedar señalados o fuera de alcance si la evidencia es insuficiente.

## Mapas: capas distintas

| Alternativa | Qué es | Costos/licencias | Decisión |
|---|---|---|---|
| OpenStreetMap | Base de datos comunitaria | ODbL, atribución y obligaciones sobre bases derivadas; no hosting ilimitado incluido | Fuente externa cartográfica candidata |
| MapLibre | Renderer abierto | Sin tarifa propia por request; tiles, estilos, fuentes y hosting separados | Renderer preferido, validar plugin Flutter |
| Google Maps | SDK + servicios + datos | Tarifas por producto; SDK mobile y mapas web tienen regímenes distintos; términos propietarios | Alternativa completa si se elige su ecosistema |
| Mapbox | SDK, tiles y APIs comerciales | Maps mobile suele facturar por MAU; Directions por requests; no confundir con Navigation SDK | Cotizar paquete real y validar licencias |

Fuentes: [MapLibre](https://maplibre.org/), [licencia OSM](https://www.openstreetmap.org/copyright), [política de tiles OSM](https://operations.osmfoundation.org/policies/tiles/), [Google tarifario](https://developers.google.com/maps/billing-and-pricing/pricing), [Mapbox tarifario](https://www.mapbox.com/pricing). El servidor comunitario OSM es best effort y prohíbe descarga masiva/offline: no será infraestructura de producción. Atribución visible en todos los mapas.

Candidato para tiles con MapLibre: MapTiler u otro proveedor con permiso comercial. [Pricing](https://www.maptiler.com/cloud/pricing/?trk=public_post_comment-text) y [términos](https://www.maptiler.com/terms/cloud/) diferencian evaluación/no comercial de uso comercial. Tarifa final pendiente. No asumir que cualquier estilo/tiles de Mapbox se puede usar libremente en MapLibre.

Reemplazo práctico: encapsular widget del mapa, modelos de capas propios y configuración de tiles/atribución. No fabricar un SDK cartográfico abstracto para todas las funciones de todos los proveedores. La PoC debe probar polilínea larga, polígonos, clustering, selección y memoria en Android de gama media; iOS requiere prueba y entorno macOS posterior.

## Navegación externa

[Waze Deep Links](https://developers.google.com/waze/deeplinks): destino mediante `https://waze.com/ul?ll={lat},{lon}&navigate=yes`. [Google Maps URLs](https://developers.google.com/maps/documentation/urls/get-started): directions mediante URL documentada con origen/destino, codificados correctamente. Probar fallback web cuando no está instalada la app.

El navegador **puede elegir otro recorrido**. Estos enlaces no transfieren de forma fiable toda la polyline analizada. Mostrar junto al botón: “El navegador puede proponer otra ruta. Este informe corresponde al recorrido mostrado”. Si el usuario cambia la ruta o la hora, debe reevaluar. No asumir que una apertura de Waze informa a RutaSmart el recorrido finalmente seguido.
