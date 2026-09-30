# Estrategia de testing

Ningún test automatizado de aplicación se ejecutó: la aplicación aún no existe. Se entregan contratos, fixtures sintéticas y criterios. CI futura no dependerá de servicios externos. Pruebas de contrato reales se ejecutan aparte, con credenciales/permiso y cuota, y no bloquean determinismo de unit tests.

## Capas

- Unit .NET: unidades/moneda, reserva, costos vs compras, timeline, solapamiento semiabierto, precedencia de estados, deduplicación y frescura con reloj inyectado.
- Integración: PostgreSQL/PostGIS real en contenedor, migraciones, índices/consultas, autorización y snapshots atómicos. No sustituir GIS por EF InMemory.
- Adaptadores: respuestas grabadas autorizadas y sanitizadas; schema drift, 429, 401, timeout, 200 vacío válido y 200 inválido. Fixtures sintéticas hasta obtener permiso.
- API: validación OpenAPI, Problem Details, idempotencia, límites y acceso entre propietarios.
- Flutter unit/widget: parsing de parcial/nulo, textos de cobertura, formateo es-AR, estado cancelable, accesibilidad y comportamiento sin GPS.
- Flutter integración: buscar→analizar→guardar→reabrir→reanálisis→navegación externa con mocks; prueba manual complementaria en dispositivos reales.
- Rendimiento/operación: carga sintética, tope de 12 requests/64 pares, interrupción de worker, restore, expiración y presupuesto.

## Casos críticos Given/When/Then

| ID | Given | When | Then |
|---|---|---|---|
| T01 | Ruta corta y fuentes críticas completas sin eventos | Analizar | Favorables con fuente/fecha y frase no garantista |
| T02 | Ruta larga en al menos dos provincias | Segmentar | Orden, metros y ETA monótonos; sin reset al cruzar provincia |
| T03 | Polígono cercano sin tocar línea | Correlacionar | No aviso “afecta recorrido”; contexto cercano opcional separado |
| T04 | Polígono cruza línea y validez cubre paso | Analizar | Motivo con tramo y ventana exacta estimada |
| T05 | Alerta vence antes de llegada más temprana | Analizar | No afectación activa para ese paso |
| T06 | Estación barata con desvío más costoso | Rankear | No gana sólo por precio; componentes explicados |
| T07 | No existe siguiente estación alcanzable con reserva | Planificar | No plan completo; distinguir dato insuficiente de imposibilidad |
| T08 | API meteorológica caída y snapshot vencido | Analizar | Información insuficiente o aviso conocido + cobertura parcial; nunca favorable |
| T09 | Corte vial viejo sin reapertura | Analizar | Vigencia no confirmada, no asumir abierto |
| T10 | Origen=destino | Validar | 422 antes de gasto externo |
| T11 | Proveedor responde NoRoute | Analizar | Error de ruta inexistente; no línea recta como ruta |
| T12 | Precio/coordenada/validez incompletos | Normalizar | Null/cuarentena o cobertura parcial, no cero por defecto |
| T13 | Tanque 47, inicio 30, reserva 5, consumo 7,5, distancia 1.200 | Calcular | 90 L, 333,33 km iniciales, compras mínimas 65 L antes de desvíos |
| T14 | Ruta se cruza a sí misma | Intersectar | Dos pasadas con distintas ETA, no una proyección global |
| T15 | Estación al otro lado del río/autopista | Evaluar acceso | No recomendar por proximidad euclidiana |
| T16 | Evento en vía paralela o puente sobre ruta | Match vial | No falso corte del recorrido |
| T17 | Alerta cancelada/revisada | Ingerir de nuevo | Revisión reemplaza efecto; auditoría conserva historial |
| T18 | Lote truncado a 300 features | Ingerir | No publicar como snapshot nacional completo |
| T19 | Misma respuesta vieja descargada hoy | Evaluar frescura | Fecha original no cambia |
| T20 | Viaje cruza medianoche y añade pausa | Calcular ETA | UTC/fecha correcta, alertas rematcheadas |
| T21 | Salida más allá del horizonte de forecast | Analizar | Parte no cubierta visible |
| T22 | Usuario A y tripId de B | GET/PATCH/DELETE | 404 y ninguna lectura/modificación |
| T23 | Misma idempotency key con otro body | POST | 409 sin nuevo gasto |
| T24 | Misma key/body concurrentes | POST simultáneo | Un análisis efectivo y respuesta consistente |
| T25 | Ruta sale a Chile aunque extremos argentinos | Validar geometría | Fuera de alcance explícito |
| T26 | GNC con consumo L/100 km | Validar | Rechazo de unidad/perfil no soportados |
| T27 | Ingesta 500 y catálogo previo válido | Publicar | Snapshot previo intacto, salud degradada |
| T28 | Límite de requests alcanzado | Buscar más estaciones | Respuesta parcial, no continuar ni falsear validación |
| T29 | Corte confirmado + SMN caído | Evaluar | Adversas y cobertura meteorológica insuficiente simultáneas |
| T30 | Polígono con hueco/tangencia/inválido | Intersectar | Respeta hueco; contacto sin duración ficticia; inválido en cuarentena |
| T31 | Precio faltante con consumo disponible | Analizar | Litros válidos, costo null |
| T32 | Notificación futura duplicada de mismo episodio | Reevaluar | Una sola notificación relevante opt-in |

## Datos mock

[Fixtures](docs/fixtures/scenarios.json) define una línea sintética en WGS84, polígonos de intersección/no intersección, periodos y aritmética de combustible. Es **geometría artificial**, no trayecto vial real ni alerta oficial. El [README de fixtures](docs/fixtures/README.md) especifica cómo ampliarla y la incertidumbre del reloj.

Casos reales de PoC se mantienen separados con licencia, hash, fuente y fecha; no descargarlos durante cada ejecución. Expandir corpus a diferentes latitudes, montañas, llanura, carreteras paralelas y ciudades. Evaluar falsos positivos y falsos negativos contra un oráculo GIS revisado manualmente, no contra el mismo algoritmo bajo test.

## Definición de terminado

Tests del dominio afectado + integración GIS/auth cuando corresponda; contrato actualizado; escenarios de fallos; observabilidad sin PII; evidencia de licencias; documentación y revisión de textos. Para beta: todos T01–T31 aprobados o exclusión documentada de función no anunciada; T32 posterior a notificaciones. No fijar porcentaje de cobertura de líneas como sustituto de estos comportamientos.
