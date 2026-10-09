import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import 'api.dart';
import 'storage.dart';
import 'route_map.dart';
import 'journey_hero.dart';

const green = Color(0xff126356);
const muted = Color(0xff66746b);
String number(dynamic n, [int decimals = 1]) =>
    n == null
        ? 'Sin dato'
        : (n as num).toStringAsFixed(decimals).replaceAll('.', ',');
String date(String iso) {
  final d = DateTime.parse(iso).toUtc().subtract(const Duration(hours: 3));
  String p(int n) => n.toString().padLeft(2, '0');
  return '${p(d.day)}/${p(d.month)} ${p(d.hour)}:${p(d.minute)}';
}

void message(BuildContext context, Object text) => ScaffoldMessenger.of(
  context,
).showSnackBar(SnackBar(content: Text('$text')));
Widget panel(Widget child) =>
    Card(child: Padding(padding: const EdgeInsets.all(24), child: child));
Widget heading(
  BuildContext context,
  String eyebrow,
  String title,
  String subtitle,
) => Padding(
  padding: const EdgeInsets.only(bottom: 24),
  child: Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(
        eyebrow.toUpperCase(),
        style: const TextStyle(
          fontSize: 11,
          fontWeight: FontWeight.w800,
          letterSpacing: 2,
          color: green,
        ),
      ),
      const SizedBox(height: 10),
      Text(title, style: Theme.of(context).textTheme.headlineLarge),
      const SizedBox(height: 10),
      Text(subtitle, style: const TextStyle(color: muted)),
    ],
  ),
);

class HomePage extends StatefulWidget {
  final LocalStore store;
  final RutaApi api;
  const HomePage({super.key, required this.store, required this.api});
  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  int tab = 0;
  int plannerVersion = 0;
  Json? reuse;
  static const names = ['Planificar', 'Mis viajes', 'Mi vehículo', 'Fuentes'];
  static const icons = [
    Icons.route_outlined,
    Icons.bookmark_border,
    Icons.directions_car_outlined,
    Icons.layers_outlined,
  ];
  @override
  void dispose() {
    widget.api.close();
    super.dispose();
  }

  void openReport(Json result) => Navigator.of(context)
      .push(
        MaterialPageRoute<void>(
          builder:
              (_) => ReportPage(
                report: result,
                store: widget.store,
                api: widget.api,
                onReplan: (input) {
                  setState(() {
                    reuse = input;
                    tab = 0;
                    plannerVersion++;
                  });
                },
              ),
        ),
      )
      .then((_) {
        if (mounted) setState(() {});
      });
  @override
  Widget build(BuildContext context) {
    final wide = MediaQuery.sizeOf(context).width >= 1000;
    final content =
        [
          PlannerPage(
            key: ValueKey(plannerVersion),
            store: widget.store,
            api: widget.api,
            initial: reuse,
            onResult: openReport,
          ),
          SavedPage(store: widget.store, onOpen: openReport),
          VehiclePage(store: widget.store),
          SourcesPage(api: widget.api),
        ][tab];
    return Scaffold(
      appBar:
          wide
              ? null
              : AppBar(
                title: const Text(
                  'RutaSmart',
                  style: TextStyle(fontWeight: FontWeight.w800, color: green),
                ),
                actions: const [
                  Padding(
                    padding: EdgeInsets.all(16),
                    child: Text(
                      'ARGENTINA',
                      style: TextStyle(fontSize: 10, letterSpacing: 1.5),
                    ),
                  ),
                ],
              ),
      body: SafeArea(
        child: Row(
          children: [
            if (wide)
              Container(
                width: 232,
                color: const Color(0xfffafbf8),
                padding: const EdgeInsets.all(24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const SizedBox(height: 22),
                    const Icon(Icons.route, color: green, size: 36),
                    const SizedBox(height: 14),
                    const Text(
                      'RutaSmart',
                      style: TextStyle(
                        fontSize: 27,
                        fontWeight: FontWeight.w800,
                        color: green,
                        letterSpacing: -1,
                      ),
                    ),
                    const Text(
                      'A R G E N T I N A',
                      style: TextStyle(fontSize: 10, color: muted),
                    ),
                    const SizedBox(height: 54),
                    for (var i = 0; i < names.length; i++)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 8),
                        child: ListTile(
                          contentPadding: const EdgeInsets.symmetric(
                            horizontal: 12,
                          ),
                          selected: tab == i,
                          selectedTileColor: const Color(0xffe3ede4),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12),
                          ),
                          leading: Icon(icons[i], size: 22),
                          title: Text(names[i]),
                          onTap: () => setState(() => tab = i),
                        ),
                      ),
                    const Spacer(),
                    const Divider(),
                    const SizedBox(height: 14),
                    const Text(
                      'Antes de salir,\nmirá el camino.',
                      style: TextStyle(
                        fontSize: 19,
                        fontWeight: FontWeight.w600,
                        color: green,
                      ),
                    ),
                    const SizedBox(height: 14),
                    const Text(
                      'Desarrollo local\nCobertura en validación',
                      style: TextStyle(color: muted, fontSize: 12),
                    ),
                    const SizedBox(height: 20),
                  ],
                ),
              ),
            Expanded(
              child: SingleChildScrollView(
                padding: EdgeInsets.all(wide ? 40 : 20),
                child: Center(
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 1160),
                    child: content,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar:
          wide
              ? null
              : NavigationBar(
                selectedIndex: tab,
                onDestinationSelected: (i) => setState(() => tab = i),
                destinations: List.generate(
                  names.length,
                  (i) => NavigationDestination(
                    icon: Icon(icons[i]),
                    label: names[i],
                  ),
                ),
              ),
    );
  }
}

