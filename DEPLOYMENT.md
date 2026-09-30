# Despliegue, costos y observabilidad

Plan, no infraestructura creada. Desarrollo con Docker Compose: API/worker del monolito + PostgreSQL/PostGIS; herramientas de observabilidad opcionales. No Kubernetes, broker ni Redis obligatorio.

## Hosting económico

| Opción | Evidencia de precio / capacidad al revisar | Ventaja y costo operativo |
|---|---|---|
| VPS Hetzner | [Tarifas revisadas junio 2026](https://docs.hetzner.com/general/infrastructure-and-availability/price-adjustment/); confirmar moneda/región/impuestos al cotizar | Control de Docker/PostGIS y bajo costo fijo; backups, parcheo y recuperación propios; latencia argentina a medir |
| Render | [Pricing](https://render.com/pricing); [PostgreSQL documenta PostGIS](https://render.com/docs/postgresql). Total de plan adecuado no confirmado | Menos trabajo de plataforma y DB gestionada; sumar API, worker si separado, DB, disco y egreso |
| Railway | [Hobby USD 5 / Pro USD 20 de consumo mínimo](https://railway.com/pricing), con consumo adicional | Despliegue sencillo; presupuesto por recursos. Verificar imagen/volumen PostGIS y backup; no asumir DB totalmente gestionada por usar plantilla |

Recomendación: PoC local; beta en un PaaS con PostGIS gestionado si entra en presupuesto. VPS como alternativa cuando se acepta operación manual. No elegir por el mínimo publicitario: persistencia, memoria, recuperación y latencia importan. No se usará plan gratuito con suspensión para prometer ingestas continuas. Región se decide midiendo latencia desde Argentina y considerando tratamiento de datos.

## Presupuesto parametrizado

`Costo mensual = compute + DB + backup + egreso + tiles + routing + clima + identidad + monitoreo + impuestos`.

Supuesto de trabajo: 1.000 usuarios activos, 4 análisis/mes, 1 ruta principal + hasta 4 consultas de desvío por análisis ⇒ hasta 20.000 requests de routing/mes antes de reintentos. Matrices facturadas por pares se presupuestan aparte: <=64 pares/análisis ⇒ hasta 256.000 pares/mes. El supuesto es techo de planificación, no medición.

Con 20.000 llamadas que calificaran únicamente para Google Compute Routes Essentials, la tarifa documentada daría USD 50 después de 10.000 gratis, **sin** matrices, mapas ni geocoding. Ese mismo número de Directions quedaría bajo la franquicia Mapbox observada, pero tampoco incluye los otros productos. Ver [comparativa](ROUTING.md). No extrapolar precio de Directions a Matrix ni contar un lote multipunto como una única unidad facturable meteorológica.

Envolvente propia preliminar: USD 30–100/mes de infraestructura y USD 0–150/mes de servicios para beta pequeña, más impuestos y posibles licencias de datos; **estimación de presupuesto**, no oferta comercial ni garantía de suficiencia. Puede superarse por planes mínimos, tiles o pronóstico comercial. No convertir a pesos con un tipo de cambio inventado. Elegir y aprobar presupuesto concreto después de PoC y cotización; no contratar servicios en esta etapa.

## CI/CD

GitHub Actions propuesta: validación de documentos/OpenAPI → restore de dependencias bloqueadas → lint/análisis estático → unit tests → integración PostGIS → build Flutter Android → build/scaneo de imagen → staging → smoke → promoción controlada de misma imagen a producción. Permisos mínimos de workflow y secrets por environment; evitar secrets en pull requests no confiables. iOS requiere runner macOS, certificados y prueba específica antes de anunciar soporte.

Migrations EF explícitas, revisadas y ejecutadas con rol separado como job único. No hacer migración automática desde cada réplica API. Cambios expand/contract compatibles con rollback de binario; backup antes de cambios riesgosos y ensayo de restauración. Rollback de base no equivale a ejecutar Down indiscriminadamente.

Staging separado en DB, credenciales y cuotas; datos sintéticos/sanitizados. Imágenes etiquetadas por commit/digest; configuración externa y secretos fuera de repositorio. Deploy con health checks y rollback ante fallo de readiness. No incluir pruebas masivas de APIs reales en CI.

## Recuperación

Objetivos propuestos para beta: RPO <=24 h y RTO <=4 h. Backups diarios cifrados fuera de la máquina, 30 días de retención; prueba mensual de restore a instancia aislada. Si se exige RPO menor, habilitar WAL/PITR gestionado y actualizar costos. Verificar también roles, extensión PostGIS y reanudación de jobs.

Runbooks mínimos: proveedor caído; esquema roto; fuente desactualizada; cuota agotada; DB sin espacio; leak de secreto; rollback; purga/borrado; conflicto entre reportes. Cada runbook contiene detección, diagnóstico sin PII, contención, recuperación y prueba de cierre.

## Observabilidad desde el primer adaptador

Logging estructurado y OpenTelemetry para trazas/métricas. CorrelationId aleatorio; excluir nombres de lugares, coordenadas, tokens y parámetros de requests externas de logs. Logs de ingesta guardan sourceId/hash/conteos, no viajes.

| Panel | Métricas / campos | Señal accionable propuesta |
|---|---|---|
| Proveedor | Último intento/éxito, sourceUpdatedAt, latencia p50/p95, errores/429, circuit state | Frescura sobre umbral o 5 fallos consecutivos |
| Calidad | Filas recibidas/aceptadas/rechazadas, geometrías inválidas, edades, cobertura | Caída abrupta >20% de filas sin explicación; no publicar lote automáticamente |
| Análisis | Duración, completos/parciales, errores por código, requests por análisis | Presupuesto >12 o p95 >8 s en ventana con muestra suficiente |
| Cache | Hits/misses, edad y versión | Caída de hit ratio con aumento de costo |
| Jobs | Lag, duración, retries, lease, heartbeat, última publicación | Job no corre durante dos intervalos previstos |
| Infra | CPU/RAM/espacio, conexiones DB, consultas lentas | Disco >80%, DB indisponible |
| Costo | Unidades por producto/proveedor y gasto estimado | Avisos al 50/80/100% del presupuesto; corte de funciones no esenciales |

Dashboard básico: tabla por fuente con estado de acceso y estado de calidad separados, cobertura, último dato, última ingesta, errores y enlace a runbook; gráficos de 24 h de latencia/fallos; contador de jobs. Grafana con Prometheus/OTel o servicio gestionado según costo. Métricas no usan tripId/lat/lon como labels de alta cardinalidad.

`/health/live`: proceso vivo; `/health/ready`: DB/esquema/recurso mínimo listo. Caída de SMN no reinicia la API en bucle: el servicio sigue devolviendo resultados parciales. Salud pública resumida en API; diagnóstico detallado sólo administradores. Alarmas agrupadas para no inundar al único operador.

## Escala y evolución

Primero optimizar índices, cache y queries. Separar worker por consumo, después escalar API. Añadir Redis sólo por medición. Si fuentes oficiales no admiten más polling, aumentar usuarios no debe aumentar consultas a esas fuentes. Escala nacional es tanto cobertura operativa como capacidad de cómputo; más servidores no solucionan datos faltantes.
