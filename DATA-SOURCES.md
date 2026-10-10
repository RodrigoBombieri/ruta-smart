# Fuentes y cobertura

| Fuente | Uso real | Límite principal |
|---|---|---|
| [GeoRef](https://www.argentina.gob.ar/datos-abiertos/georef) | Localidades y centroides | El usuario confirma el punto |
| [OSRM](https://project-osrm.org/docs/v5.24.0/api/) | Ruta y duración | Servidor público de prueba; sin SLA |
| [ORS](https://openrouteservice.org/dev/) | Adaptador alternativo configurable | Requiere clave y configuración; sin prueba con cuenta real |
| [SMN CAP](https://ssl.smn.gob.ar/CAP/AR.php) | Avisos, polígonos y vigencia | Inventario parcial; no es pronóstico para cualquier fecha |
| [Energía](https://datos.gob.ar/el/dataset/energia-precios-surtidor---resolucion-3142016) | Estaciones y precios declarados | Fecha por registro; no garantiza operación ni stock |
| [Vialidad Nacional](https://www.argentina.gob.ar/transporte/vialidad-nacional/estado-de-rutas) | Partes de rutas nacionales | Coincidencia por ruta/provincia, sin corte exacto ni sentido |
| [OpenStreetMap](https://www.openstreetmap.org/copyright) | Tiles del mapa web | Atribución visible y sin descarga masiva/offline |

## Condiciones de uso implementadas

OSRM público solo en Development, para pruebas no comerciales en Argentina; máximo una solicitud cada 1,1 segundos e identificación `RutaSmart/0.1`. [Política del servidor](https://github.com/Project-OSRM/osrm-backend/wiki/Demo-server).

Los tiles OSM siguen la [política publicada](https://operations.osmfoundation.org/policies/tiles/): vista interactiva, caché del navegador y atribución. Android admite URL/atribución configurables; no trae mapa base por defecto.

SMN se consulta desde el índice CAP registrado en la [OMM](https://alertingauthority.wmo.int/authorities.php?recId=4). Se aceptan mensajes públicos reales del emisor, sin autenticación ni reutilización de claves ajenas.

El CSV de Energía redirige de HTTPS a HTTP. Desarrollo permite ese canal público sin datos personales; producción lo desactiva por defecto y permite importación local. La descarga no renueva la fecha de los precios. No se infiere una licencia general de redistribución del mero acceso público.

## Vialidad y cartografía nacional

Vialidad publica una planilla vinculada desde su página oficial. Se consulta su [CSV público](https://docs.google.com/spreadsheets/d/17AqjqeNvM4nG6cOUsUFKFaKXMiNmztYfzHIxeM9FcXk/gviz/tq?tqx=out:csv&sheet=tablavisible) cada hora, sin credenciales. Se conserva la fecha de cada parte; fechas ausentes, futuras o de más de 48 horas se marcan inciertas. Una coincidencia genera precaución, sin afirmar que el corte intercepte exactamente el recorrido.

Los [polígonos GeoRef](https://datosgobar.github.io/georef-ar-api/shapefiles/) usan [CC BY 4.0](https://datosgobar.github.io/georef-ar-api/terms/). `scripts/prepare-boundaries.py` documenta su simplificación; el archivo conserva fuente y hash. La tolerancia de planificación es aproximadamente 500 m, sin certificar límites legales.

El plan usa matrices OSRM y luego verifica la ruta completa con las paradas. No sustituye tramos desconectados por distancia aérea. El catálogo no acredita entrada al surtidor, horario ni stock.

[Evidencia de consultas](docs/research/VERIFICATION.md). Las condiciones de proveedores deben revisarse antes de distribución comercial.
