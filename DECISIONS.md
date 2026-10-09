# Decisiones

Registro consolidado al 9 de octubre de 2026. Reemplaza los documentos de diseño y ADR repetidos de la etapa inicial.

| Decisión | Motivo y alcance |
|---|---|
| Monolito .NET 10 + Flutter | Una API y sus trabajos de ingesta; sin microservicios ni Redis obligatorio |
| PostgreSQL/PostGIS + Npgsql | Persistencia de informes y geometría; snapshots JSONB; SQL explícito |
| GIS con NetTopologySuite | Intersección por arista para preservar recorridos repetidos y huecos |
| Hora argentina explícita | Instantes con offset y zona IANA; presentación UTC−3 |
| Fuentes públicas sin claves | GeoRef, SMN CAP y Energía se consultan por sus canales públicos |
| OSRM público solo para pruebas | Uso local no comercial, corredor piloto y máximo una solicitud cada 1,1 segundos |
| ORS configurable | Clave solo en servidor, polígono de cobertura y compatibilidad de persistencia explícitos |
| flutter_map | Mapa inicial con tiles OSM en web; proveedor configurable en Android |
| Sin cuenta obligatoria | Token temporal, guardado local voluntario y sin seguimiento continuo |
| Combustible nafta/diésel | Litros y ARS; GNC, eléctricos y perfiles de camión no incluidos |
| Cobertura por dimensión | Un dato adverso prevalece; datos críticos faltantes impiden declarar favorable |
| Una instancia de API | Semáforos y cuotas locales; escalar exige coordinación compartida |
| Compose con nombre rutasmart | Contenedores y volúmenes aislados de otros proyectos |

El usuario autorizó la implementación y luego pidió simplificar la documentación y mejorar el diseño. Las fuentes abiertas no requieren una autorización individual adicional por el solo hecho de integrarlas; se respetan sus condiciones publicadas.
