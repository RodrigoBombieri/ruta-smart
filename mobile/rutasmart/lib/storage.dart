import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import 'api.dart';

class LocalStore {
  final SharedPreferences prefs;
  LocalStore(this.prefs);
  List<Json> trips() {
    try {
      return (jsonDecode(prefs.getString('trips.v1') ?? '[]') as List)
          .cast<Json>();
    } catch (_) {
      return [];
    }
  }

  Future<void> saveTrip(Json trip) async {
    final all = trips()..removeWhere((t) => t['tripId'] == trip['tripId']);
    all.insert(0, trip);
    if (!await prefs.setString('trips.v1', jsonEncode(all.take(50).toList()))) {
      throw ApiException('No se pudo guardar el viaje en el dispositivo.');
    }
  }

  Future<void> deleteTrip(String id) async {
    final all = trips()..removeWhere((t) => t['tripId'] == id);
    if (!await prefs.setString('trips.v1', jsonEncode(all))) {
      throw ApiException('No se pudo borrar el viaje local.');
    }
  }

  Json vehicle() {
    try {
      return jsonDecode(prefs.getString('vehicle.v1') ?? '') as Json;
    } catch (_) {
      return {
        'name': 'Mi auto',
        'fuelType': 'nafta_2',
        'tankLitres': 47,
        'consumptionLPer100Km': 7.5,
      };
    }
  }

  Future<void> saveVehicle(Json v) async {
    if (!await prefs.setString('vehicle.v1', jsonEncode(v))) {
      throw ApiException('No se pudo guardar el vehículo.');
    }
  }
}
