"""Convert GeoRef's provincial shapefile to a compact, attributed planning layer.
Requires pyshp and shapely. Input: official provincias?formato=shp&max=100 ZIP.
"""
import hashlib
import io
import json
import sys
import zipfile
from pathlib import Path
import shapefile
from shapely.geometry import shape, mapping, box
from shapely import make_valid

root = Path(__file__).resolve().parent.parent
source = Path(sys.argv[1])
with zipfile.ZipFile(source) as archive:
    def member(suffix):
        return io.BytesIO(archive.read(next(n for n in archive.namelist() if n.endswith(suffix))))
    reader = shapefile.Reader(shp=member('.shp'), shx=member('.shx'), dbf=member('.dbf'), encoding='utf-8')
    features = []
    for record in reader.iterShapeRecords():
        properties = record.record.as_dict()
        geometry = make_valid(shape(record.shape.__geo_interface__)).intersection(box(-74, -56, -53, -21))
        geometry = geometry.simplify(.0005, preserve_topology=True)
        if geometry.is_empty:
            continue
        if geometry.geom_type not in ('Polygon', 'MultiPolygon') or not geometry.is_valid:
            raise ValueError(f'Invalid provincial geometry: {properties}')
        features.append({'type':'Feature','properties':{'id':properties['id'],'name':properties['nombre']},'geometry':mapping(geometry)})
result = {'type':'FeatureCollection','source':'https://apis.datos.gob.ar/georef/api/provincias?formato=shp&max=100',
          'attribution':'GeoRef / fuentes oficiales argentinas — CC BY 4.0',
          'retrievedAt':'2026-10-09','sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),
          'transform':'Clipped to road-planning extent; topology-preserving simplification 0.0005 degrees. Not a legal boundary.',
          'features':features}
target = root / 'src/RutaSmart.Core/Data/provinces.geojson'
target.parent.mkdir(parents=True, exist_ok=True)
target.write_text(json.dumps(result,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
print(f'{len(features)} provinces, {target.stat().st_size} bytes')
