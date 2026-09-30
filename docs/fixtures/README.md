# Fixtures sintéticas

`scenarios.json` es un corpus inicial de contrato/dominio. No son datos oficiales ni una ruta de carretera real. Fechas 2030 elegidas para separar estos ejemplos de información vigente.

La ruta de tres vértices tiene dos edges de 1.800 segundos. Los tiempos se dan por edge; las longitudes se calculan geodésicamente en los futuros tests GIS. No comparar esas distancias con los 1.200 km del caso aritmético T13, que es independiente.

T03 no intersecta aunque esté cerca. T04 cruza la línea dentro de la validez. T05 terminó una hora antes de salida, incluso considerando la banda propuesta. T13 calcula consumo, autonomía y compras mínimas sin desvíos; no garantiza un plan de estaciones factible.

Ampliar con ruta de bucle, polígono con hueco, cruce a distinto nivel, evento vial ambiguo y respuestas de fallo por proveedor según TESTING.md. Los futuros tests deben crear fuente `synthetic`, reloj fijo y aislamiento de producción. Registrar una respuesta real sólo con permiso, sin credenciales y con procedencia/fecha/hash.
