using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;

namespace RutaSmart.Core;

public sealed class ProvinceCatalog
{
    readonly (string Name, IPreparedGeometry Shape)[] provinces;
    public static ProvinceCatalog Shared { get; } = new();
    ProvinceCatalog()
    {
        using var stream=typeof(ProvinceCatalog).Assembly.GetManifestResourceStream("RutaSmart.Core.Data.provinces.geojson")!;
        using var json=JsonDocument.Parse(stream);
        var factory=new GeometryFactory(new PrecisionModel(),4326);
        LinearRing Ring(JsonElement coordinates)=>factory.CreateLinearRing(coordinates.EnumerateArray()
            .Select(p=>new Coordinate(p[0].GetDouble(),p[1].GetDouble())).ToArray());
        Polygon Polygon(JsonElement rings)=>factory.CreatePolygon(Ring(rings[0]),rings.EnumerateArray().Skip(1).Select(Ring).ToArray());
        provinces=json.RootElement.GetProperty("features").EnumerateArray().Select(feature=> {
            var g=feature.GetProperty("geometry"); var coordinates=g.GetProperty("coordinates");
            Geometry shape=g.GetProperty("type").GetString()=="Polygon" ? Polygon(coordinates)
                : factory.CreateMultiPolygon(coordinates.EnumerateArray().Select(Polygon).ToArray());
            if (!shape.IsValid) throw new InvalidDataException("Invalid bundled province geometry");
            return (feature.GetProperty("properties").GetProperty("name").GetString()!,PreparedGeometryFactory.Prepare(shape));
        }).ToArray();
    }
    public string? Province(Point point)
    {
        var p=new NetTopologySuite.Geometries.Point(point.Longitude,point.Latitude);
        return provinces.FirstOrDefault(x=>x.Shape.Geometry.EnvelopeInternal.Covers(p.Coordinate) && x.Shape.Covers(p)).Name;
    }
    public bool Contains(Point point)
    {
        if(Province(point) is not null) return true;
        // ~500 m tolerance for cartographic differences, not a legal boundary test.
        var p=new NetTopologySuite.Geometries.Point(point.Longitude,point.Latitude);
        return provinces.Any(x=>x.Shape.Geometry.EnvelopeInternal.Distance(p.EnvelopeInternal)<=.005 && x.Shape.Geometry.IsWithinDistance(p,.005));
    }
    public bool CrossesBorder(Point[] points)=>points.Any(p=>!Contains(p));
    public static string Key(string value)=>string.Concat(value.Normalize(NormalizationForm.FormD)
        .Where(c=>System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)!=System.Globalization.UnicodeCategory.NonSpacingMark))
        .ToUpperInvariant().Replace(" ","").Replace("-","");
    public static string? NationalRoad(string reference, bool officialNumber=false)
    {
        var text=reference.Trim().ToUpperInvariant();
        var match=Regex.Match(text,officialNumber ? @"^(?:RN\s*)?(A[\s-]*)?0*(\d+)$" : @"^RN\s*-?\s*(A[\s-]*)?0*(\d+)$");
        return match.Success ? (match.Groups[1].Success ? "A" : "")+int.Parse(match.Groups[2].Value) : null;
    }
    public RouteRoad[] LocateRoad(string reference,Point[] geometry,double from,double distance)
    {
        var refs=reference.Split(';').Select(r=>NationalRoad(r)).Where(r=>r is not null).Cast<string>().Distinct().ToArray();
        if (refs.Length==0 || geometry.Length<2) return [];
        var lengths=geometry.Zip(geometry.Skip(1),Geo.Distance).ToArray(); var total=lengths.Sum();
        if (total<=0) return [];
        var result=new List<RouteRoad>(); var progress=from;
        for(var i=0;i<lengths.Length;i++)
        {
            var next=progress+distance*lengths[i]/total;
            var a=geometry[i]; var b=geometry[i+1];
            var province=Province(new((a.Latitude+b.Latitude)/2,(a.Longitude+b.Longitude)/2));
            if (province is not null)
                foreach(var road in refs) result.Add(new(road,province,progress,next));
            progress=next;
        }
        return result.GroupBy(r=>(r.Reference,r.Province)).Select(g=>new RouteRoad(g.Key.Reference,g.Key.Province,g.Min(r=>r.DistanceFromM),g.Max(r=>r.DistanceToM))).ToArray();
    }
}