class PlannerPage extends StatefulWidget {
  final LocalStore store;
  final RutaApi api;
  final Json? initial;
  final ValueChanged<Json> onResult;
  const PlannerPage({
    super.key,
    required this.store,
    required this.api,
    required this.onResult,
    this.initial,
  });
  @override
  State<PlannerPage> createState() => _PlannerPageState();
}

class _PlannerPageState extends State<PlannerPage> {
  final form = GlobalKey<FormState>();
  late Json origin;
  late Json destination;
  late DateTime departure;
  late TextEditingController initial;
  late TextEditingController reserve;
  late TextEditingController price;
  final pauses = <Json>[];
  bool demo = false, confirmed = false, busy = false;
  String? error;
  @override
  void initState() {
    super.initState();
    final input = widget.initial;
    origin =
        input?['origin'] as Json? ??
        {
          'label': 'Buenos Aires',
          'point': {'latitude': -34.6037, 'longitude': -58.3816},
        };
    destination =
        input?['destination'] as Json? ??
        {
          'label': 'Rosario, Santa Fe',
          'point': {'latitude': -32.9468, 'longitude': -60.6393},
        };
    departure = DateTime.now()
        .toUtc()
        .subtract(const Duration(hours: 3))
        .add(const Duration(days: 1));
    departure = DateTime(departure.year, departure.month, departure.day, 8);
    initial = TextEditingController(text: '${input?['initialLitres'] ?? 30}');
    reserve = TextEditingController(text: '${input?['reserveLitres'] ?? 5}');
    price = TextEditingController(
      text: '${input?['manualPriceArsPerLitre'] ?? ''}',
    );
    demo = input?['demo'] as bool? ?? false;
    if (input?['pauses'] is List) {
      pauses.addAll((input!['pauses'] as List).cast<Json>());
    }
    widget.api
        .status()
        .then((s) {
          if (mounted && s['demoAvailable'] == false) {
            setState(() => demo = false);
          }
        })
        .catchError((Object _) {});
  }

  @override
  void dispose() {
    initial.dispose();
    reserve.dispose();
    price.dispose();
    super.dispose();
  }

  Future<void> choose(bool from) async {
    final result = await showDialog<Json>(
      context: context,
      builder:
          (_) => LocationDialog(
            api: widget.api,
            initial: from ? origin : destination,
          ),
    );
    if (result != null && mounted) {
      setState(() {
        if (from) {
          origin = result;
        } else {
          destination = result;
        }
        confirmed = false;
      });
    }
  }

