# Motor de análisis de viaje

Especificación de reglas propuesta. Todos los ejemplos numéricos son sintéticos. Las ventanas no representan probabilidades calibradas ni garantías de llegada.

## 1. Pipeline reproducible

Entrada: ruta completa versionada, salida con zona, vehículo/snapshot, combustible inicial, reserva, paradas aceptadas, fuentes con versiones y cobertura. Salida: TravelAssessment con condiciones, calidad de información, razones ordenadas, intervalos afectados, plan estimado de combustible, fuente/fecha por dato y limitaciones.

Servicios puros para timeline, consumo y evaluación; consultas espaciales detrás de un servicio probado con PostGIS real; IO fuera del motor de reglas. Resultado determinista para el mismo input, reloj y versión de reglas. No IA necesaria.

## 2. Route Segment Timeline

Cada edge `i` tiene longitud `d_i` y duración `t_i`. `ETA_i = salida + suma(t_j, j<i) + suma(paradas previas)`. Dentro del edge, interpolar por distancia local si el proveedor no aporta perfil más preciso. Con sólo duración total, usar proporción de longitud como **fallback de baja confianza**, indicándolo; no simular velocidades detalladas.

Segmentos de presentación por localidad relevante, cambio de vía, evento o intervalo de tiempo propuesto de 30 minutos; no confundirlos con aristas del grafo. Mantener índices de geometría para consultar exposiciones exactas.

Incertidumbre inicial configurable: ventana `ETA ± max(15 minutos, 20% del tiempo de conducción acumulado)`. Es una regla conservadora de producto para sensibilidad, **no intervalo estadístico de confianza**. Las pausas omitidas pueden desplazar más el viaje. Redondear horarios presentados a 5–15 minutos según calidad. Mostrar duración de conducción y duración con pausas separadas.

| Distancia sintética acumulada | Hora central sintética | Consideración |
|---|---|---|
| 0 km | 07:00 | Salida indicada |
| 300 km | 10:30 | Duración de tramos del proveedor |
| 600 km | 14:00 | Incluir pausas aceptadas antes |
| 900 km | 18:00 | Ventana más amplia por acumulación |

No extrapolar estas velocidades a otro recorrido. Cambiar hora, ruta, combustible o paradas relevantes invalida assessment. Medianoche y viajes de varios días usan instantes UTC y zona IANA, no strings de hora. El MVP no recomienda conducir sin descanso; permite ingresar pausas y advierte cuando el cálculo sólo incluye conducción. No introduce reservas de hotel.

## 3. Weather Matching

Para cada intervalo espacial dentro del polígono, obtener `[entrada_min,salida_max]` desde el timeline. Comparar con validez `[alerta_desde,alerta_hasta)`:

`match = entrada_min < alerta_hasta AND salida_max > alerta_desde`.

Si sólo coincide la banda de incertidumbre, motivo `possible_temporal_overlap`; si coincide el intervalo central, `estimated_temporal_overlap`. Una alerta que terminó antes del extremo temprano de llegada no genera aviso activo para ese paso. Si no hay fin explícito, no extenderla indefinidamente: marcar temporalidad incierta y aplicar política de fuente.

Conservar fenómeno y nivel original, emisor, área, inicio/fin y recomendaciones oficiales con atribución permitida. No convertir una zona provincial entera en tormenta sobre cada punto. Si sólo se recibe un código de zona, resolver a geometría oficial de esa misma versión; provincia sin delimitación útil = baja precisión, aviso contextual separado.

Forecast: muestrear por celda espacial/horaria y cambios de altitud/entorno relevantes según resolución disponible. Deduplicar requests por modelo, ciclo y celda. Obtener precipitación/probabilidad, ráfagas, viento/dirección, visibilidad, temperatura y nieve si están disponibles. No representar una grilla horaria como observación exacta de un punto y minuto.

Reglas iniciales de relevancia: mostrar alertas oficiales intersectadas; agrupar lluvia/nieve/viento/visibilidad que el producto fuente califique o que supere umbrales configurados y revisados antes de beta. **No se fijan umbrales universales de seguridad de conducción en esta etapa.** Temperatura cercana a congelación con precipitación puede indicar condiciones compatibles con hielo, nunca confirmar hielo sobre calzada. Las reglas cuantitativas se validan con especialista, vehículo y contexto.

La precipitación de un intervalo acumulado se conserva con esa unidad; no multiplicar mm por probabilidad ni sumar tasas sin integrar tiempo. Viento transversal estimado puede ser futuro: requiere rumbo del tramo y dirección de viento, y no sustituye evaluación por vehículo.

