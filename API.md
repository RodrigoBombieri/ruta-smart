# REST API propuesta

[OpenAPI 3.1](docs/openapi.json) es el contrato de diseño de RutaSmart, **no una API pública existente**. Los endpoints aquí definidos son propios y deliberadamente propuestos; no se atribuyen a organismos. No se despliega nada en esta etapa.

## Operaciones

| Método/ruta | Función | Autorización |
|---|---|---|
| POST /api/v1/sessions/anonymous | Sesión temporal y token opaco | Público, rate limited |
| GET /api/v1/locations/search | Búsqueda de localidades por nombre/provincia | Sesión o cuenta |
| POST /api/v1/trips/analyze | Crear viaje temporal y primer análisis | Sesión o cuenta |
| GET /api/v1/trips/{tripId} | Leer snapshot propio | Mismo principal propietario |
| GET /api/v1/trips | Listar viajes guardados con cursor | Cuenta |
| PATCH /api/v1/trips/{tripId} | Guardar/desguardar y cambiar nombre | Cuenta propietaria |
| DELETE /api/v1/trips/{tripId} | Borrar viaje propio | Sesión/cuenta propietaria |
| POST /api/v1/trips/{tripId}/analyses | Nueva versión con horario/paradas/input corregidos | Mismo propietario |
| GET/POST /api/v1/vehicles | Listar/crear vehículos | Cuenta |
| GET/PATCH/DELETE /api/v1/vehicles/{vehicleId} | Consultar/editar/borrar | Cuenta propietaria |
| GET /api/v1/sources/status | Estado público resumido y alcance | Público con cache |

Auth de cuenta: OIDC authorization code + PKCE con proveedor a elegir; no diseñar endpoints de contraseñas/token propios sin necesidad. Sesión anónima opaca distinta del JWT de identidad. El guardado anónimo es local en Flutter; temporal backend expira. Migración de datos anónimos a cuenta se agrega sólo con transferencia explícita y comprobación de ambos principals; no endpoint público de “reclamar por UUID”.

Un análisis devuelve conjuntamente ruta, timeline resumido, eventos, clima y combustible: evita rondas redundantes desde Flutter. `matchedEvents` lleva geometrías de evidencia para capas del mapa y `forecastSamples` las variables disponibles con unidades, intervalo, modelo y resolución. Arrays vacíos se interpretan junto a `coverage`, nunca como prueba aislada de ausencia de riesgos. No exponer endpoints que reciban polylines arbitrarias enormes como `/fuel-stations/route`. Si los detalles crecen, paginar por tripId/assessmentVersion, respetando autorización y el snapshot, sin volver a llamar a proveedores.

## Semántica

POST de análisis es síncrono con deadline. 201 crea un snapshot completo o parcial; 503 si no se obtiene ruta utilizable. No devolver 200/favorable al fallar fuentes. `availability` por dimensión acompaña el estado general. Errores Problem Details incluyen código estable y correlationId, nunca secretos ni payload del proveedor.

`Idempotency-Key` requerido para POST de análisis: alcance por propietario/ruta de operación, hash de body, TTL propuesto 24 h. Misma clave y mismo body devuelve resultado original; body distinto da 409. Una respuesta caducada sigue siendo original y debe reanalizarse con nueva clave. No usar cache de evaluación para fingir frescura.

Fechas ISO 8601 con offset obligatorio en entrada, zona IANA separada y coherente. Coordenadas como objeto `latitude/longitude` para evitar inversión; geometrías GeoJSON `[lon,lat]`. Metros, segundos, litros y ARS explícitos. Precio `null` no es cero. Enum de procedencia: official, external, user_input; `calculated` y `estimated` como flags separados del origen.

Validaciones adicionales al esquema: lat/lon dentro del territorio soportado; ruta íntegra soportada; salida futura dentro del horizonte de planificación propuesto de 30 días (clima puede cubrir menos); origen=destino ⇒ 422 `same_origin_destination`; ninguna pausa más allá de la ruta; acumulados monótonos; combustible inicial <= tanque; reserva < tanque. Solicitud lleva snapshot de vehículo para análisis, aunque provenga de un vehículo guardado. Esto hace reproducible el resultado.

429 incluye Retry-After. 401 token inválido; 403 operación no permitida; 404 recurso ajeno/inexistente para evitar enumeración; 409 conflicto de idempotencia; 422 input/ruta no soportados; 503 dependencia esencial indisponible. Concurrency control en edición guardada mediante ETag/If-Match en implementación; esquema extensible antes de congelar v1.

## Ejemplo conceptual de resultado

“PRECAUCIÓN — cobertura vial parcial” puede incluir un aviso meteorológico que coincide con el paso estimado y costo nulo por precio faltante. `reasons` contiene código, texto, intervalo, evidencia y confianza. Un `sourceRef` resuelve fuente, enlace, emisión y actualización. El color no reemplaza estos campos.

Un viaje guardado es una solicitud persistida más un informe fechado, no un monitor continuo. GET no dispara llamadas externas ocultas; el cliente presenta “Actualizar análisis” si venció. Las reevaluaciones producen nuevas versiones, no reescriben la historia usada para explicar un resultado.