  Future<void> chooseDate() async {
    final now = DateTime.now().toUtc().subtract(const Duration(hours: 3));
    final d = await showDatePicker(
      context: context,
      initialDate: departure,
      firstDate: DateTime(now.year, now.month, now.day),
      lastDate: now.add(const Duration(days: 30)),
    );
    if (d == null || !mounted) return;
    final t = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(departure),
    );
    if (t != null && mounted) {
      setState(
        () => departure = DateTime(d.year, d.month, d.day, t.hour, t.minute),
      );
    }
  }

  Future<void> analyze() async {
    if (!form.currentState!.validate()) return;
    if (!confirmed) {
      setState(() => error = 'Confirmá los puntos de origen y destino.');
      return;
    }
    setState(() {
      busy = true;
      error = null;
    });
    try {
      final input = <String, dynamic>{
        'origin': origin,
        'destination': destination,
        'departureAt': '${departure.toIso8601String()}-03:00',
        'timeZone': 'America/Argentina/Buenos_Aires',
        'vehicle': widget.store.vehicle(),
        'initialLitres': parse(initial.text),
        'reserveLitres': parse(reserve.text),
        'manualPriceArsPerLitre':
            price.text.trim().isEmpty ? null : parse(price.text),
        'pauses': pauses,
        'demo': demo,
      };
      final result = await widget.api.analyze(input);
      if (mounted) widget.onResult(result);
    } catch (e) {
      if (mounted) setState(() => error = '$e');
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final vehicle = widget.store.vehicle();
    final left = Form(
      key: form,
      child: panel(
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Tu próximo recorrido',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 24),
            locationTile(
              'ORIGEN',
              origin,
              Icons.trip_origin,
              () => choose(true),
            ),
            Align(
              alignment: Alignment.centerRight,
              child: IconButton(
                tooltip: 'Intercambiar origen y destino',
                onPressed:
                    busy
                        ? null
                        : () => setState(() {
                          final old = origin;
                          origin = destination;
                          destination = old;
                          confirmed = false;
                        }),
                icon: const Icon(Icons.swap_vert),
              ),
            ),
            locationTile(
              'DESTINO',
              destination,
              Icons.location_on_outlined,
              () => choose(false),
            ),
            const SizedBox(height: 20),
            OutlinedButton.icon(
              onPressed: busy ? null : chooseDate,
              icon: const Icon(Icons.calendar_today_outlined, size: 18),
              label: Text(
                'Salida: ${departure.day}/${departure.month}/${departure.year} · ${departure.hour.toString().padLeft(2, '0')}:${departure.minute.toString().padLeft(2, '0')} (UTC−3)',
              ),
            ),
            const SizedBox(height: 20),
            Text(
              '${vehicle['name']} · ${number(vehicle['consumptionLPer100Km'])} L/100 km',
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 14),
            Wrap(
              spacing: 12,
              runSpacing: 12,
              children: [
                SizedBox(
                  width: 180,
                  child: numeric(
                    initial,
                    'Combustible inicial (L)',
                    max: (vehicle['tankLitres'] as num).toDouble(),
                  ),
                ),
                SizedBox(
                  width: 180,
                  child: numeric(
                    reserve,
                    'Reserva (L)',
                    max: (vehicle['tankLitres'] as num).toDouble() - .01,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 14),
            numeric(
              price,
              'Precio manual opcional (ARS/L)',
              optional: true,
              min: .01,
              max: 1000000,
            ),
            const SizedBox(height: 8),
            const Text(
              'Se usa solo como referencia para estimar costos.',
              style: TextStyle(fontSize: 12, color: muted),
            ),
            const SizedBox(height: 16),
            for (var i = 0; i < pauses.length; i++)
              ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.coffee_outlined),
                title: Text(
                  'Km ${number((pauses[i]['distanceM'] as num) / 1000, 0)} · ${number((pauses[i]['durationSeconds'] as num) / 60, 0)} min',
                ),
                trailing: IconButton(
                  tooltip: 'Quitar pausa',
                  onPressed:
                      busy ? null : () => setState(() => pauses.removeAt(i)),
                  icon: const Icon(Icons.close),
                ),
              ),
            TextButton.icon(
              onPressed:
                  busy || pauses.length >= 20
                      ? null
                      : () async {
                        final pause = await showDialog<Json>(
                          context: context,
                          builder: (_) => const PauseDialog(),
                        );
                        if (pause != null && mounted) {
                          setState(() => pauses.add(pause));
                        }
                      },
              icon: const Icon(Icons.add),
              label: const Text('Agregar pausa al recorrido'),
            ),
            const Divider(height: 30),
            SwitchListTile(
              contentPadding: EdgeInsets.zero,
              value: demo,
              onChanged: busy ? null : (v) => setState(() => demo = v),
              title: const Text('Modo demostración'),
              subtitle: const Text(
                'Recorrido y tiempos sintéticos. No usar para viajar.',
                style: TextStyle(fontSize: 12),
              ),
            ),
            CheckboxListTile(
              contentPadding: EdgeInsets.zero,
              controlAffinity: ListTileControlAffinity.leading,
              value: confirmed,
              onChanged:
                  busy ? null : (v) => setState(() => confirmed = v ?? false),
              title: const Text(
                'Confirmé los puntos del viaje',
                style: TextStyle(fontSize: 14),
              ),
            ),
            if (error != null)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 12),
                child: Text(
                  error!,
                  style: TextStyle(color: Theme.of(context).colorScheme.error),
                ),
              ),
            const SizedBox(height: 12),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: busy ? null : analyze,
                icon:
                    busy
                        ? const SizedBox(
                          width: 18,
                          height: 18,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                        : const Icon(Icons.arrow_forward),
                label: Text(
                  busy ? 'Analizando tu recorrido…' : 'Analizar viaje',
                ),
              ),
            ),
          ],
        ),
      ),
    );
    final right = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const JourneyHero(),
        const SizedBox(height: 20),
        panel(
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Icon(Icons.explore_outlined, color: green, size: 28),
              const SizedBox(height: 14),
              Text(
                'Más contexto.\nMejores decisiones.',
                style: Theme.of(context).textTheme.headlineMedium,
              ),
              const SizedBox(height: 20),
              const InfoRow(
                Icons.schedule,
                'El viaje, en su horario',
                'Tiempos de paso estimados, incluyendo tus pausas.',
              ),
              const InfoRow(
                Icons.local_gas_station_outlined,
                'Combustible con tus datos',
                'Consumo, autonomía y costo de referencia opcional.',
              ),
              const InfoRow(
                Icons.visibility_outlined,
                'Lo que sabemos, a la vista',
                'Cada informe muestra las fuentes que faltan.',
              ),
            ],
          ),
        ),
        const SizedBox(height: 18),
        const Text(
          'Alertas SMN con cobertura parcial. Estado vial aún no habilitado. El routing público de prueba cubre Buenos Aires–Rosario y alrededores; una ruta calculada no implica condiciones favorables.',
          style: TextStyle(color: muted, fontSize: 12),
        ),
      ],
    );
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        heading(
          context,
          'Un poco de previsión, mucho camino',
          'Tu viaje empieza acá.',
          'Planificá el recorrido y conocé qué información tenés antes de salir.',
        ),
        LayoutBuilder(
          builder:
              (_, c) =>
                  c.maxWidth > 790
                      ? Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(flex: 6, child: left),
                          const SizedBox(width: 24),
                          Expanded(flex: 5, child: right),
                        ],
                      )
                      : Column(
                        children: [left, const SizedBox(height: 24), right],
                      ),
        ),
      ],
    );
  }

  Widget locationTile(
    String caption,
    Json location,
    IconData icon,
    VoidCallback callback,
  ) => InkWell(
    onTap: busy ? null : callback,
    borderRadius: BorderRadius.circular(12),
    child: Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        border: Border.all(color: const Color(0xffd9e1d8)),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          Icon(icon, color: green),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  caption,
                  style: const TextStyle(
                    fontSize: 10,
                    letterSpacing: 1.5,
                    color: muted,
                  ),
                ),
                Text(
                  location['label'] as String,
                  style: const TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                Text(
                  '${number(location['point']['latitude'], 4)}, ${number(location['point']['longitude'], 4)}',
                  style: const TextStyle(fontSize: 11, color: muted),
                ),
              ],
            ),
          ),
          const Icon(Icons.edit_outlined, size: 18, color: muted),
        ],
      ),
    ),
  );
}

double? parse(String text) => double.tryParse(text.trim().replaceAll(',', '.'));
Widget numeric(
  TextEditingController controller,
  String label, {
  bool optional = false,
  double min = 0,
  double max = 10000,
}) => TextFormField(
  controller: controller,
  decoration: InputDecoration(labelText: label),
  keyboardType: const TextInputType.numberWithOptions(decimal: true),
  validator: (text) {
    if (optional && (text ?? '').trim().isEmpty) return null;
    final value = parse(text ?? '');
    return value == null || !value.isFinite || value < min || value > max
        ? 'Valor entre $min y $max'
        : null;
  },
);

