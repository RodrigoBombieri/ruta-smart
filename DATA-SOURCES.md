# Investigación de fuentes y contratos de datos

Fecha de revisión: **2026-09-29**. Ver [registro de pruebas](docs/research/VERIFICATION.md). La revisión web no constituye una prueba de disponibilidad sostenida. Una página accesible no acredita API pública; una API funcional no acredita licencia comercial.

## Resultado ejecutivo

| Fuente | Acceso confirmado documentalmente | Prueba técnica | Uso recomendado |
|---|---|---|---|
| GeoRef | REST y OpenAPI oficiales | GET real 200/JSON para Urdinarrain | Normalización y selección de localidad |
| Vialidad: estado | Publicación web oficial | Sin contrato de integración validado | Bloqueante para automatizar estado vial |
| Transporte/DNV: red nacional | Catálogo SHP, GeoJSON y KML | Descarga GeoJSON devolvió 502 en herramienta web | PoC de cartografía; no estado operativo |
| Energía: estaciones | Recurso CSV/SHP catalogado | Archivo completo no inspeccionado | Candidato a inventario sujeto a calidad/licencia |
| Energía: precios 314 | Catálogo de CSV, metadatos históricos | Vigencia de registros no comprobada | No usar como precio actual por defecto |
| SMN | SAT, advertencias, avisos y datos publicados | API pública de integración no validada | Prioridad de convenio/acceso documentado |
| INA | Asistente oficial de Web API | No se verificó una serie de observaciones | Fuera del MVP; PoC posterior |

No hay fuente aprobada todavía para afirmar cobertura completa de cortes o alertas a escala nacional. El MVP completo depende de cerrar estos accesos. Una beta reducida debe anunciar expresamente las prestaciones y territorios que no cubre.

## 1. Vialidad Nacional y red vial

