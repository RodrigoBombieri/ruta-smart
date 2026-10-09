import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'api.dart';

class RouteMap extends StatelessWidget {
  final List<Json> points;
  final bool synthetic;
  final double height;
  final List<Json> alerts;
  final List<Json> stations;
  const RouteMap({
    super.key,
    required this.points,
    required this.synthetic,
    this.height = 320,
    this.alerts = const [],
    this.stations = const [],
  });
  @override
  Widget build(BuildContext context) {
    final coordinates =
        points
            .map(
              (p) => LatLng(
                (p['latitude'] as num).toDouble(),
                (p['longitude'] as num).toDouble(),
              ),
            )
            .toList();
    const tiles = String.fromEnvironment(
      'TILES_URL',
      defaultValue:
          kIsWeb ? 'https://tile.openstreetmap.org/{z}/{x}/{y}.png' : '',
    );
    const attribution = String.fromEnvironment(
      'TILES_ATTRIBUTION',
      defaultValue: kIsWeb ? '© OpenStreetMap contributors · ODbL' : '',
    );
    final showTiles =
        tiles.startsWith('https://') && attribution.isNotEmpty && !synthetic;
    return SizedBox(
      height: height,
      child: Stack(
        children: [
          FlutterMap(
            key: ValueKey('${coordinates.first}-${coordinates.last}'),
            options: MapOptions(
              backgroundColor: const Color(0xffe1e9df),
              initialCameraFit: CameraFit.bounds(
                bounds: LatLngBounds.fromPoints(coordinates),
                padding: const EdgeInsets.all(55),
              ),
              interactionOptions: const InteractionOptions(
                flags:
                    InteractiveFlag.drag |
                    InteractiveFlag.pinchZoom |
                    InteractiveFlag.doubleTapZoom |
                    InteractiveFlag.scrollWheelZoom,
              ),
            ),
            children: [
              if (showTiles)
                TileLayer(
                  urlTemplate: tiles,
                  userAgentPackageName: 'ar.rutasmart.rutasmart',
                ),
              if (!synthetic && alerts.isNotEmpty)
                PolygonLayer(
                  polygons: [
                    for (final match in alerts)
                      for (final ring in match['alert']['polygons'] as List)
                        Polygon(
                          points:
                              (ring as List)
                                  .map(
                                    (p) => LatLng(
                                      (p['latitude'] as num).toDouble(),
                                      (p['longitude'] as num).toDouble(),
                                    ),
                                  )
                                  .toList(),
                          color: const Color(0x33e99432),
                          borderColor: const Color(0xffc57825),
                          borderStrokeWidth: 1,
                        ),
                  ],
                ),
              if (!synthetic && stations.isNotEmpty)
                MarkerLayer(
                  markers: [
                    for (final candidate in stations)
                      Marker(
                        point: LatLng(
                          (candidate['station']['point']['latitude'] as num)
                              .toDouble(),
                          (candidate['station']['point']['longitude'] as num)
                              .toDouble(),
                        ),
                        width: 26,
                        height: 26,
                        child: Tooltip(
                          message:
                              '${candidate['station']['brand']} · Acceso no verificado',
                          child: Container(
                            decoration: BoxDecoration(
                              color: Colors.white,
                              borderRadius: BorderRadius.circular(6),
                            ),
                            child: const Icon(
                              Icons.local_gas_station,
                              size: 20,
                              color: Color(0xff805019),
                            ),
                          ),
                        ),
                      ),
                  ],
                ),
              PolylineLayer(
                polylines: [
                  Polyline(
                    points: coordinates,
                    strokeWidth: 5,
                    color: const Color(0xff267a66),
                    borderStrokeWidth: 3,
                    borderColor: Colors.white,
                  ),
                ],
              ),
              MarkerLayer(
                markers: [
                  for (var i = 0; i < coordinates.length; i++)
                    if (i == 0 || i == coordinates.length - 1)
                      Marker(
                        point: coordinates[i],
                        width: 34,
                        height: 34,
                        child: Container(
                          decoration: BoxDecoration(
                            color:
                                i == 0
                                    ? const Color(0xff126356)
                                    : const Color(0xffc78837),
                            shape: BoxShape.circle,
                            border: Border.all(color: Colors.white, width: 3),
                          ),
                          child: Icon(
                            i == 0 ? Icons.trip_origin : Icons.flag,
                            size: 18,
                            color: Colors.white,
                          ),
                        ),
                      ),
                ],
              ),
            ],
          ),
          Positioned(
            left: 14,
            right: 14,
            top: 14,
            child: Align(
              alignment: Alignment.topLeft,
              child: Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 8,
                ),
                decoration: BoxDecoration(
                  color: Colors.white.withAlpha(235),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  synthetic
                      ? 'ESQUEMA ILUSTRATIVO · NO ES UNA RUTA VIAL'
                      : showTiles
                      ? 'RECORRIDO ANALIZADO'
                      : 'TRAZADO SIN CARTOGRAFÍA BASE',
                  style: const TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.w700,
                    letterSpacing: .6,
                  ),
                ),
              ),
            ),
          ),
          Positioned(
            left: 12,
            right: 12,
            bottom: 10,
            child: Text(
              showTiles
                  ? attribution
                  : 'A  Origen                                        B  Destino\nAcercá y desplazá para explorar el trazado.',
              style: const TextStyle(fontSize: 10, color: Color(0xff3d594b)),
            ),
          ),
        ],
      ),
    );
  }
}
