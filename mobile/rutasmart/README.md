# Cliente RutaSmart

Flutter 3.29.2 / Dart 3.7.2. Web y Android, textos en español.

```sh
flutter pub get
flutter analyze
flutter test
flutter build web --release --dart-define=API_URL=
```

`API_URL` vacío utiliza el mismo origen en web. Android necesita una URL de backend; el emulador accede al host con `http://10.0.2.2:5080` en debug.

[Ejecución completa](../../DEPLOYMENT.md) · [Arquitectura](../../ARCHITECTURE.md) · [Pruebas](../../TESTING.md)
