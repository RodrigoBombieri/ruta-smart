# Producto, alcance y experiencia

## MVP alcanzable

Beta inicial Android, automóvil particular, viajes dentro de Argentina y corredores cuya cobertura de fuentes haya sido validada. Flutter permite extender a iOS, pero no elimina pruebas/distribución específicas. Definir corredores después de PoC; no prometer todo el territorio con el mismo nivel de información.

Los 15 objetivos del MVP se conservan con condiciones explícitas:

| Objetivo | Entrega / aceptación |
|---|---|
| Origen y destino | Buscar localidad, mostrar provincia y confirmar punto |
| Ruta, distancia y duración | Geometría completa, proveedor y fecha; duración estimada |
| Identificar RN/RP | Referencias con jurisdicción, orden y cobertura conocida |
| Estado vial | Eventos oficiales habilitados; huecos visibles; no falsear “sin cortes” |
| Alertas intersectadas | Matching espacial/temporal con feed autorizado; bloqueante para prometer esta función |
| Estaciones próximas | Candidatas por corredor, accesibilidad y producto reportado |
| Precios disponibles | Fuente, fecha, unidad; precio ausente permitido |
| Combustible | L/100 km manual y litros estimados |
| Costo | Precio válido o manual; nulo si no existe; no incluye peajes |
| Vehículo guardado | Local sin cuenta; sincronizado opcional con identidad |
| Viajes guardados | Local y nube opcional, reanálisis al reabrir si vencido |
| Navegación externa | Waze/Google con advertencia de posible ruta diferente |

Sin acceso oficial sostenible para clima/vialidad, **no se declara completado el MVP original**. Se puede probar una PoC o beta acotada de ruta/combustible y acceso a fuentes, con decisión explícita de alcance. Un enlace a una web no satisface “detectar alertas que intersecten”.

## Priorización

- **MUST:** normalización/punto confirmado; routing completo; timeline base; fuentes y cobertura; matching de alertas y eventos habilitados; consumo/costo disponible; autonomía con reserva; estaciones candidatas; guardado; mapa simple; navegación externa; manejo de fallos, privacidad y tests críticos.
- **SHOULD:** plan de carga multietapa validado y explicado; pronóstico horario externo complementario; pausas editables; filtros/clustering; accesibilidad avanzada; dashboard de calidad detallado.
- **COULD:** comparar horarios de salida; alternativas de ruta soportadas; exportación de informe; notificaciones opt-in tras MVP.
- **NOT NOW:** navegación propia, tráfico propio, reportes comunitarios, accidentes de usuarios, red social, reservas/hoteles/restaurantes, camiones, eléctricos/GNC, hidrología automática, offline cartográfico completo, Kubernetes, microservicios, CQRS/Event Sourcing e IA.

El MVP muestra autonomía y candidatos; la recomendación “conviene cargar en” sólo aparece si se implementa la validación de factibilidad y acceso. No simular la versión SHOULD con un ranking por distancia recta.

## UX

Home: “¿A dónde viajás?”, origen/destino, fecha/hora local, vehículo y opción continuar sin cuenta. Inputs extra de combustible inicial/reserva aparecen al solicitar autonomía. Ubicación del dispositivo es opcional; no bloquear búsqueda manual si se deniega permiso.

Resultado: estado y cobertura juntos, distancia, conducción estimada, pausas, litros y costo con supuestos. Después tarjetas de estado vial, clima/alertas, combustible, mapa y, sólo en futuro habilitado, hidrología. Mostrar primero el evento impeditivo conocido y las fuentes críticas faltantes.

Cada tarjeta relevante indica tramo, ventana de paso, motivo, fuente enlazada, emisión/última actualización y limitaciones. Formulación: “Según la duración prevista, pasarías por esta zona entre…”. Nunca “a las 14:20 habrá una tormenta” como certeza. El estado no se comunica sólo por color: texto, iconos y lector de pantalla.

Mapa: línea, eventos aplicables, polígonos de alerta relevantes y estaciones sugeridas. Clustering para puntos a bajo zoom; cortes críticos permanecen detectables en listado. Filtros por capa; agrupar episodios; evitar cientos de marcadores iniciales. Seleccionar tarjeta enfoca tramo y viceversa. Listado accesible ofrece toda la información aunque falle el mapa.

Estados de pantalla: validación, cargando/cancelable, completo, parcial, ruta inexistente, fuera de alcance, proveedor caído, informe vencido y sin conexión. Snapshot offline de informe permitido por licencias con sello “consultado el…”; no equivale a análisis actualizado ni incluye mapas offline por defecto.

Botones de navegación llevan destino y preferencias admitidas, con aclaración de divergencia. Si el usuario está lejos del origen, el navegador puede comenzar desde su posición; el informe sigue ligado a la ruta analizada.

## Flutter

Organizar por features `trip_search`, `assessment`, `vehicles`, `saved_trips`, `map`, `settings`. Repositorios HTTP, modelos generados/validados contra OpenAPI y una elección única de manejo de estado durante PoC; evitar mezclar BLoC/Riverpod/Providers sin necesidad. Cache local mínima cifrada o protegida por plataforma, tokens en secure storage, nunca claves secretas de proveedores en app.

Widget del mapa encapsulado, sin tipos de SDK en todo el dominio. El backend resuelve GIS y assessment para tener una única interpretación. Cliente maneja presentación, inputs, guardado local y lanzamiento de URLs con allowlist. Validar es-AR, coma decimal, fechas, zona horaria y montos con moneda, sin redondear valores antes de calcular.

## Competencia: evidencia y diferenciación a validar

| Producto | Función acreditada por su publicación | Relación con RutaSmart |
|---|---|---|
| [Google Maps](https://www.google.com/maps/about/) | Mapas, búsqueda, rutas y navegación | Destino de navegación; no competir en guía/POIs |
| [Waze](https://www.waze.com/waze) | Navegación y reportes de tráfico | Complemento para conducción en tiempo real |
| [Ruta0](https://www.ruta0.com/ruta/) | Planificación de rutas, distancias, alternativas, GNC y peajes | Competidor cercano en preparación; diferenciar mediante evidencia oficial y tiempo de paso |
| [Rutas.ar](https://rutas.ar/acerca.html) | Declara usar información de Vialidad en su mapa | Referencia de estado vial; su declaración no prueba API oficial abierta |
| [Río Negro](https://rionegro.gov.ar/servicio/330/estado-de-rutas), [Chubut](https://www.avp.chubut.gov.ar/estado-de-rutas/) | Publicaciones provinciales de rutas | Fuentes locales/competidores informativos; no se verificó app/API uniforme |
| [App YPF](https://app.ypf.com/) y [YPF Ruta](https://ruta.ypf.com/) | Pagos/beneficios y gestión de combustible para flotas | No afirmar comparador neutral de todas las banderas |
| [SMN Tiempo y Pronóstico](https://www.argentina.gob.ar/aplicaciones/smn-tiempo-y-pronostico) | Tiempo, pronósticos y alertas | Emisor oficial; RutaSmart aporta correlación de trayecto/hora |
| [Meteored](https://www.meteored.com.ar/sobre-nosotros/productos) | Pronóstico y productos meteorológicos | Información general; no copiar su presentación o datos sin permiso |

Las apps de precios por bandera, los comparadores y los datos comunitarios deben evaluarse por cobertura, fuente y edad, no por cantidad de marcadores. No se verificó aquí un proveedor de precios multibandera con API comercial utilizable. Esta tabla no demuestra que ningún competidor haga análisis temporal: falta prueba funcional comparativa y entrevistas. Evitar publicidad “única app” sin esa evidencia.