Viaje fuera del horizonte: devolver clima no disponible para esa parte. Alerta vigente hoy no implica validez dentro de diez días. Una buena respuesta del servidor con forecast truncado también puede ser cobertura insuficiente.

## 4. Road Condition Engine

Tipos `closure`, `restriction`, `construction`, `surface_condition`, `precaution`; `RoadStatus` es proyección temporal por tramo. Regla de aplicabilidad: correspondencia vial y sentido + categoría de vehículo + intervalo + precisión. Un corte total confirmado bloquea el tramo del recorrido evaluado aunque el proveedor de routing lo incluya.

Cortes/restricciones con fin desconocido se muestran como “informado; vigencia actual no confirmada” si perdieron frescura, nunca como reabiertos. Reapertura explícita cancela el aviso correspondiente; no inferirla por desaparición en un feed parcial. Reportes contradictorios conservan ambas evidencias y una advertencia de conflicto; preferir autoridad competente y revisión posterior del mismo evento, no “el dato que llegó último”.

No deducir demora fija de una obra sin dato. Mostrar posible demora no cuantificada. No recalcular automáticamente alternativas que incumplan las limitaciones de routing; el usuario puede solicitar nueva ruta y obtener otro assessment.

## 5. Fuel Engine

Inputs obligatorios para cálculo: consumo L/100 km y combustible compatible. Para autonomía/paradas: tanque, litros iniciales o porcentaje convertido explícitamente y reserva elegida. Marca, modelo, año y consumo urbano son metadatos opcionales; prima consumo manual del viaje. Rechazar consumo cero, reserva mayor/igual al tanque y combustible inicial fuera de `[0,tanque]`.

Fórmulas:

- `litros_consumidos = distancia_km × consumo_L100 / 100`.
- `autonomía_útil_km = max(0, litros_actuales − reserva_litros) × 100 / consumo_L100`.
- `costo_de_consumo = litros_consumidos × precio_referencia` cuando se usa un único precio válido.
- `litros_a_comprar = max(0, litros_consumidos + combustible_final_deseado − litros_iniciales)`, con límite de tanque por parada.
- `desembolso_en_ruta = suma(litros_comprados_j × precio_j)`; no equivale al costo del combustible consumido.

Ejemplo: 1.200 km, 7,5 L/100 km ⇒ 90 L. Tanque 47 L, inicio 30 L, reserva final 5 L ⇒ autonomía útil inicial 333,33 km y compras mínimas 65 L si es factible conservar reserva. Con precio hipotético ARS 1.500/L ⇒ consumo valorizado ARS 135.000; compras teóricas ARS 97.500. No son precios actuales ni incluyen desvíos. Tanque lleno con reserva de 5 L ⇒ 560 km útiles, no 626,67 km.

Rango opcional de sensibilidad usando consumo ±15% elegido como supuesto: 76,5–103,5 L en ese ejemplo. Explicar que viento, carga, velocidad, relieve y esperas pueden cambiarlo; no afirmar cobertura estadística.

## 6. Plan de carga y ranking explicable

1. Proyectar estaciones al progreso de ruta, filtrar producto reportado compatible y calidad de ubicación.
2. Buscar estaciones antes de agotar autonomía útil. No elegir la primera que esté después del punto crítico.
3. Verificar desvío vial y consumo del acceso/retorno. Producto reportado ≠ stock; horario desconocido ≠ abierto.
4. Construir grafo dirigido acotado: origen, estaciones candidatas en orden y destino. Arista sólo si puede recorrerse conservando reserva. Verificar también conexión posterior para evitar una estación alcanzable que lleve a un callejón sin siguiente carga posible.
5. MVP: plan factible priorizando menor cantidad de paradas y desvío, con carga hasta capacidad como estrategia sencilla. No anunciar optimización global de costo. Comparar precios frescos entre alternativas factibles cercanas; compras parciales óptimas quedan posteriores.
6. Generar hasta 3 alternativas explicadas por ventana. Si se supera límite de candidatos/requests, declarar plan parcial sin prometer cobertura de toda la autonomía.

Orden lexicográfico preferido a score opaco: factibilidad de reserva → acceso y calidad → desvío/tiempo → precio comparable → desempate estable. Cuando se utiliza costo generalizado, mostrar componentes monetarios y supuesto de valoración del tiempo; no sumar litros, km y pesos sin normalizar.

