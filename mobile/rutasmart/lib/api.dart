import 'dart:convert';
import 'dart:math';
import 'package:http/http.dart' as http;

typedef Json = Map<String, dynamic>;

class ApiException implements Exception {
  final String message;
  final int status;
  ApiException(this.message, [this.status = 0]);
  @override
  String toString() => message;
}

class RutaApi {
  final String baseUrl;
  final http.Client client;
  String? _token;
  String? _lastBody;
  String? _lastKey;
  RutaApi({String? baseUrl, http.Client? client})
    : baseUrl =
          baseUrl ??
          const String.fromEnvironment(
            'API_URL',
            defaultValue: 'http://localhost:5080',
          ),
      client = client ?? http.Client();
  Future<dynamic> request(
    String method,
    String path, {
    Json? body,
    String? key,
    bool auth = true,
    bool retry = true,
  }) async {
    try {
      if (auth && _token == null) {
        final session =
            await request('POST', '/sessions/anonymous', auth: false) as Json;
        _token = session['token'] as String;
      }
      final req = http.Request(method, Uri.parse('$baseUrl/api/v1$path'));
      req.headers['Content-Type'] = 'application/json';
      if (auth) req.headers['Authorization'] = 'Bearer $_token';
      if (key != null) req.headers['Idempotency-Key'] = key;
      if (body != null) req.body = jsonEncode(body);
      final response = await client
          .send(req)
          .then(http.Response.fromStream)
          .timeout(const Duration(seconds: 20));
      if (response.statusCode == 401 && retry) {
        _token = null;
        return request(
          method,
          path,
          body: body,
          key: key,
          auth: auth,
          retry: false,
        );
      }
      if (response.statusCode >= 400) {
        String message =
            'No pudimos completar la solicitud (${response.statusCode}).';
        try {
          message =
              (jsonDecode(response.body) as Json)['title'] as String? ??
              message;
        } catch (_) {}
        throw ApiException(message, response.statusCode);
      }
      return response.body.isEmpty ? null : jsonDecode(response.body);
    } on ApiException {
      rethrow;
    } catch (_) {
      throw ApiException(
        'No pudimos conectar con RutaSmart. Revisá la conexión y la dirección del servidor. Tus viajes guardados siguen disponibles.',
      );
    }
  }

  Future<Json> analyze(Json input, {bool refresh = false}) async {
    final encoded = jsonEncode(input);
    if (refresh || _lastBody != encoded) {
      _lastBody = encoded;
      _lastKey =
          List.generate(
            24,
            (_) => Random.secure().nextInt(16).toRadixString(16),
          ).join();
    }
    return await request('POST', '/trips/analyze', body: input, key: _lastKey)
        as Json;
  }

  Future<List<Json>> search(String q) async =>
      (await request(
                'GET',
                '/locations/search?q=${Uri.encodeQueryComponent(q)}',
              )
              as List)
          .cast<Json>();
  Future<Json> status() async =>
      await request('GET', '/sources/status', auth: false) as Json;
  Future<void> delete(String id) async {
    await request('DELETE', '/trips/$id');
  }

  void close() => client.close();
}
