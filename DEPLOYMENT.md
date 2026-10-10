# Ejecución y configuración

## Local

Requisitos: .NET 10, Flutter 3.29.2 y PowerShell. `./scripts/run-local.ps1` compila web y sirve la app en `http://localhost:5080`. `-SkipBuild` reutiliza el build existente. Sin base configurada, los datos del servidor se pierden al reiniciar.

Para PostgreSQL/PostGIS, con Docker activo:

```powershell
Copy-Item .env.example .env
# Editar POSTGRES_PASSWORD en .env antes de continuar.
docker compose --env-file .env -f infrastructure/compose.yaml -f infrastructure/compose.dev.yaml up -d db
./scripts/run-local.ps1 -WithDatabase
```

La base usa `localhost:55438`. Compose se llama `rutasmart`; su volumen es independiente. La migración SQL inicial se ejecuta al crear un volumen vacío. Cambiar el SQL no migra una base ya existente.

## Android

Desde `mobile/rutasmart`, con JDK y Android SDK instalados:

```powershell
flutter build apk --debug --dart-define=API_URL=http://10.0.2.2:5080
```

Salida: `build/app/outputs/flutter-apk/app-debug.apk`. `10.0.2.2` apunta al host desde el emulador; para un teléfono real usar un servidor accesible. El HTTP local solo se permite en debug. Release requiere firma propia y URL HTTPS.

## Configuración

| Variable | Función |
|---|---|
| `ConnectionStrings__Database` | Conexión Npgsql; obligatoria fuera de Development/Testing |
| `Routing__Provider` | `osrm_public` en desarrollo o `ors` |
| `Routing__Enabled`, `Routing__PersistenceApproved` | Habilitación ORS y compatibilidad de almacenamiento |
| `Routing__ApiKey`, `Routing__CoverageWkt` | Clave privada y polígono WGS84 de cobertura ORS |
| `Energy__CsvPath` | Importación local del CSV público |
| `Energy__AllowPublicHttp` | Descarga HTTP; true en desarrollo, false en producción |
| `Cors__Origins__0` | Origen permitido si el cliente se sirve por separado |
| `API_URL` (dart-define) | Base de API; vacío usa el mismo origen en web |
| `TILES_URL`, `TILES_ATTRIBUTION` (dart-define) | Proveedor cartográfico y atribución |

## Contenedor

Compilar web antes de construir la imagen: el Dockerfile copia `mobile/rutasmart/build/web`. El Compose base ejecuta Production y deshabilita demos; el override dev habilita el routing nacional de prueba local. No hay despliegue público realizado.

Una sola instancia de API. Antes de publicar: HTTPS, proveedor de routing apto, firma Android, backups probados, operación de fuentes y beta. No usar el servidor demo OSRM como infraestructura comercial.
