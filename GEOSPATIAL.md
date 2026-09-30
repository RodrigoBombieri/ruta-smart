# Motor geoespacial

Diseño propuesto, pendiente de PoC. PostGIS centraliza operaciones espaciales; .NET coordina el dominio. No se desarrollará un motor de routing.

## Coordenadas, medidas y geometría

Guardar geometría WGS84, SRID 4326, orden **longitud, latitud** en GeoJSON. Distancias métricas mediante geography o proyección local adecuada. Nunca usar grados como metros ni EPSG:3857 para cálculos precisos de largas distancias. Para Argentina extensa, dividir por tramos y usar cálculos geodésicos/locales; una única zona UTM no es apropiada para todo el país.

Ruta: línea original ordenada + vértices/edges ordenados con distancia acumulada y tiempo acumulado. Conservar repeticiones de coordenadas si representan pasadas distintas. Reparar/validar geometrías de proveedor con reporte; no corregir silenciosamente polígonos inválidos. No usar una simplificación visual para resolver intersecciones críticas.

Línea del renderer simplificada con tolerancia propuesta 20–50 m según zoom. Original usada en cálculos. Tolerancias se ensayan con polígonos estrechos, límites y meandros; no se eliminan intersecciones reales para ahorrar memoria.

## Corredor y relevancia

El corredor es un **filtro de candidatos**, no una prueba universal de afectación. Parámetros iniciales para calibrar:

| Objeto | Prefiltro | Decisión final |
|---|---|---|
| Alerta poligonal oficial | bbox contra línea; contexto opcional a 10 km | Intersección de línea real y polígono + solapamiento temporal |
| Estación | 5 km lateral, ampliar a 10/25 km en zonas sin candidatos | Acceso vial, sentido, desvío, producto y autonomía |
| Evento vial georreferenciado | 100–250 m como tolerancia técnica propuesta | Identidad de vía, tramo, sentido y conectividad; proximidad no basta |
| Aviso vial aproximado | Hasta 5 km para revisión | “Posible afectación” si falta correspondencia verificable |
| Hidrología futura | Corredor exploratorio 25 km | Intersección de área de impacto o infraestructura asociada; nunca radio solo |

Una alerta a 300 km se descarta. Una a 2 km sin intersección puede ser contexto cercano, separada de “afecta el recorrido”. Las tolerancias no son umbrales de seguridad.

Consultas conceptuales: `ST_Intersects(route.geom, alert.geom)` para polígonos; `ST_DWithin(station.geog, segment.geog, radius_m)` para candidatos métricos. Ver [ST_DWithin](https://postgis.net/docs/ST_DWithin.html). Para mostrar corredores puede usarse buffer en proyección apropiada por tramo y unión validada; consultar distancias no requiere materializar cada buffer.

## Matching vial

1. Obtener `ref`/nombre/ID del proveedor, si disponible. Normalizar RN 14, RP 14 y provincia como identidades distintas; conservar valor original.
2. Comparar tramos con red oficial en un corredor estrecho usando longitud compartida, orientación y continuidad, no el punto más cercano aislado.
3. Mantener múltiples referencias en tramos concurrentes. Un cruce a distinto nivel no implica transitar la otra vía.
4. Para eventos “RN X km A–B” hace falta referencia kilométrica oficial calibrada, jurisdicción, ramal, sentido y versión. **Km de ruta no es distancia acumulada desde el origen del viaje.**
5. Si no hay calibración, asociar localidades/tramo textual como precisión aproximada; no inventar coordenadas del corte. Se muestra incertidumbre y fuente.

RoadCoverage expresa longitudes soportadas, no soportadas y ambiguas. “Detectar rutas atravesadas” se entrega con porcentaje de distancia identificada y segmentos desconocidos; no con un listado aparentemente completo extraído sólo de instrucciones textuales.

## Intersección y timeline espacial

Dividir la ruta en edges ordenados por recorrido, no ordenar resultados de intersección por coordenada. Intersectar cada edge con polígonos; extraer intervalos de fracción `[a,b]` dentro del edge, incluidos multipolígonos y huecos. Traducirlos a distancia y tiempo acumulados. Agrupar intervalos contiguos, sin unir dos entradas separadas por una salida del polígono.

[ST_LineLocatePoint](https://postgis.net/docs/ST_LineLocatePoint.html) sirve para proyectar un punto sobre una línea simple, pero aplicado a toda una ruta con bucles puede asignar la pasada equivocada. Resolver a nivel de edge y conservar todas las ocurrencias. La distancia geodésica del edge determina progreso; la interpolación de tiempo es local al edge, no proporcional a la distancia total del viaje.

Tocar un borde cuenta como contacto geométrico; clasificar como límite/precisión incierta si no hay longitud de exposición. No inventar 30 minutos de exposición a partir de un punto de tangencia. Validar geometrías con huecos, geometrías multipartes y CRS incorrecto.

## Estaciones y desvíos

La estación se proyecta para obtener una posición candidata, pero su acceso se valida por red. Para un par de anclas anteriores/posteriores `a,b`, calcular `dist(a,estación)+dist(estación,b)-dist_ruta(a,b)`, con restricciones de sentido. Los costos deben usar el mismo motor/perfil y geometría compatible. Rechazar saltos a la otra orilla, caminos privados y retornos imposibles.

Seleccionar candidatos por ventanas de autonomía; consultar costos en lote si el proveedor lo permite y validar las mejores geometrías. No pedir rutas para cada punto del catálogo. Si no puede verificarse el acceso dentro del presupuesto, mostrar candidato de estación con “desvío no verificado”, no una parada recomendada definitiva.

## Índices y rendimiento

- GiST en geometrías de alertas/eventos/red vial y geography de estaciones; el cast usado en consulta debe coincidir con el índice.
- B-tree en fuente/ID, producto, timestamps y versión de snapshot. GiST de rangos temporales si el volumen y plan de consulta lo justifican.
- Subdividir polígonos muy grandes conservando el ID original y deduplicar resultados.
- Persistir red/estaciones normalizadas; no descargar país completo por viaje.
- Calcular corredor por tramos, filtrar por bbox/índice y luego ejecutar predicado exacto.
- `EXPLAIN (ANALYZE, BUFFERS)` en staging con fixtures ampliadas; comprobar índice real, no sólo existencia del índice.

Objetivo propio: consultas espaciales p95 <500 ms para el corpus de beta, medido en hardware declarado. No es una capacidad garantizada sin benchmark. No introducir particionado, tiles propios ni clustering de base de datos hasta medir necesidad.