class InfoRow extends StatelessWidget {
  final IconData icon;
  final String title, description;
  const InfoRow(this.icon, this.title, this.description, {super.key});
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 18),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, color: green, size: 21),
        const SizedBox(width: 14),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: const TextStyle(fontWeight: FontWeight.w600)),
              const SizedBox(height: 4),
              Text(
                description,
                style: const TextStyle(color: muted, fontSize: 13),
              ),
            ],
          ),
        ),
      ],
    ),
  );
}

class LocationDialog extends StatefulWidget {
  final RutaApi api;
  final Json initial;
  const LocationDialog({super.key, required this.api, required this.initial});
  @override
  State<LocationDialog> createState() => _LocationDialogState();
}

class _LocationDialogState extends State<LocationDialog> {
  late final TextEditingController name, lat, lon;
  final form = GlobalKey<FormState>();
  List<Json> results = [];
  bool busy = false;
  String? error;
  String? georefId;
  @override
  void initState() {
    super.initState();
    name = TextEditingController(text: widget.initial['label'] as String);
    lat = TextEditingController(text: '${widget.initial['point']['latitude']}');
    lon = TextEditingController(
      text: '${widget.initial['point']['longitude']}',
    );
  }

  @override
  void dispose() {
    name.dispose();
    lat.dispose();
    lon.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AlertDialog(
    title: const Text('Confirmar un punto'),
    content: SizedBox(
      width: 470,
      child: SingleChildScrollView(
        child: Form(
          key: form,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextFormField(
                controller: name,
                maxLength: 120,
                decoration: const InputDecoration(
                  labelText: 'Localidad o nombre del punto',
                ),
                validator:
                    (v) =>
                        (v ?? '').trim().isEmpty ? 'Ingresá un nombre' : null,
              ),
              Align(
                alignment: Alignment.centerRight,
                child: TextButton.icon(
                  onPressed:
                      busy
                          ? null
                          : () async {
                            setState(() {
                              busy = true;
                              error = null;
                            });
                            try {
                              final list = await widget.api.search(name.text);
                              if (mounted) {
                                setState(() {
                                  results = list;
                                  if (list.isEmpty) {
                                    error =
                                        'No encontramos localidades. Ajustá la búsqueda o ingresá coordenadas.';
                                  }
                                });
                              }
                            } catch (e) {
                              if (mounted) setState(() => error = '$e');
                            } finally {
                              if (mounted) setState(() => busy = false);
                            }
                          },
                  icon: const Icon(Icons.search),
                  label: Text(busy ? 'Buscando…' : 'Buscar en GeoRef'),
                ),
              ),
              if (error != null)
                Text(
                  error!,
                  style: TextStyle(color: Theme.of(context).colorScheme.error),
                ),
              for (final r in results)
                ListTile(
                  title: Text(r['label'] as String),
                  subtitle: const Text(
                    'Centroide de localidad; verificá el punto exacto',
                  ),
                  onTap:
                      () => setState(() {
                        name.text = r['label'] as String;
                        lat.text = '${r['point']['latitude']}';
                        lon.text = '${r['point']['longitude']}';
                        georefId = r['georefId'] as String?;
                        results = [];
                      }),
                ),
              const SizedBox(height: 14),
              numeric(lat, 'Latitud', min: -55.2, max: -21.7),
              const SizedBox(height: 14),
              numeric(lon, 'Longitud', min: -73.6, max: -53.5),
              const SizedBox(height: 14),
              const Text(
                'GeoRef devuelve el centro de la localidad. Podés corregir las coordenadas antes de confirmar.',
                style: TextStyle(fontSize: 12, color: muted),
              ),
            ],
          ),
        ),
      ),
    ),
    actions: [
      TextButton(
        onPressed: () => Navigator.pop(context),
        child: const Text('Cancelar'),
      ),
      FilledButton(
        onPressed: () {
          if (form.currentState!.validate()) {
            Navigator.pop(context, <String, dynamic>{
              'label': name.text.trim(),
              'point': {
                'latitude': parse(lat.text),
                'longitude': parse(lon.text),
              },
              'georefId': georefId,
            });
          }
        },
        child: const Text('Confirmar punto'),
      ),
    ],
  );
}

class PauseDialog extends StatefulWidget {
  const PauseDialog({super.key});
  @override
  State<PauseDialog> createState() => _PauseDialogState();
}

class _PauseDialogState extends State<PauseDialog> {
  final km = TextEditingController();
  final minutes = TextEditingController(text: '20');
  final form = GlobalKey<FormState>();
  @override
  void dispose() {
    km.dispose();
    minutes.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AlertDialog(
    title: const Text('Agregar pausa'),
    content: Form(
      key: form,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          numeric(km, 'Km desde el origen', max: 8000),
          const SizedBox(height: 16),
          numeric(minutes, 'Duración (minutos)', min: 1, max: 1440),
        ],
      ),
    ),
    actions: [
      TextButton(
        onPressed: () => Navigator.pop(context),
        child: const Text('Cancelar'),
      ),
      FilledButton(
        onPressed: () {
          if (form.currentState!.validate()) {
            Navigator.pop(context, <String, dynamic>{
              'distanceM': parse(km.text)! * 1000,
              'durationSeconds': (parse(minutes.text)! * 60).round(),
            });
          }
        },
        child: const Text('Agregar'),
      ),
    ],
  );
}

class VehiclePage extends StatefulWidget {
  final LocalStore store;
  const VehiclePage({super.key, required this.store});
  @override
  State<VehiclePage> createState() => _VehiclePageState();
}

