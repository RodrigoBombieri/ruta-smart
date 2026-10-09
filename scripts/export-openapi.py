"""Export the implemented HTTP surface, with record schemas from Core/Models.cs."""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def ref(name):
    return {"$ref": f"#/components/schemas/{name}"}


def schema(type_name):
    if type_name.endswith("?"):
        return {"anyOf": [schema(type_name[:-1]), {"type": "null"}]}
    if type_name.endswith("[]"):
        return {"type": "array", "items": schema(type_name[:-2])}
    return {
        "string": {"type": "string"}, "bool": {"type": "boolean"},
        "int": {"type": "integer"}, "double": {"type": "number"},
        "decimal": {"type": "number"}, "Guid": {"type": "string", "format": "uuid"},
        "DateTimeOffset": {"type": "string", "format": "date-time"},
    }.get(type_name, ref(type_name))


models = (ROOT / "src/RutaSmart.Core/Models.cs").read_text(encoding="utf-8")
schemas = {}
for name, parameters in re.findall(r"public sealed record (\w+)\((.*?)\)(?:;|\s*\{)", models, re.S):
    properties, required = {}, []
    for parameter in parameters.split(","):
        match = re.fullmatch(r"\s*([\w?\[\]]+)\s+(\w+)(?:\s*=\s*(.*))?\s*", parameter)
        if not match:
            raise ValueError(parameter)
        type_name, field, default = match.groups()
        field = field[0].lower() + field[1:]
        properties[field] = schema(type_name)
        if default is None and not type_name.endswith("?"):
            required.append(field)
    schemas[name] = {"type": "object", "properties": properties, "required": required}
for field, type_name in [("weatherAlerts", "WeatherMatch[]"), ("stations", "StationCandidate[]")]:
    schemas["Assessment"]["properties"][field] = schema(type_name)
    schemas["Assessment"]["required"].append(field)
schemas["Session"] = {"type": "object", "properties": {"token": schema("string"), "expiresAt": schema("DateTimeOffset")}, "required": ["token", "expiresAt"]}
schemas["Problem"] = {"type": "object", "properties": {key: schema(kind) for key, kind in {"type":"string","title":"string","status":"int","code":"string","correlationId":"string"}.items()}, "required": ["title", "status"]}
schemas["SourceStatus"] = {"type": "object", "properties": {key: schema(kind) for key, kind in {
    "routing":"string", "weather":"string", "weatherDetail":"string", "weatherLastSuccessAt":"DateTimeOffset?",
    "weatherRecords":"int", "weatherRejected":"int", "roads":"string", "stations":"string",
    "stationsDetail":"string", "stationsCount":"int", "stationsUpdatedAt":"DateTimeOffset?",
    "prices":"string", "accounts":"string", "storage":"string", "demoAvailable":"bool",
}.items()}}


def response(description, body=None, mime="application/json"):
    return {"description": description, **({"content": {mime: {"schema": body}}} if body else {})}


errors = {"default": response("Error de validación, sesión, cuota o dependencia", ref("Problem"), "application/problem+json")}
trip_id = {"name": "id", "in": "path", "required": True, "schema": schema("Guid")}
idem = {"name": "Idempotency-Key", "in": "header", "required": True, "schema": {"type": "string", "pattern": "^[a-zA-Z0-9_-]{8,100}$"}}


def operation(operation_id, status, body=None, parameters=None, public=False):
    result = {"operationId": operation_id, "responses": {str(status): response("Resultado", body), **errors}}
    if parameters:
        result["parameters"] = parameters
    if public:
        result["security"] = []
    return result


def analysis(operation_id, parameters):
    result = operation(operation_id, 201, ref("Assessment"), parameters)
    result["requestBody"] = {"required": True, "content": {"application/json": {"schema": ref("TripInput")}}}
    return result


paths = {
    "/health/live": {"get": operation("health", 200, {"type":"object","properties":{"status":schema("string")}}, public=True)},
    "/api/v1/sessions/anonymous": {"post": operation("createSession", 201, ref("Session"), public=True)},
    "/api/v1/locations/search": {"get": operation("searchLocations", 200, schema("Location[]"), [{"name":"q","in":"query","required":True,"schema":{"type":"string","minLength":2,"maxLength":80}}])},
    "/api/v1/trips/analyze": {"post": analysis("analyzeTrip", [idem])},
    "/api/v1/trips/{id}": {"get": operation("getTrip", 200, ref("Assessment"), [trip_id]), "delete": operation("deleteTrip", 204, parameters=[trip_id])},
    "/api/v1/trips/{id}/analyses": {"post": analysis("reanalyzeTrip", [trip_id, idem])},
    "/api/v1/sources/status": {"get": operation("sourceStatus", 200, ref("SourceStatus"), public=True)},
    "/api/v1/trips": {"get": {"operationId":"listTripsUnavailable","summary":"Cuentas no habilitadas; guardado anónimo local","responses":{"403":response("Cuentas no configuradas",ref("Problem"),"application/problem+json"),**errors}}},
}
for method in ("get", "delete"):
    paths["/api/v1/trips/{id}"][method]["responses"]["404"] = response("No existe o no pertenece a la sesión")
paths["/api/v1/trips/{id}/analyses"]["post"]["responses"]["404"] = response("No existe o no pertenece a la sesión")
spec = {
    "openapi": "3.1.0",
    "info": {"title": "RutaSmart Argentina", "version": "0.1.0", "description": "Operaciones implementadas. Routing piloto y cobertura parcial; sin cuentas sincronizadas."},
    "servers": [{"url": "http://localhost:5080", "description": "Desarrollo local"}],
    "security": [{"session": []}], "paths": paths,
    "components": {"securitySchemes": {"session": {"type":"http","scheme":"bearer","bearerFormat":"opaque"}}, "schemas": schemas},
}
content = json.dumps(spec, ensure_ascii=False, indent=2) + "\n"
target = ROOT / "docs/openapi.json"
if "--check" in sys.argv:
    if target.read_text(encoding="utf-8") != content:
        raise SystemExit("OpenAPI desactualizado: ejecutar python scripts/export-openapi.py")
else:
    target.write_text(content, encoding="utf-8")
print(f"OpenAPI: {len(paths)} paths, {len(schemas)} schemas.")
