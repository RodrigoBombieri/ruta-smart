# Verificación

Revisión local: **10 de octubre de 2026**. Los conteos describen esas consultas; no garantizan disponibilidad futura.

| Evidencia | Resultado |
|---|---|
| OSRM Buenos Aires–Rosario | HTTP 200; 302.143 m, 11.814,6 s y 1.623 puntos |
| GeoRef Urdinarrain | HTTP 200; localidad 30056110 |
| Índice CAP del SMN | Público, sin credenciales; 23 avisos activos, sin rechazos en la última consulta |
| CSV Energía | 36.649 filas; 4.209 estaciones normalizadas tras filtrar productos/registros |
| .NET + PostgreSQL | 57 tests aprobados, ninguno omitido; incluye geometría SRID 4326, propiedad, idempotencia, borrado y purga |
| Flutter | Analyze limpio; 5 tests; web release y Android debug compilados |
| Vialidad | 663 partes públicos normalizados; fechas preservadas |
| Routing nacional | 8 recorridos: Centro, Cuyo, NOA, NEA, Patagonia norte/sur, Tierra del Fuego y tránsito internacional |
| Carga Buenos Aires–Córdoba | 696,841 km; una parada; compra 27,27 L; llegada con 5,007 L de reserva; propuesta confirmada |
| Chrome | Análisis real, plan de carga, confirmación, guardado y capturas renovadas |

El servidor OSRM rechazó inicialmente el cliente sin User-Agent; identificar RutaSmart resolvió el problema. Se corrigió además una comparación de fechas que marcaba catálogos nuevos como antiguos, y la selección que concentraba estaciones cerca del origen.

La compilación Android exigió reparar el launcher Flutter local y usar los certificados públicos ya confiados por Windows/Java en un almacén local. No se desactivó TLS ni se incluyeron certificados privados en el repositorio.

## Referencias

- [Registro CAP del SMN en OMM](https://alertingauthority.wmo.int/authorities.php?recId=4) y [índice público](https://ssl.smn.gob.ar/CAP/AR.php).
- [Catálogo Energía](https://datos.gob.ar/el/dataset/energia-precios-surtidor---resolucion-3142016). La descarga observada redirige de HTTPS a HTTP; las fechas de precios se conservan.
- [API OSRM](https://project-osrm.org/docs/v5.24.0/api/) y [política del servidor demo](https://github.com/Project-OSRM/osrm-backend/wiki/Demo-server).
- [Política de tiles OSM](https://operations.osmfoundation.org/policies/tiles/).

Las pruebas nacionales son una muestra regional: no acreditan todas las carreteras, operación de estaciones ni condiciones seguras para viajar.
