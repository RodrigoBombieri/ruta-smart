# Glosario

| Término | Definición en RutaSmart |
|---|---|
| Travel Intelligence Layer | Capa de análisis que contextualiza un viaje; no guía de navegación |
| Route | Itinerario geométrico ordenado calculado por proveedor |
| Road | Carretera identificada por jurisdicción y código |
| RoadSegment | Tramo de la red vial de referencia |
| RouteSegment | Parte ordenada del itinerario particular |
| Corridor | Área de búsqueda alrededor de ruta; no prueba de impacto |
| Polyline | Secuencia de vértices que representa el recorrido |
| ETA | Hora estimada de paso/llegada, sujeta a supuestos |
| Timeline | Acumulación temporal de segmentos y pausas |
| Exposure interval | Parte del recorrido y tiempo estimado dentro de un evento |
| Forecast | Predicción para un intervalo futuro, no observación |
| Alerta | Producto de advertencia del emisor; conservar tipo/nivel original |
| ACP | Aviso a muy corto plazo; no confundir con pronóstico general |
| Coverage | Alcance efectivo espacial/temporal de información disponible |
| Stale | Dato o consulta que excede política de vigencia aplicable |
| Official | Procedencia institucional; no garantía automática de actualidad |
| External | Fuente privada/comunitaria ajena al organismo oficial |
| Calculated | Resultado de regla o algoritmo reproducible |
| Estimated | Resultado con incertidumbre o supuestos |
| Snapshot | Versión coherente e inmutable de datos/análisis |
| SourceUpdatedAt | Momento de actualización comunicado por la fuente |
| FetchedAt | Momento de descarga; no rejuvenece el dato |
| ValidFrom/To | Intervalo de aplicación del dato |
| GiST | Tipo de índice usado para búsquedas espaciales/rangos |
| Geometry / Geography | Tipos PostGIS; plano con CRS / geodésico terrestre |
| SRID | Identificador del sistema de referencia espacial |
| CRS | Sistema de referencia de coordenadas |
| Snapping | Ajuste de punto al grafo vial; puede desplazar el lugar seleccionado |
| Map matching | Correspondencia entre geometría/observación y red vial |
| Linear referencing | Ubicación por medida sobre una vía calibrada |
| Reserva | Combustible que el plan intenta no consumir |
| Autonomía útil | Distancia estimada con combustible actual menos reserva |
| Precio reportado | Referencia declarada con fecha; no garantía en caja |
| Producto reportado | Registro de combustible ofrecido; no garantía de stock |
| Idempotencia | Reintentar misma operación no duplica su efecto |
| Circuit breaker | Pausa temporal de llamadas a dependencia fallando |
| D / T / L | Documentación / respuesta técnica / derechos de uso, verificaciones separadas |
| ADR | Registro de contexto, decisión, alternativas y consecuencias |
| RPO / RTO | Pérdida máxima de datos / tiempo objetivo de recuperación |
| MAU | Usuarios activos mensuales, posible unidad de facturación |
| Matriz | Distancias/tiempos entre pares; no geometría de rutas |