class _VehiclePageState extends State<VehiclePage> {
  final form = GlobalKey<FormState>();
  late final TextEditingController name, tank, consumption;
  late String fuel;
  @override
  void initState() {
    super.initState();
    final v = widget.store.vehicle();
    name = TextEditingController(text: v['name'] as String);
    tank = TextEditingController(text: '${v['tankLitres']}');
    consumption = TextEditingController(text: '${v['consumptionLPer100Km']}');
    fuel = v['fuelType'] as String;
  }

  @override
  void dispose() {
    name.dispose();
    tank.dispose();
    consumption.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      heading(
        context,
        'Tu punto de partida',
        'Mi vehículo',
        'Usamos tus valores para estimar consumo y autonomía. Se guardan solo en este dispositivo.',
      ),
      panel(
        Form(
          key: form,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Icon(Icons.directions_car_outlined, size: 48, color: green),
              const SizedBox(height: 24),
              TextFormField(
                controller: name,
                maxLength: 80,
                decoration: const InputDecoration(
                  labelText: 'Nombre del vehículo',
                ),
                validator:
                    (v) =>
                        (v ?? '').trim().isEmpty ? 'Ingresá un nombre' : null,
              ),
              const SizedBox(height: 16),
              DropdownButtonFormField<String>(
                value: fuel,
                decoration: const InputDecoration(labelText: 'Combustible'),
                items: const [
                  DropdownMenuItem(
                    value: 'nafta_2',
                    child: Text('Nafta grado 2'),
                  ),
                  DropdownMenuItem(
                    value: 'nafta_3',
                    child: Text('Nafta grado 3'),
                  ),
                  DropdownMenuItem(
                    value: 'diesel_2',
                    child: Text('Diésel grado 2'),
                  ),
                  DropdownMenuItem(
                    value: 'diesel_3',
                    child: Text('Diésel grado 3'),
                  ),
                ],
                onChanged: (v) => setState(() => fuel = v!),
              ),
              const SizedBox(height: 20),
              numeric(tank, 'Capacidad del tanque (L)', min: 1, max: 200),
              const SizedBox(height: 20),
              numeric(
                consumption,
                'Consumo estimado (L/100 km)',
                min: .1,
                max: 60,
              ),
              const SizedBox(height: 24),
              const Text(
                'El combustible inicial y la reserva se eligen en cada viaje. Carga, velocidad, viento y relieve pueden modificar el consumo.',
                style: TextStyle(color: muted),
              ),
              const SizedBox(height: 24),
              FilledButton.icon(
                onPressed: () async {
                  if (!form.currentState!.validate()) return;
                  try {
                    await widget.store.saveVehicle({
                      'name': name.text.trim(),
                      'fuelType': fuel,
                      'tankLitres': parse(tank.text),
                      'consumptionLPer100Km': parse(consumption.text),
                    });
                    if (context.mounted) {
                      message(
                        context,
                        'Vehículo guardado en este dispositivo.',
                      );
                    }
                  } catch (e) {
                    if (context.mounted) message(context, e);
                  }
                },
                icon: const Icon(Icons.check),
                label: const Text('Guardar vehículo'),
              ),
            ],
          ),
        ),
      ),
    ],
  );
}

class SavedPage extends StatefulWidget {
  final LocalStore store;
  final ValueChanged<Json> onOpen;
  const SavedPage({super.key, required this.store, required this.onOpen});
  @override
  State<SavedPage> createState() => _SavedPageState();
}