Ejemplo de rechazo: estación A ahorra ARS 50/L en 20 L, pero exige 30 km adicionales; a 7,5 L/100 km consume 2,25 L extra. Con precio supuesto ARS 1.500/L, el extra ya cuesta ARS 3.375 y supera el ahorro de ARS 1.000, antes del tiempo. No recomendar A por su precio menor.

Explicación de sugerencia: “Antes del límite estimado de autonomía; producto reportado compatible; desvío estimado 4 km; precio declarado el [fecha]. Confirmá disponibilidad”. Si el precio es viejo, se excluye del ranking económico, pero la estación puede seguir como candidata por alcance si su ubicación es válida.

Ninguna estación alcanzable: “No pudimos construir un plan de carga con la información disponible”. Distinguir imposibilidad matemática conocida de falta de datos. No sugerir agotar reserva ni inventar una estación.

## 7. Calidad, cobertura y caducidad

Cada dimensión tiene `available / partial / unavailable / stale`, alcance espacial y horizonte temporal, `lastSuccessAt`, `sourceUpdatedAt`, `validUntil` y huecos. Porcentaje de kilómetros con información no equivale a porcentaje de seguridad. Listado de eventos sin contrato de cobertura no acredita el 100% libre de eventos.

Política inicial propia a calibrar:

| Información | Frescura de ingesta propuesta | Regla adicional |
|---|---|---|
| Alertas | Degradar cobertura tras 15 min sin actualización exitosa | Usar intervalo de validez y revisiones; avisos conocidos no desaparecen |
| Estado vial | Degradar tras 60 min sin actualización exitosa | Evento viejo/fin desconocido permanece como no confirmado |
| Forecast | Cache 30 min; degradar si ciclo >6 h o contrato indica antes | Nunca fuera de horizonte; proveedor puede requerir política más estricta |
| Precio | <=24 h reciente; 1–7 días referencia antigua visible | >7 días fuera del cálculo automático/ranking; umbrales internos |
| Inventario estación | Edad de inventario siempre visible | No garantiza operación, stock u horario |

Una respuesta 200 idéntica no actualiza `sourceUpdatedAt`. Puede acreditar salud de acceso, pero sólo un contrato explícito de snapshot vigente permite renovar certeza de consulta sin nueva emisión.

## 8. TripAssessmentEngine

Se calculan dos ejes: **condición observada** y **suficiencia de información**. El encabezado resume sin ocultar ninguno.

| Evidencia | Encabezado | Complemento obligatorio |
|---|---|---|
| Corte/restricción impeditiva confirmada o alerta oficial de alta severidad intersectada según reglas | CONDICIONES ADVERSAS | Motivos, intervalos y cobertura faltante si existe |
| Precaución oficial/obra o posible afectación relevante | PRECAUCIÓN | Calidad de cada motivo; advertencias por datos insuficientes |
| Sin motivos conocidos pero cobertura crítica incompleta/vencida | INFORMACIÓN INSUFICIENTE | Qué faltó y cómo consultar fuente |
| Sin motivos y todas las dimensiones críticas habilitadas cubren la ruta/tiempo | CONDICIONES FAVORABLES | “No se detectaron advertencias relevantes en las fuentes consultadas” |

No esconder un corte conocido detrás de “información insuficiente”: conservar adversas con cobertura parcial. Tampoco permitir favorable si no se pudo consultar alertas o estado vial. La falta de precio sólo afecta estimación económica; no vuelve adverso el clima. Falta de plan de combustible se muestra como precaución logística o insuficiencia, según evidencia. Hidrología no es dimensión requerida en MVP.

Razón estructurada: `code`, `category`, `severity`, `message`, `sourceRef`, `evidenceVersion`, `distanceFromM/toM`, `passageFrom/to`, `validFrom/to`, `confidence`, `recommendedAction`. Recomendación redactada a partir de fuente y límites del producto; no imperativos de circular por vías inseguras. Ordenar por impacto y proximidad temporal, deduplicar emisiones del mismo episodio y permitir desplegar detalle.

## 9. Evolución

Notificación futura: comparar revisiones de assessment sobre un viaje guardado, programar reevaluación antes de salida (ejemplo 2 h) y avisar sólo cambios relevantes, no cada ingesta. Opt-in, quiet hours, deduplicación, TTL y reintentos. No prometer entrega inmediata de FCM.

Modo viaje: ubicación puntual opt-in para reevaluar remanente, intervalos adaptativos y límites de batería; no guía giro a giro. Separar ruta planificada de posición observada. Si hay desvío, generar nueva versión. Pantallas diseñadas para consulta detenido o acompañante, no fomentar interacción conduciendo.
