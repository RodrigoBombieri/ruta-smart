# Fuentes y cobertura

| Fuente | Uso real | Límite principal |
|---|---|---|
| [GeoRef](https://www.argentina.gob.ar/datos-abiertos/georef) | Localidades y centroides | El usuario confirma el punto |
| [OSRM](https://project-osrm.org/docs/v5.24.0/api/) | Ruta y duración | Servidor público de prueba; sin SLA |
| [ORS](https://openrouteservice.org/dev/) | Adaptador alternativo configurable | Requiere clave y configuración; sin prueba con cuenta real |
| [SMN CAP](https://ssl.smn.gob.ar/CAP/AR.php) | Avisos, polígonos y vigencia | Inventario parcial; no es pronóstico para cualquier fecha |
| [Energía](https://datos.gob.ar/el/dataset/energia-precios-surtidor---resolucion-3142016) | Estaciones y precios declarados | Fecha por registro; no garantiza operación ni stock |
| [OpenStreetMap](https://www.openstreetmap.org/copyright) | Tiles del mapa web | Atribución visible y sin descarga masiva/offline |

## Condiciones de uso implementadas

OSRM público solo en Development, para pruebas no comerciales Buenos Aires–Rosario y alrededores; máximo una solicitud cada 1,1 segundos e identificación `RutaSmart/0.1`. [Política del servidor](https://github.com/Project-OSRM/osrm-backend/wiki/Demo-server).

Los tiles OSM siguen la [política publicada](https://operations.osmfoundation.org/policies/tiles/): vista interactiva, caché del navegador y atribución. Android admite URL/atribución configurables; no trae mapa base por defecto.

SMN se consulta desde el índice CAP registrado en la [OMM](https://alertingauthority.wmo.int/authorities.php?recId=4). Se aceptan mensajes públicos reales del emisor, sin autenticación ni reutilización de claves ajenas.

El CSV de Energía redirige de HTTPS a HTTP. Desarrollo permite ese canal público sin datos personales; producción lo desactiva por defecto y permite importación local. La descarga no renueva la fecha de los precios. No se infiere una licencia general de redistribución del mero acceso público.

## Qué falta

[Estado de rutas de Vialidad](https://www.argentina.gob.ar/transporte/vialidad-nacional/estado-de-rutas) es una referencia pública, pero todavía no está integrado. No se afirma ausencia de cortes. Tampoco hay forecast complementario ni cobertura nacional de routing validada.

[Evidencia de consultas](docs/research/VERIFICATION.md). Las condiciones de proveedores deben revisarse antes de distribución comercial.
