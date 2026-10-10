# API

Base: `/api/v1`. JSON en camelCase. [Contrato OpenAPI](docs/openapi.json). Los modelos ejecutables están en `src/RutaSmart.Core/Models.cs`.

Regenerar el contrato: `python scripts/export-openapi.py`. CI verifica que sus esquemas y rutas coincidan con el código.

| Método | Ruta | Resultado |
|---|---|---|
| POST | `/sessions/anonymous` | 201: token y vencimiento a 24 horas |
| GET | `/locations/search?q=Rosario` | Localidades GeoRef |
| POST | `/trips/analyze` | 201: nuevo informe |
| GET | `/trips/{id}` | Última versión propia |
| POST | `/trips/{id}/analyses` | 201: nueva versión |
| POST | `/trips/{id}/fuel-plan` | 201: propuesta o confirmación, nueva versión |
| DELETE | `/trips/{id}` | 204: borrado propio |
| GET | `/sources/status` | Estado y fechas de las fuentes |
| GET | `/trips` | 403: cuentas no habilitadas |

`GET /health/live` informa si el proceso responde. No certifica la salud de las fuentes.

## Autorización e idempotencia

Excepto sesión, fuentes y salud, usar `Authorization: Bearer <token>`. Nunca guardar el token en Git ni logs. Un UUID no permite acceder a recursos de otro propietario.

Los POST de análisis y plan de carga requieren `Idempotency-Key` de 8–100 caracteres alfanuméricos, guion o guion bajo. Reenviar la misma clave y el mismo cuerpo devuelve el informe existente; cambiar el cuerpo da 409. El hash usa los bytes JSON: conservar también el formato al reintentar.

## Entrada de análisis

```json
{
  "origin": {"label": "Buenos Aires", "point": {"latitude": -34.6037, "longitude": -58.3816}},
  "destination": {"label": "Rosario", "point": {"latitude": -32.9468, "longitude": -60.6393}},
  "departureAt": "2026-10-10T08:00:00-03:00",
  "timeZone": "America/Argentina/Buenos_Aires",
  "vehicle": {"name": "Mi auto", "fuelType": "nafta_2", "tankLitres": 47, "consumptionLPer100Km": 7.5},
  "initialLitres": 30,
  "reserveLitres": 5,
  "manualPriceArsPerLitre": null,
  "pauses": [],
  "demo": false
}
```

Actualizar la fecha del ejemplo: debe estar dentro de los próximos 30 días. Combustibles: `nafta_2`, `nafta_3`, `diesel_2`, `diesel_3`. Cada pausa tiene `distanceM` y `durationSeconds`; máximo 20. Los extremos deben confirmarse y separarse al menos 100 metros.

## Plan de carga

Enviar `{"expectedVersion":1,"accept":false}` para calcular. Confirmar con la versión devuelta y `accept:true`. La propuesta vence a los 10 minutos; confirmar recalcula ruta, horarios, alertas y partes viales. Una versión anterior da 409.

Hasta 20 candidatas, 8 cargas y 20 pausas totales. Cada carga agrega 15 minutos. Se prioriza menor cantidad de paradas y luego distancia; no optimiza precios. El desvío máximo admitido es 30% de la ruta más 30 km. Se excluyen estaciones a más de 150 m del acceso cartográfico. No encontrar un plan no significa que no existan otras estaciones.

## Errores y límites

Problem Details con `title`, `status`, `code` y, cuando corresponde, `correlationId`. Lecturas/borrados desconocidos devuelven 404. Códigos habituales: 400 formato, 401 sesión, 409 idempotencia, 422 datos, 429 cuota y 503 dependencia.

Por instancia: 5 análisis/hora/sesión, 10 sesiones/hora/IP y 240 solicitudes/minuto. Cuerpo máximo 64 KiB; análisis hasta 15 segundos y plan hasta 35 segundos. Calcular y confirmar consumen la misma cuota de análisis. Los reintentos también consumen cuota. No hay endpoints de cuentas ni vehículos sincronizados.