[Estado de rutas](https://www.argentina.gob.ar/transporte/vialidad-nacional/estado-de-rutas) es una publicación oficial, no una especificación REST. [SIG-Vial](https://www.argentina.gob.ar/transporte/vialidad-nacional/sig-vial) acredita un visor GIS; no se confirmó un servicio de eventos versionado, autenticación, frecuencia garantizada ni SLA. La información web sirve para consulta humana y para solicitar un canal oficial de integración. No se extrajeron endpoints internos ni se automatizó scraping.

El [catálogo Rutas Nacionales](https://datos.transporte.gob.ar/dataset/rutas-nacionales) publica recursos SHP/GeoJSON/KML, frecuencia eventual y licencia “Otra (Abierta)”. Su metadato de actualización es 2023-04-12: no prueba antigüedad de cada tramo. El enlace GeoJSON publicado apunta a WFS con `maxFeatures=300`: **no asumir descarga completa**. Antes de ingerir: capacidades, CRS, conteo total, paginado, identificadores, extensión nacional y permiso concreto de reutilización.

Datos estáticos: denominación, jurisdicción y ejes de rutas cuando el recurso los contenga. Datos operativos: cortes, obras, restricciones, superficie y transitabilidad requieren otra publicación. No deducir que una vía cartografiada está habilitada.

Cobertura provincial: investigar por jurisdicción. [Río Negro](https://rionegro.gov.ar/servicio/330/estado-de-rutas) y [Chubut](https://www.avp.chubut.gov.ar/estado-de-rutas/) tienen publicaciones de estado; no se comprobó API ni permiso de ingesta. Incorporarlas una por una, con su alcance. No generalizar su cobertura a toda Argentina.

**Puerta vial:** obtener formato de intercambio autorizado y ejemplos de cierre, modificación, reapertura y evento sin fecha final. Si sólo hay PDF/HTML, evaluar carga editorial acotada con enlace y revisión humana, únicamente bajo permiso de reutilización. Para un desarrollador individual no es razonable operar una redacción nacional permanente. Sin canal sostenible: excluir esa cobertura, mostrar información insuficiente y no anunciar detección automática nacional.

## 2. Secretaría de Energía

La [Resolución 717/2025](https://www.argentina.gob.ar/normativa/nacional/resoluci%C3%B3n-717-2025-413477/texto), publicada el 2 de junio de 2025, derogó la 314/2016. Por tanto, su antiguo plazo obligatorio de reporte no fundamenta la frescura actual. La existencia del catálogo no acredita continuidad ni interrupción total del suministro: hay que medir las filas.

El [catálogo de precios 314/2016](https://www.datos.gob.ar/sk/dataset/energia-precios-surtidor---resolucion-3142016) ofrece CSV vigente e histórico de naftas, gasoil y GNC. Declara frecuencia horaria y no especifica licencia en esa ficha. Son metadatos publicados, no una garantía vigente. Se verificó la ficha indexada; aperturas directas posteriores agotaron el tiempo de consulta. No se midieron cantidades, porcentaje de coordenadas válidas ni fechas reales.

El [recurso estaciones de servicio](https://datos.gob.ar/dataset/energia_669615be-17f4-41ad-9aad-4c438cb974f9/archivo/energia_b7c42965-f140-4430-892d-0a065a6aeb35) describe CSV y SHP EPSG:4326, actualización eventual y un último cambio de 2017. [Información Geográfica de Energía](https://www.argentina.gob.ar/economia/energia/planeamiento-energetico/sistema-unificado-de-informacion-energetica-1) también enlaza mapas de estaciones. Ninguno demuestra operación comercial actual de cada boca.

Esquema canónico requerido, **no supuesto esquema del CSV**: `sourceStationId`, razón social opcional, bandera, dirección, provincia/localidad, punto lon/lat, producto/grado, unidad, moneda, precio, fecha declarada. El adaptador debe documentar el mapeo de columnas reales; una fila estación-producto-precio no equivale a una estación nueva.

Auditoría PoC: conteos por provincia/producto; duplicados; coordenadas nulas/0,0/intercambiadas/fuera de territorio; valores no positivos; fechas futuras; antigüedad p50/p95; producto sin estación; bandera renombrada; estaciones sin actualización. No corregir coordenadas silenciosamente. Poner en cuarentena y registrar transformación. Publicar métricas antes de decidir cobertura.

Nafta y diésel en L y ARS/L en MVP; normalizar grados sin confundir nombres comerciales. GNC usa m³: catalogarlo no habilita calcularlo con L/100 km. GNC/eléctricos y vehículos bifuel quedan fuera del cálculo inicial.

**Fallback de precio:** referencia ingresada por el usuario, con fecha y etiqueta; si no hay referencia válida, devolver litros y costo nulo. Un precio histórico no se actualiza por inflación supuesta. La [página de precios de hidrocarburos](https://www.argentina.gob.ar/economia/energia/hidrocarburos/precios-de-hidrocarburos) lista otras series; promedios mayoristas o mensuales no son precio en surtidor de una estación. Sólo podrían usarse como otra clase de referencia explícita tras inspección.

## 3. Servicio Meteorológico Nacional

El [SAT publicado](https://ws2.smn.gob.ar/alertas) distingue alertas, advertencias y avisos a muy corto plazo. La [descarga de datos](https://ws2.smn.gob.ar/descarga-de-datos) acredita un canal de publicación, pero el acceso web tuvo errores 502/403 en intentos de lectura. Esto no prueba que el servicio esté caído para todos.

Una [nota técnica del SMN](https://repositorio.smn.gob.ar/bitstream/handle/20.500.12160/1838/Nota_Tecnica_SMN_2022-124.pdf?isAllowed=y&sequence=1) menciona distribución por API. La mención no aporta por sí sola un contrato público utilizable, credenciales, cuotas ni autorización para esta aplicación. No usar tokens del frontend ni wrappers comunitarios como si fueran servicios oficiales.

La página SAT declara CC Reconocimiento 2.5 Argentina para sus contenidos. Confirmar alcance sobre el feed y geometrías elegidos; licencia de contenido y permiso de acceso son distintos. No se verificaron frecuencia de actualización del feed, cobertura geométrica completa ni horizonte de un API operativo.

Solicitar: alertas/advertencias/ACP con ID, revisiones/cancelaciones, fenómeno, severidad, emisión, validez, polígono o códigos de zonas versionados, unidades y documentación. Para pronóstico: grilla o puntos, resolución espacial y temporal, ciclos, horizonte, precipitación, viento/ráfagas, temperatura y visibilidad cuando exista. Una observación de estación no sustituye un pronóstico futuro.

**Puerta meteorológica:** poder distinguir “lista vacía válida” de respuesta incompleta, expirada o fallida. Conseguir una muestra y permiso automatizado. No ofrecer detección oficial automática sin esto.

Complemento posible: [Open-Meteo](https://open-meteo.com/en/docs) documenta pronóstico horario y múltiples coordenadas. Es proveedor **externo**, no emisor de alertas SMN. Su [plan gratuito](https://open-meteo.com/en/pricing) es para uso no comercial y evaluación; producción comercial necesita plan apropiado. No fijar precio mensual que no pudo comprobarse. No reemplaza las alertas oficiales faltantes. Consultar pocas variables y agrupar por celdas/horas; requests HTTP y unidades facturables pueden diferir.

## 4. INA e hidrología

El [asistente oficial de Web API](https://alerta.ina.gob.ar/pub/gui/apibase) enumera estaciones, redes, variables, series, datos observados y pronosticados; su alcance declarado es el sistema de la Cuenca del Plata. [Estaciones](https://alerta.ina.gob.ar/pub/gui/estaciones) y [datos](https://alerta.ina.gob.ar/pub/gui/datos) permiten explorar parámetros. Se verificó documentación, no continuidad de observaciones, licencia comercial ni cuota. El [sistema A5](https://alerta.ina.gob.ar/a5/html/README.html) menciona usuario y contraseña; no confundirlo con acceso público irrestricto.

Valor concreto posterior: puente o acceso bajo ligado a una estación y umbral documentado; tramo intersectado por una delimitación de inundación válida; aviso oficial de anegamiento sobre un corredor. No basta proximidad a un río, precipitación intensa ni altura aislada: faltan topografía, datum, relación hidráulica y condiciones de infraestructura. Requiere validación especializada y evidencia local. Sin ese vínculo, mostrar sólo contexto hidrológico opcional, no cambiar estado vial.

AlertaRío será otro posible proveedor de `HydrologyImpact`, con procedencia original, versión, área, periodo y limitaciones. La ausencia de este módulo no degrada un MVP que explícitamente no ofrece cobertura hidrológica.

## 5. GeoRef

[Documentación oficial OpenAPI](https://www.argentina.gob.ar/datos-abiertos/georef/openapi) y [portal](https://www.argentina.gob.ar/georef). Recursos documentados: provincias, departamentos, municipios, localidades, direcciones y ubicación inversa, entre otros. Se ejecutó `GET https://apis.datos.gob.ar/georef/api/localidades?nombre=Urdinarrain&max=1`: 200 JSON, localidad `30056110`, Entre Ríos.

Usar para nombres y códigos administrativos. Su centroide no es la puerta de un domicilio ni garantiza estar sobre una vía transitable. El usuario debe confirmar el punto de inicio/destino. Revisar licencia de cada exportación; no se verificaron SLA ni cuota numérica. Cache de localidades e ingesta de base sólo conforme al permiso confirmado.

## 6. Política común de ingesta

Cada fuente tiene ficha con: responsable, URL pública, recurso exacto, tipo oficial/externo, licencia y evidencia de autorización, método, esquema/versionado, frecuencia declarada, frecuencia observada, alcance territorial/temporal, timestamps, límites, contacto, estado D/T/L y fecha de revisión.

No scraping en el diseño base. Una excepción exigiría permiso documentado, ausencia de alternativa, análisis de términos y sostenibilidad, límites de acceso y ADR específico. `robots.txt` por sí solo no otorga una licencia. No hubo contacto con organismos ni se enviaron solicitudes durante esta etapa.

Persistir emisión, validez, modificación de fuente, recepción y hash por separado. Descargar hoy no rejuvenece datos viejos. Conservar último lote válido durante un fallo; nunca reemplazarlo por vacío por error de parseo. Las bajas sólo se interpretan si el contrato indica snapshot completo o cancelación explícita.

## 7. Criterios para habilitar una fuente

1. D: documentación y esquema identificados, con enlace oficial.
2. T: respuesta real reproducible y casos vacíos/errores conocidos.
3. L: acceso automatizado, almacenamiento, redistribución, atribución y explotación previstos autorizados.
4. Calidad: muestreo territorial y temporal, unicidad, actualización y recuperación verificadas.
5. Operación: límites configurados, alertas de frescura y fallback probado.

Una fuente no pasa a producción por aprobar solamente uno de estos controles.