class _SavedPageState extends State<SavedPage> {
  @override
  Widget build(BuildContext context) {
    final trips = widget.store.trips();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        heading(
          context,
          'Tus planes, a mano',
          'Mis viajes',
          'Informes guardados en este dispositivo. Consultarlos no actualiza las condiciones.',
        ),
        if (trips.isEmpty)
          panel(
            const InfoRow(
              Icons.bookmark_border,
              'Todavía no guardaste viajes',
              'Analizá un recorrido y elegí “Guardar en este dispositivo”. Podrás consultar el informe sin conexión.',
            ),
          ),
        for (final trip in trips)
          Padding(
            padding: const EdgeInsets.only(bottom: 16),
            child: panel(
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    '${trip['input']['origin']['label']} → ${trip['input']['destination']['label']}',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Analizado: ${date(trip['computedAt'] as String)} · ${number((trip['route']['distanceM'] as num) / 1000, 0)} km',
                  ),
                  Text(
                    trip['route']['synthetic'] == true
                        ? 'DEMOSTRACIÓN · Datos sintéticos'
                        : 'Informe fechado · Verificá su vigencia',
                    style: const TextStyle(color: muted, fontSize: 12),
                  ),
                  const SizedBox(height: 14),
                  Wrap(
                    spacing: 12,
                    children: [
                      OutlinedButton(
                        onPressed: () => widget.onOpen(trip),
                        child: const Text('Ver informe'),
                      ),
                      TextButton.icon(
                        onPressed: () async {
                          try {
                            await widget.store.deleteTrip(
                              trip['tripId'] as String,
                            );
                            if (mounted) setState(() {});
                          } catch (e) {
                            if (context.mounted) message(context, e);
                          }
                        },
                        icon: const Icon(Icons.delete_outline),
                        label: const Text('Borrar copia local'),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        const SizedBox(height: 16),
        const Text(
          'Máximo 50 informes locales. Los datos temporales del servidor vencen a las 24 horas. Borrar una copia local no elimina automáticamente el informe temporal del servidor.',
          style: TextStyle(color: muted, fontSize: 12),
        ),
      ],
    );
  }
}

class SourcesPage extends StatefulWidget {
  final RutaApi api;
  const SourcesPage({super.key, required this.api});
  @override
  State<SourcesPage> createState() => _SourcesPageState();
}

class _SourcesPageState extends State<SourcesPage> {
  late Future<Json> status;
  @override
  void initState() {
    super.initState();
    status = widget.api.status();
  }

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      heading(
        context,
        'La información también tiene límites',
        'Fuentes y cobertura',
        'La ausencia de avisos no significa que el camino esté libre de riesgos.',
      ),
      panel(
        FutureBuilder<Json>(
          future: status,
          builder:
              (_, s) => Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (s.connectionState == ConnectionState.waiting)
                    const LinearProgressIndicator(),
                  if (s.hasError)
                    Text(
                      '${s.error}',
                      style: TextStyle(
                        color: Theme.of(context).colorScheme.error,
                      ),
                    ),
                  InfoRow(
                    Icons.route,
                    'Routing',
                    s.data?['routing'] == 'configured_pending_benchmark'
                        ? 'Configurado. Benchmark de cobertura pendiente.'
                        : s.data?['routing'] == 'osrm_public_pilot'
                        ? 'OSRM público, sin cuenta. Corredor piloto Buenos Aires–Rosario. Solo pruebas no comerciales; sin garantía de disponibilidad.'
                        : 'Requiere proveedor y área aprobados. Demostración sintética disponible en desarrollo.',
                  ),
                  InfoRow(
                    Icons.thunderstorm_outlined,
                    'Alertas meteorológicas · SMN',
                    '${s.data?['weatherDetail'] ?? 'Consultando el canal público CAP…'}\nAvisos activos descargados: ${s.data?['weatherRecords'] ?? '—'}. Última consulta: ${s.data?['weatherLastSuccessAt'] == null ? 'sin descarga válida' : date(s.data!['weatherLastSuccessAt'] as String)}.',
                  ),
                  const InfoRow(
                    Icons.construction,
                    'Estado vial · Vialidad',
                    'No habilitado. No se informa ausencia de cortes.',
                  ),
                  InfoRow(
                    Icons.local_gas_station_outlined,
                    'Estaciones y precios',
                    '${s.data?['stationsDetail'] ?? 'Consultando catálogo público de Energía…'}\nEstaciones descargadas: ${s.data?['stationsCount'] ?? '—'}.',
                  ),
                  const InfoRow(
                    Icons.place_outlined,
                    'Localidades · GeoRef',
                    'Búsqueda de localidades. El centroide no reemplaza la confirmación del punto exacto.',
                  ),
                  OutlinedButton.icon(
                    onPressed:
                        () => setState(() => status = widget.api.status()),
                    icon: const Icon(Icons.refresh),
                    label: const Text('Consultar estado'),
                  ),
                ],
              ),
        ),
      ),
      const SizedBox(height: 24),
      panel(
        const InfoRow(
          Icons.privacy_tip_outlined,
          'Tu privacidad',
          'Sin seguimiento continuo ni cuenta obligatoria. Los viajes temporales del servidor vencen a las 24 horas. El guardado local es voluntario y se puede borrar. Las consultas de localidades y rutas se comparten con sus proveedores.',
        ),
      ),
    ],
  );
}

class ReportPage extends StatefulWidget {
  final Json report;
  final LocalStore store;
  final RutaApi api;
  final ValueChanged<Json> onReplan;
  const ReportPage({
    super.key,
    required this.report,
    required this.store,
    required this.api,
    required this.onReplan,
  });
  @override
  State<ReportPage> createState() => _ReportPageState();
}

