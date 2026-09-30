# Registro de verificación — 2026-09-29

Esta investigación combinó documentación primaria, lectura de catálogos y una consulta HTTP real. No se ejecutaron benchmarks de routing, no se usaron API keys y no se adquirieron planes. Las fechas de rastreo de un buscador no son fechas de actualización del dato.

| Objeto | Método / resultado observado | Lo que no permite afirmar |
|---|---|---|
| GeoRef localidades | PowerShell Invoke-WebRequest, HTTPS, 200 application/json; consulta abajo | SLA, disponibilidad permanente, precisión domiciliaria |
| Vialidad estado | Lectura de página oficial; interfaz de búsqueda | API estable o licencia para extraer HTML |
| SIG-Vial | Página oficial identifica visor | WFS de eventos habilitado para terceros |
| Rutas Nacionales | Catálogo y enlace WFS publicados; intento de descarga 502 en herramienta web | Que el WFS esté caído globalmente o que 300 filas sean toda la red |
| Energía precios | Ficha oficial indexada; apertura posterior con timeout | Vigencia de precios ni licencia de reutilización |
| Energía estaciones | Ficha indexada CSV/SHP; apertura posterior con timeout | Cobertura ni coordenadas efectivas auditadas |
| Resolución 717/2025 | Texto oficial leído | Existencia de nuevo feed equivalente |
| SMN SAT/datos | Páginas indexadas y documentación institucional; lecturas 403/502 en intentos | Inexistencia de API o prohibición absoluta de consumo |
| INA | Asistente Web API legible con recursos y parámetros | Serie actual funcionando, SLA o autorización comercial |
| ORS | Documentación y restricciones legibles; planes/TOS redirigen a HeiGIT dinámico sin texto | Cuota gratuita vigente ni habilitación comercial |
| OSRM | Especificación oficial v5.24.0 leída | Versión a desplegar actual o demo con SLA |
| GraphHopper | Precios y documentación del proveedor | Calidad argentina medida; se leyó nota de free no comercial |
| Valhalla | Repositorio oficial confirma motor/matrices; antigua URL de docs redirige y una ruta GitHub 404 | Contrato de hosting público gratuito |
| Google | Tarifario y políticas de Routes leídos | Que Directions legacy convenga para un desarrollo nuevo |
| HERE | Restricciones publicadas legibles; apertura tarifario 403 | Tarifa monetaria aplicable o elegibilidad de RutaSmart |
| Mapbox | Documentación Directions/Matrix y pricing legibles | Licencia final para cualquier renderer o almacenamiento |

## Consulta técnica confirmada

Solicitud: [GeoRef Urdinarrain](https://apis.datos.gob.ar/georef/api/localidades?nombre=Urdinarrain&max=1).

Respuesta resumida, sin modificar coordenadas:

```json
{
  "cantidad": 1,
  "total": 1,
  "localidad": {
    "id": "30056110",
    "nombre": "Urdinarrain",
    "centroide": {"lat": -32.6910460992997, "lon": -58.8884200764691},
    "provincia": "Entre Ríos",
    "departamento": "Gualeguaychú"
  }
}
```

Es un resumen de campos, no el body exacto. El intento dentro del sandbox falló por TLS; tras ejecutar con autorización fuera de ese entorno respondió 200. No se desactivó validación de certificados.

## Enlace geográfico observado

El recurso GeoJSON publicado por Transporte enlazó a:

```text
https://ide.transporte.gob.ar/geoserver/idera/ows?maxFeatures=300&outputFormat=application%2Fjson&request=GetFeature&service=WFS&typeName=idera%3ARutas_Nacionales&version=1.0.0
```

No es un endpoint inventado. El intento devolvió 502 en la herramienta de lectura. Falta revisar capacidades/paginación y licencia antes de consumirlo. No se eligió un endpoint de Energía o SMN inferido a partir de patrones.

## Acciones de investigación pendientes

- SMN y Vialidad: preparar consultas sobre acceso, esquema, revisión/cancelación, frecuencia y derechos; el envío requiere una instrucción explícita del responsable del proyecto.
- Energía: obtener archivo vigente por enlace oficial, inspeccionar filas y aclarar derechos. No concluir que todos los registros quedaron congelados en 2025.
- Routing: cuenta propia, condiciones guardadas, set de rutas argentinas y cálculo de costos facturables.
- INA: validar una serie con estación, variable, unidad, datum, periodo y acceso permitido.
- Antes de beta: repetir revisión de licencias y precios. Las condiciones pueden cambiar después de esta fecha.
