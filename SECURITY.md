# Seguridad y privacidad

## Modelo de amenaza

Activos: ubicaciones planeadas, vehículos, identidad, API keys, presupuesto de proveedores e integridad de avisos. Amenazas principales: acceso a viajes ajenos, extracción masiva de ubicaciones, abuso de routing, payload geográfico costoso, dependencia comprometida, logs con coordenadas y fuente obsoleta presentada como actual.

| Amenaza | Control y prueba |
|---|---|
| BOLA/IDOR | Filtrar por owner/session en cada acceso; test usuario A consulta/modifica ID de B ⇒ 404 |
| Robo de tokens | Secure storage móvil; JWT de corta duración, issuer/audience/algoritmo/exp verificados |
| Sesión anónima extraída | Token opaco aleatorio, hash almacenado, TTL 24 h; revocable y nunca en URL |
| Refresh token robado | OIDC/PKCE, rotación/revocación y detección de reutilización según proveedor; evitar implementar auth propia |
| Consumo abusivo de APIs | Cuotas por principal y agregadas por proveedor, límites monetarios y de concurrencia |
| DoS espacial | Body máximo propuesto 64 KB; sólo puntos/inputs limitados, no polígonos enviados por cliente; timeout SQL |
| SSRF | Hosts externos configurados/allowlist; ninguna URL de proveedor elegida por usuario; validar redirects |
| Inyección | SQL parametrizado, validación de CSV/JSON, escape de contenido al presentar fuente |
| Fuga de secretos | Variables/secret manager, CI secret scan, claves server-side, rotación y permisos mínimos |
| Datos manipulados | HTTPS con certificados válidos, esquema, límites, hashes, cuarentena y auditoría de ingesta |
| Caída de proveedor | Deadline, circuit breaker, fallback explicado; no inventar datos |

Rate limit interno inicial: anónimo 5 análisis/h y 20/día; cuenta 20/h y 100/día, además límite global que respete presupuesto real. Son propuestas a calibrar, no límites de servicios externos. IP como señal complementaria con cuidado por NAT móvil, no único identificador. Publicar 429 útil. Endpoints administrativos bajo rol, sin acceso público a payloads crudos.

HTTPS en todos los entornos públicos; DB sin puerto público; roles separados de migración/runtime; backups cifrados; contenedores sin privilegios, imágenes fijadas y actualización regular. Claves públicas restringidas de tiles en mobile no se confunden con secretos de backend. TLS nunca se desactiva para resolver fallos de proveedor.

## Privacidad por diseño

No tracking continuo en MVP. Coordenadas sólo cuando el usuario confirma un viaje o usa voluntariamente ubicación actual. La app funciona con búsqueda manual. No enviar identidad/vehículo al proveedor de routing si sólo necesita puntos y perfil. Mapas y routing pueden recibir datos de ubicación: incluir proveedores, finalidad y transferencias en aviso.

| Datos | Finalidad | Retención propuesta |
|---|---|---|
| Viaje anónimo/backend y token | Consultar resultado temporal | 24 h; purga programada |
| Viaje guardado local | Reutilización solicitada | Hasta eliminación local; control del usuario |
| Viaje guardado en cuenta | Sincronización voluntaria | Hasta borrado; política de inactividad propuesta 12 meses con aviso previo |
| Vehículo/cuenta | Preferencias y propiedad | Hasta borrado o política de inactividad informada |
| Coordenadas en logs | No necesarias | No registrar; tampoco en URLs de traces |
| Logs técnicos sin PII | Diagnóstico | 14 días |
| Eventos mínimos de seguridad | Investigar abuso | 30 días, acceso restringido y minimización |
| Métricas agregadas | Operación | 90 días sin rutas, identidad ni IP |
| Backups cifrados | Recuperación | Ventana 30 días; borrados reaplicados tras restore |
| Payloads de fuentes públicas | Auditoría | Máximo 30 días si licencia lo permite; no confundir con datos de usuarios |

Estos plazos son decisiones propuestas del producto, no plazos legales atribuidos a una norma. Licencias más restrictivas prevalecen. Borrado lógico inmediato para impedir acceso y físico por job <=24 h; backups expiran y se mantienen inaccesibles al usuario, con tombstones para no resucitar datos. Tests verifican purga y restore.

## Marco argentino

La [Ley 25.326 actualizada](https://www.argentina.gob.ar/normativa/nacional/64790/actualizacion) es referencia para calidad, finalidad, información y derechos del titular. La [AAIP](https://www.argentina.gob.ar/aaip/datospersonales) ofrece información de cumplimiento. Antes de beta comercial: definir responsable, base aplicable, aviso de privacidad, medios de acceso/rectificación/supresión, proveedores/transferencias y obligaciones de registro que correspondan. Revisión jurídica del caso concreto pendiente; no se declara cumplimiento por usar JWT o consentimiento genérico.

Ubicación puede revelar domicilio, hábitos y viajes futuros. Evitar publicidad basada en rutas y exportaciones de analytics con coordenadas. Notificaciones futuras opt-in y contenido discreto en pantalla bloqueada. Modo viaje requerirá evaluación y consentimiento separados; activar push no autoriza tracking.

Responsabilidad de recomendaciones: fuentes/fechas visibles, lenguaje de estimación, cobertura explícita, corrección de datos y canal de reporte de errores del producto. No sustituir avisos oficiales ni órdenes de autoridad. La UI debe hacer entendible la limitación; no ocultarla en un descargo extenso.
