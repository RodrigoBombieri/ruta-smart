# Seguridad y privacidad

## Implementado

- Tokens aleatorios de 256 bits; el servidor guarda su hash y vence la sesión a 24 horas.
- Autorización por propietario en lectura, versiones y borrado; consultas SQL parametrizadas.
- Límites de cuerpo, tiempo y solicitudes. URLs de proveedores fijas y sin redirecciones automáticas.
- CAP con DTD deshabilitado, tamaño limitado y validación de emisor/geometría.
- Secretos en configuración local; `.env`, `.local/` y archivos de build excluidos de Git.
- Sin GPS continuo, analytics ni credenciales de proveedores en Flutter.
- Sin logs HTTP de coordenadas, búsquedas o tokens. Errores internos con respuesta genérica.

## Datos y borrado

Viajes del servidor: acceso temporal y purga cada 5 minutos al vencer su retención de 24 horas. La sesión puede vencer antes que un informe creado posteriormente; sin sesión válida no se permite consultarlo.

Vehículo e informes guardados: preferencias locales, sin cifrado adicional de la app. Los informes se guardan solo a pedido, con un máximo de 50, y pueden borrarse. Eliminar la copia local y borrar el informe del servidor son acciones separadas. El token de sesión no se persiste en el cliente. Android deshabilita backup automático de la app.

Routing recibe origen y destino; GeoRef recibe búsquedas; el proveedor de tiles recibe solicitudes de la vista del mapa. Esos servicios tienen sus propias políticas.

## Antes de producción

Configurar HTTPS y proxy confiable; gestionar secretos y backups; ensayar restauración y borrado; revisar privacidad, proveedores y distribución. OIDC, cifrado local adicional y coordinación entre réplicas no están implementados. No se declara una auditoría de seguridad ni cumplimiento jurídico concluido.
