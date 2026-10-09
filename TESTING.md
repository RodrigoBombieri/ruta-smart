# Pruebas

## Ejecutar

```powershell
dotnet test RutaSmart.slnx
cd mobile/rutasmart
flutter analyze
flutter test
flutter build web --release --dart-define=API_URL=
```

Para incluir PostGIS, definir `RUTASMART_TEST_DB` con una conexión de prueba antes de ejecutar .NET. El test crea un esquema propio y lo elimina al terminar. Sin esa variable, se omite.

Con la app local encendida, desde la raíz:

```powershell
npm ci
npm run test:e2e
```

Requiere Chrome instalado. El recorrido usa fuentes reales y puede fallar si un proveedor no responde; no sustituye las pruebas deterministas. `RUTASMART_SCREENSHOTS=docs/screenshots` guarda capturas para el README.

## Qué se verifica

- Consumo/reserva, costos nulos, entradas inválidas y zonas horarias.
- Intersecciones con huecos/bucles, vigencia CAP, cancelaciones y cobertura parcial.
- CSV de precios, antigüedad y estaciones repartidas por el viaje.
- Propiedad, idempotencia, versiones, borrado y expiración; persistencia PostGIS real.
- Formularios móviles, vehículo, informes y almacenamiento local.
- Chrome: análisis Buenos Aires–Rosario, informe, guardado y vistas responsive.

## Última revisión

Build web y APK debug exitosos; Flutter analyze sin hallazgos y 5 tests aprobados. El recorrido real en Chrome devolvió 302,143 km, 20 estaciones candidatas y estado de información insuficiente. La suite .NET y las capturas finales se registran en [verificación](docs/research/VERIFICATION.md).

No hay prueba de conducción, beta de usuarios ni validación nacional. La APK fue compilada; falta prueba en dispositivo/emulador. GitHub Actions valida .NET/PostGIS y Flutter sin fuentes externas; no se afirma una ejecución remota hasta verla en GitHub.
