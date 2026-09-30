# Visión de producto

RutaSmart Argentina transforma información dispersa sobre un recorrido en un informe previo al viaje: qué se sabe, dónde afecta, cuándo podría coincidir con el paso del vehículo y qué información falta.

## Problema y usuarios

Hoy un conductor debe combinar publicaciones de rutas, mapas, alertas por zonas, pronósticos por localidad y referencias de combustible. La dificultad está en relacionarlas con su itinerario y hora de salida. Más información sin contexto puede generar ruido o falsa tranquilidad.

Usuario primario: conductor particular que prepara un viaje interurbano en automóvil. Secundarios: familias y viajeros frecuentes. Validar con 8–12 entrevistas y 5 pruebas de uso; son metas de investigación propuestas, no investigación ya realizada.

Transportistas y flotas son mercados posteriores. Un perfil de automóvil no resuelve límites de altura, peso, cargas peligrosas, jornadas laborales ni restricciones comerciales. No presentar el MVP como apto para camiones.

## Propuesta de valor

Un informe entendible y trazable que relacione **recorrido + ventana de paso + condiciones + vehículo**. La ventaja a comprobar es reducir el esfuerzo para preparar el viaje y encontrar avisos realmente relevantes. No se afirma exclusividad frente al mercado sin validación comercial.

Principios: honestidad sobre cobertura; explicación por motivo; enlaces a emisores; estimaciones con fecha; privacidad por defecto; accesibilidad; continuidad con navegadores existentes. Nunca “es seguro viajar”.

## Validación y éxito

Hipótesis H1: los usuarios comprenden la diferencia entre condiciones favorables y ausencia de datos. H2: pueden identificar tramo, horario y fuente del principal aviso en menos de un minuto. H3: autonomía y desvío aportan más utilidad que ordenar estaciones por precio. H4: una parte vuelve a analizar antes de salir.

Medir finalización de análisis, comprensión de cobertura, fallos de proveedor, costo por análisis, apertura de fuente, uso de navegación externa y retorno voluntario. No medir reducción de accidentes ni “seguridad lograda”: el producto no permite atribuir ese resultado. Recoger métricas agregadas, sin almacenar rutas personales en analytics.

## Negocio futuro: hipótesis, no funcionalidad MVP

| Segmento | Valor a explorar | Condición previa |
|---|---|---|
| B2C premium | Viajes guardados, cambios relevantes y comparaciones de salida | Retención demostrada y avisos confiables |
| Transportistas y flotas | Evaluación de corredores y restricciones por vehículo | Perfil de camión y acuerdos de datos |
| Empresas y logística | API de análisis, lotes y SLA | Costos unitarios y aislamiento por organización |
| Agro | Accesibilidad de corredores rurales | Fuentes locales y transitabilidad verificable |
| Turismo | Informe previo compartible | Licencias de redistribución y explicación simple |
| Seguros | Información contextual para asistencia | Consentimiento y evaluación legal; sin inferir conducta del conductor |
| Municipios | Publicación interoperable de eventos | Convenios y responsabilidad editorial definida |

La futura operación conceptual `POST /route-risk-analysis` podrá reutilizar el motor mediante una API B2B versionada. No se implementa ahora ni se promete una probabilidad de siniestro. Monetizar alertas no debe ocultar su fuente ni presentar recomendaciones comerciales como evaluación técnica.

## AlertaRío Argentina

RutaSmart podrá consumir un servicio de inteligencia hidrológica independiente mediante contratos de eventos con geometría, intervalo, fuente, incertidumbre y relación con infraestructura. El sistema funciona sin él. No compartir bases de datos, identidad ni despliegue por conveniencia. Ninguna altura de río se transforma automáticamente en corte vial.
