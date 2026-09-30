# Registro de decisiones

Estado al 2026-09-29. “Adoptada” significa decisión de diseño para este plan; no contratación ni despliegue. Las recomendaciones condicionadas se mantienen como pendientes hasta tener evidencia.

| ID | Decisión | Estado | Justificación / puerta |
|---|---|---|---|
| D01 | Capa de análisis; navegación externa | Adoptada | Diferencial solicitado y alcance individual |
| D02 | Monolito modular ASP.NET Core .NET 10 LTS | Adoptada | Operación simple y runtime soportado |
| D03 | PostgreSQL + PostGIS | Adoptada | Intersección, distancias e índices compartidos |
| D04 | Flutter; beta Android primero | Propuesta operativa | iOS requiere infraestructura/pruebas propias |
| D05 | ORS primer candidato PoC | Condicionada | Cuenta, licencia, costos y corpus argentino pendientes |
| D06 | MapLibre renderer preferido, tiles comerciales separados | Condicionada | Validar plugin, licencia y presupuesto |
| D07 | GeoRef para localidades con pin confirmado | Adoptada para PoC | D/T confirmados; SLA/licencia concreta de exportaciones pendientes |
| D08 | No depender de precios 314 como vigentes | Adoptada | Cambio normativo y frescura no auditada |
| D09 | SMN prioritario; forecast externo complementario | Condicionada | Feed SMN no aprobado; externo no sustituye emisor oficial |
| D10 | Estado vial por cobertura verificable | Adoptada | Fuente nacional automatizada no aprobada |
| D11 | Condiciones + cobertura, sin score único | Adoptada | Ausencia de datos no prueba condiciones favorables |
| D12 | Reserva y combustible inicial por viaje | Adoptada | Necesarios para autonomía real estimada |
| D13 | Sin scraping en diseño base | Adoptada | Sostenibilidad, contratos y derechos no acreditados |
| D14 | Sin Redis, microservicios, Kubernetes ni broker inicial | Adoptada | No hay carga medida que lo justifique |
| D15 | Hidrología/AlertaRío fuera de MVP, contrato desacoplado | Adoptada | Altura de río no equivale a impacto vial |
| D16 | Cuenta opcional, guardado local y retención mínima | Adoptada | Minimización de ubicación |
| D17 | Notificaciones y modo viaje posteriores | Adoptada | Requieren operación continua y consentimiento |

## Pendientes con responsable y disparador

| Decisión pendiente | Responsable propuesto | Evidencia requerida / fecha lógica |
|---|---|---|
| Integración oficial SMN | Desarrollador + organismo | Contrato, muestra, permiso, revisiones; cierre F2 |
| Estado vial automatizado y cobertura RP | Desarrollador + autoridades | Fuente sostenible y km calibrados; cierre F2 |
| Fuente actual de estaciones/precios | Desarrollador | Auditoría, licencia, fecha real; cierre F2 |
| Proveedor routing definitivo y renderer compatible | Responsable de producto/desarrollador | Benchmark, costos y términos; F1 antes de fijar adapters |
| Presupuesto, hosting y tiles | Responsable del proyecto | Cotización actual y costo por análisis; antes de beta |
| Umbrales de forecast para conducción | Especialista de dominio + producto | Validación documentada y lenguaje; antes de beta |
| Corredores y promesa de beta | Producto | Cobertura demostrada, no sólo disponibilidad de geometría |
| Proveedor OIDC y revisión privacidad/licencias | Responsable del proyecto | DPA, transferencias y operación; antes de guardar cuentas reales |
| Inclusión de plan multietapa SHOULD | Producto/desarrollador | Factibilidad y presupuesto F5; no fingirlo con heurística incompleta |
| iOS inicial o posterior | Producto | Dispositivo/runner, firma, distribución y capacidad |

## ADR

[Índice de decisiones arquitectónicas](docs/adr/README.md). Toda modificación de fuente, condiciones del estado general, tratamiento de datos o proveedor incompatible debe actualizar ADR y pruebas, no sólo configuración.