class _ReportPageState extends State<ReportPage> {
  bool saved = false;
  @override
  Widget build(BuildContext context) {
    final r = widget.report;
    final input = r['input'] as Json;
    final route = r['route'] as Json;
    final fuel = r['fuel'] as Json;
    final synthetic = route['synthetic'] == true;
    final stale = DateTime.parse(
      r['validUntil'] as String,
    ).isBefore(DateTime.now());
    final timeline = (r['timeline'] as List).cast<Json>();
    final status =
        {
          'adverse': 'Condiciones adversas',
          'caution': 'Precaución',
          'favorable': 'Condiciones favorables',
          'insufficient_information': 'Información insuficiente',
        }[r['status']] ??
        'Información insuficiente';
    return Scaffold(
      appBar: AppBar(title: const Text('Informe del viaje')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 1100),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                heading(
                  context,
                  synthetic
                      ? 'Demostración · No usar para viajar'
                      : 'Tu recorrido, con contexto',
                  '${input['origin']['label']} → ${input['destination']['label']}',
                  'Salida ${date(input['departureAt'] as String)} · Horarios de Argentina (UTC−3)',
                ),
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(24),
                  decoration: BoxDecoration(
                    color: const Color(0xffffedcf),
                    borderRadius: BorderRadius.circular(18),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          const Icon(
                            Icons.info_outline,
                            color: Color(0xff805019),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Text(
                              status,
                              style: const TextStyle(
                                fontSize: 22,
                                fontWeight: FontWeight.w700,
                                color: Color(0xff805019),
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 10),
                      const Text(
                        'Revisá los avisos y la cobertura de cada fuente. El estado vial no está integrado y la cobertura meteorológica es parcial; este informe no garantiza condiciones seguras.',
                      ),
                      if (stale)
                        const Padding(
                          padding: EdgeInsets.only(top: 10),
                          child: Text(
                            'Informe vencido. Volvé a planificar para obtener una evaluación actualizada.',
                            style: TextStyle(fontWeight: FontWeight.w700),
                          ),
                        ),
                    ],
                  ),
                ),
                const SizedBox(height: 24),
                LayoutBuilder(
                  builder:
                      (context, constraints) => Wrap(
                        spacing: 12,
                        runSpacing: 12,
                        children: [
                          stat(
                            'Distancia estimada',
                            '${number((route['distanceM'] as num) / 1000, 0)} km',
                            constraints.maxWidth,
                          ),
                          stat(
                            'Conducción estimada',
                            '${number((route['durationSeconds'] as num) / 3600)} h',
                            constraints.maxWidth,
                          ),
                          stat(
                            'Consumo estimado',
                            '${number(fuel['consumedLitres'])} L',
                            constraints.maxWidth,
                          ),
                          stat(
                            'Autonomía inicial útil',
                            '${number(fuel['initialRangeKm'], 0)} km',
                            constraints.maxWidth,
                          ),
                        ],
                      ),
                ),
                const SizedBox(height: 24),
                ClipRRect(
                  borderRadius: BorderRadius.circular(20),
                  child: RouteMap(
                    points: (route['geometry'] as List).cast<Json>(),
                    synthetic: synthetic,
                    height: 360,
                    alerts: (r['weatherAlerts'] as List? ?? []).cast<Json>(),
                    stations: (r['stations'] as List? ?? []).cast<Json>(),
                  ),
                ),
                const SizedBox(height: 24),
                panel(
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Combustible para el recorrido',
                        style: Theme.of(context).textTheme.titleLarge,
                      ),
                      const SizedBox(height: 18),
                      Text(
                        'Compras mínimas teóricas: ${number(fuel['minimumPurchaseLitres'])} L',
                        style: const TextStyle(
                          fontSize: 20,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        'Consumo valorizado: ${fuel['consumptionCostArs'] == null ? 'sin precio de referencia' : 'ARS ${number(fuel['consumptionCostArs'], 0)}'}',
                      ),
                      Text(
                        'Compras teóricas valorizadas: ${fuel['minimumPurchaseCostArs'] == null ? 'sin precio de referencia' : 'ARS ${number(fuel['minimumPurchaseCostArs'], 0)}'}',
                      ),
                      const SizedBox(height: 12),
                      Text(
                        fuel['planStatus'] == 'stations_unavailable'
                            ? 'Necesitás cargar en el recorrido. Las estaciones candidatas no constituyen un plan de carga: falta verificar acceso vial y disponibilidad.'
                            : 'Con los valores ingresados, el combustible inicial alcanzaría conservando la reserva.',
                      ),
                      const SizedBox(height: 12),
                      const Text(
                        'Estimación sin desvíos. El consumo y la compra son conceptos distintos. Un precio manual no acredita el precio ni el stock de una estación.',
                        style: TextStyle(color: muted, fontSize: 12),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 24),
                panel(
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      if ((r['stations'] as List? ?? []).isNotEmpty) ...[
                        Text(
                          'Estaciones candidatas',
                          style: Theme.of(context).textTheme.titleLarge,
                        ),
                        const SizedBox(height: 8),
                        const Text(
                          'Hasta 20 estaciones a menos de 5 km del trazado. Distancia lateral, no desvío por carretera. Confirmá acceso, horario y stock.',
                          style: TextStyle(color: muted, fontSize: 12),
                        ),
                        for (final candidate in r['stations'] as List)
                          ExpansionTile(
                            tilePadding: EdgeInsets.zero,
                            leading: const Icon(
                              Icons.local_gas_station_outlined,
                              color: green,
                            ),
                            title: Text(
                              '${candidate['station']['brand']} · Km ${number((candidate['distanceAlongM'] as num) / 1000, 0)}',
                            ),
                            subtitle: Text(
                              '${candidate['station']['name']} · ${number((candidate['lateralDistanceM'] as num) / 1000)} km laterales',
                            ),
                            children: [
                              Padding(
                                padding: const EdgeInsets.only(bottom: 16),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      candidate['station']['address'] as String,
                                    ),
                                    for (final price
                                        in candidate['station']['prices']
                                            as List)
                                      Text(
                                        '${price['schedule']}: ARS ${number(price['amountArsPerLitre'], 0)}/L · ${date(price['declaredAt'] as String)}${DateTime.parse(price['declaredAt'] as String).isBefore(DateTime.now().subtract(const Duration(days: 7))) ? ' · ANTIGUO, no usar para comparar' : ' · Precio declarado, no garantizado'}',
                                        style: const TextStyle(fontSize: 12),
                                      ),
                                    const Text(
                                      'Fuente: Secretaría de Energía · Acceso vial no verificado',
                                      style: TextStyle(
                                        fontSize: 12,
                                        color: muted,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        const Divider(height: 32),
                      ],
                      Text(
                        'Tiempos de paso',
                        style: Theme.of(context).textTheme.titleLarge,
                      ),
                      const SizedBox(height: 8),
                      const Text(
                        'Interpolación proporcional a la distancia, de baja confianza. La ventana es una sensibilidad de cálculo; no una garantía.',
                        style: TextStyle(color: muted, fontSize: 12),
                      ),
                      const SizedBox(height: 16),
                      for (var i = 0; i < timeline.length; i++)
                        if (i == 0 ||
                            i == timeline.length - 1 ||
                            i % ((timeline.length / 6).ceil().clamp(1, 100)) ==
                                0)
                          ListTile(
                            contentPadding: EdgeInsets.zero,
                            leading: Icon(
                              i == timeline.length - 1
                                  ? Icons.flag_outlined
                                  : Icons.circle_outlined,
                              color: green,
                              size: 20,
                            ),
                            title: Text(
                              'Km ${number((timeline[i]['distanceM'] as num) / 1000, 0)} · ${date(timeline[i]['at'] as String)}',
                            ),
                            subtitle: Text(
                              'Ventana: ${date(timeline[i]['earliest'] as String)} — ${date(timeline[i]['latest'] as String)}',
                            ),
                          ),
                    ],
                  ),
                ),
                const SizedBox(height: 24),
                panel(
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      for (final match
                          in (r['weatherAlerts'] as List? ?? [])) ...[
                        InfoRow(
                          Icons.thunderstorm_outlined,
                          'SMN · ${match['alert']['event']}',
                          '${match['alert']['description']}\nKm ${number((match['distanceFromM'] as num) / 1000, 0)} a ${number((match['distanceToM'] as num) / 1000, 0)} · ${match['confidence'] == 'possible_temporal_overlap' ? 'Coincidencia posible en la ventana horaria' : 'Coincidencia con el paso estimado'}\nVigencia: ${date(match['alert']['from'] as String)} — ${match['alert']['to'] == null ? 'fin no indicado' : date(match['alert']['to'] as String)}',
                        ),
                        Text(
                          'Recomendaciones del SMN: ${match['alert']['instruction']}',
                          style: const TextStyle(fontSize: 13),
                        ),
                        TextButton(
                          onPressed: () async {
                            final uri = Uri.tryParse(
                              match['alert']['url'] as String,
                            );
                            if (uri != null &&
                                uri.scheme == 'https' &&
                                uri.host == 'ssl.smn.gob.ar') {
                              try {
                                await launchUrl(
                                  uri,
                                  mode: LaunchMode.externalApplication,
                                );
                              } catch (_) {}
                            }
                          },
                          child: const Text('Ver aviso original · SMN'),
                        ),
                        const Divider(height: 28),
                      ],
                      Text(
                        'Cobertura del informe',
                        style: Theme.of(context).textTheme.titleLarge,
                      ),
                      const SizedBox(height: 20),
                      for (final c in r['coverage'] as List)
                        InfoRow(
                          Icons.info_outline,
                          {
                                'weather': 'Meteorología',
                                'roads': 'Estado vial',
                                'stations': 'Estaciones',
                                'prices': 'Precios',
                              }[c['dimension']] ??
                              '${c['dimension']}',
                          c['message'] as String,
                        ),
                      for (final limitation in route['limitations'] as List)
                        Padding(
                          padding: const EdgeInsets.only(bottom: 8),
                          child: Text(
                            '• $limitation',
                            style: const TextStyle(color: muted, fontSize: 12),
                          ),
                        ),
                      Text(
                        'Fuente de ruta: ${route['attribution']}\nAnalizado ${date(r['computedAt'] as String)} · Versión ${r['version']} · Reglas ${r['ruleVersion']}',
                        style: const TextStyle(color: muted, fontSize: 12),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 24),
                Wrap(
                  spacing: 12,
                  runSpacing: 12,
                  children: [
                    FilledButton.icon(
                      onPressed: () async {
                        try {
                          await widget.store.saveTrip(r);
                          if (context.mounted) {
                            setState(() => saved = true);
                            message(
                              context,
                              'Informe guardado en este dispositivo.',
                            );
                          }
                        } catch (e) {
                          if (context.mounted) message(context, e);
                        }
                      },
                      icon: Icon(
                        saved ? Icons.bookmark : Icons.bookmark_border,
                      ),
                      label: Text(
                        saved
                            ? 'Guardado en este dispositivo'
                            : 'Guardar en este dispositivo',
                      ),
                    ),
                    OutlinedButton.icon(
                      onPressed: () {
                        widget.onReplan(input);
                        Navigator.pop(context);
                      },
                      icon: const Icon(Icons.refresh),
                      label: const Text('Volver a planificar'),
                    ),
                    OutlinedButton.icon(
                      onPressed:
                          synthetic ? null : () => navigate('google', input),
                      icon: const Icon(Icons.navigation_outlined),
                      label: const Text('Google Maps'),
                    ),
                    OutlinedButton(
                      onPressed:
                          synthetic ? null : () => navigate('waze', input),
                      child: const Text('Waze'),
                    ),
                    TextButton(
                      onPressed: () async {
                        try {
                          await widget.api.delete(r['tripId'] as String);
                          if (context.mounted) {
                            message(
                              context,
                              'Informe temporal eliminado del servidor.',
                            );
                          }
                        } catch (e) {
                          if (context.mounted) message(context, e);
                        }
                      },
                      child: const Text('Borrar informe del servidor'),
                    ),
                  ],
                ),
                const SizedBox(height: 18),
                const Text(
                  'El navegador puede elegir otra ruta. Este informe no se actualiza durante el viaje. Consultá la app detenido o con ayuda de un acompañante.',
                  style: TextStyle(color: muted, fontSize: 12),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget stat(String label, String value, double availableWidth) => SizedBox(
    width:
        availableWidth >= 700
            ? (availableWidth - 36) / 4
            : (availableWidth - 12) / 2,
    child: Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label, style: const TextStyle(fontSize: 12, color: muted)),
            const SizedBox(height: 10),
            Text(
              value,
              style: const TextStyle(
                fontSize: 25,
                fontWeight: FontWeight.w700,
                color: green,
              ),
            ),
          ],
        ),
      ),
    ),
  );
  Future<void> navigate(String provider, Json input) async {
    final o = input['origin']['point'];
    final d = input['destination']['point'];
    final url =
        provider == 'google'
            ? Uri.https('www.google.com', '/maps/dir/', {
              'api': '1',
              'origin': '${o['latitude']},${o['longitude']}',
              'destination': '${d['latitude']},${d['longitude']}',
              'travelmode': 'driving',
            })
            : Uri.https('waze.com', '/ul', {
              'll': '${d['latitude']},${d['longitude']}',
              'navigate': 'yes',
            });
    try {
      if (!await launchUrl(url, mode: LaunchMode.externalApplication) &&
          mounted) {
        message(context, 'No se pudo abrir el navegador.');
      }
    } catch (_) {
      if (mounted) message(context, 'No se pudo abrir el navegador.');
    }
  }
}
