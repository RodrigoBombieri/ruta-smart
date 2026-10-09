import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:rutasmart/api.dart';
import 'package:rutasmart/main.dart';
import 'package:rutasmart/pages.dart';
import 'package:rutasmart/storage.dart';

void main() {
  late LocalStore store;
  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    store = LocalStore(await SharedPreferences.getInstance());
  });
  RutaApi api() => RutaApi(
    client: MockClient(
      (r) async => http.Response(jsonEncode({'demoAvailable': true}), 200),
    ),
  );
  testWidgets('Planner renders on a small phone and keeps demo explicit', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpWidget(RutaSmartApp(store: store, api: api()));
    await tester.pumpAndSettle();
    expect(find.text('Tu viaje empieza acá.'), findsOneWidget);
    await tester.ensureVisible(find.text('Modo demostración'));
    expect(
      find.text('Recorrido y tiempos sintéticos. No usar para viajar.'),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
  });
  testWidgets('Vehicle can be edited and saved locally', (tester) async {
    await tester.pumpWidget(RutaSmartApp(store: store, api: api()));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Mi vehículo').last);
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextFormField).first, 'Auto familiar');
    await tester.ensureVisible(find.text('Guardar vehículo'));
    await tester.tap(find.text('Guardar vehículo'));
    await tester.pumpAndSettle();
    expect(store.vehicle()['name'], 'Auto familiar');
    expect(tester.takeException(), isNull);
  });
  testWidgets(
    'Offline report preserves null costs and disables navigation for demo',
    (tester) async {
      final input = {
        'origin': {'label': 'Buenos Aires'},
        'destination': {'label': 'Rosario'},
        'departureAt': '2026-10-08T08:00:00-03:00',
      };
      final report = <String, dynamic>{
        'tripId': 'test-trip',
        'version': 1,
        'ruleVersion': 'test',
        'computedAt': '2026-10-07T09:00:00Z',
        'validUntil': '2026-10-07T09:05:00Z',
        'input': input,
        'status': 'insufficient_information',
        'route': {
          'synthetic': true,
          'distanceM': 300000,
          'durationSeconds': 14400,
          'geometry': [
            {'latitude': -34.6, 'longitude': -58.4},
            {'latitude': -32.9, 'longitude': -60.6},
          ],
          'limitations': [],
          'attribution': 'synthetic',
        },
        'timeline': [],
        'coverage': [],
        'fuel': {
          'consumedLitres': 22.5,
          'initialRangeKm': 333,
          'minimumPurchaseLitres': 0,
          'consumptionCostArs': null,
          'minimumPurchaseCostArs': null,
          'planStatus': 'no_refuel_estimated',
        },
      };
      await tester.pumpWidget(
        MaterialApp(
          home: ReportPage(
            report: report,
            store: store,
            api: api(),
            onReplan: (_) {},
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('Información insuficiente'), findsOneWidget);
      expect(
        find.text('Consumo valorizado: sin precio de referencia'),
        findsOneWidget,
      );
      expect(
        tester
            .widget<ButtonStyleButton>(
              find
                  .ancestor(
                    of: find.text('Google Maps'),
                    matching: find.byWidgetPredicate(
                      (w) => w is ButtonStyleButton,
                    ),
                  )
                  .first,
            )
            .onPressed,
        isNull,
      );
      await tester.ensureVisible(find.text('Guardar en este dispositivo'));
      await tester.tap(find.text('Guardar en este dispositivo'));
      await tester.pumpAndSettle();
      expect(store.trips().single['tripId'], 'test-trip');
      expect(tester.takeException(), isNull);
    },
  );
  test('Local trips survive a new store and can be deleted', () async {
    await store.saveTrip({'tripId': 'a', 'value': 1});
    await store.saveTrip({'tripId': 'a', 'value': 2});
    final restored = LocalStore(await SharedPreferences.getInstance());
    expect(restored.trips().length, 1);
    expect(restored.trips().single['value'], 2);
    await restored.deleteTrip('a');
    expect(restored.trips(), isEmpty);
  });
  test(
    'HTTP errors retain useful messages and same input reuses idempotency key',
    () async {
      final keys = <String?>[];
      final client = RutaApi(
        client: MockClient((r) async {
          if (r.url.path.endsWith('/sessions/anonymous')) {
            return http.Response('{"token":"token"}', 201);
          }
          keys.add(r.headers['Idempotency-Key']);
          return http.Response('{"title":"Routing no disponible"}', 503);
        }),
      );
      for (var i = 0; i < 2; i++) {
        await expectLater(
          client.analyze({'demo': false}),
          throwsA(
            isA<ApiException>().having(
              (e) => e.message,
              'message',
              'Routing no disponible',
            ),
          ),
        );
      }
      expect(keys.first, isNotNull);
      expect(keys.first, keys.last);
      client.close();
    },
  );
}
