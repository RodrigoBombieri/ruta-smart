using System.Globalization;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;
using RutaSmart.Core;

namespace RutaSmart.Infrastructure;
public sealed record EnergySnapshot(FuelStation[] Stations,DateTimeOffset FetchedAt,int Rows,int Rejected,int ExcludedProducts,string Transport);

public static class EnergyParser
{
    public static EnergySnapshot Parse(string csv,DateTimeOffset now,string transport)
    {
        using var reader=new TextFieldParser(new StringReader(csv)) { HasFieldsEnclosedInQuotes=true,TrimWhiteSpace=true };
        reader.SetDelimiters(",");
        var headers=reader.ReadFields() ?? throw new FormatException("CSV header missing");
        var columns=headers.Select((value,index)=>(value,index)).ToDictionary(p=>p.value,p=>p.index,StringComparer.OrdinalIgnoreCase);
        string[] required=["idempresa","empresa","direccion","localidad","provincia","idproducto","tipohorario","precio","fecha_vigencia","empresabandera","latitud","longitud"];
        if (required.Any(h=>!columns.ContainsKey(h))) throw new FormatException("CSV schema changed");
        var stations=new Dictionary<(string Id,string Fuel),FuelStation>();
        int rows=0,rejected=0,excluded=0;
        while (!reader.EndOfData)
        {
            if (++rows>100000) throw new FormatException("CSV exceeds row limit");
            var values=reader.ReadFields();
            if (values is null || values.Length!=headers.Length) { rejected++; continue; }
            string Cell(string name)=>values[columns[name]];
            string? fuel=Cell("idproducto") switch { "2"=>"nafta_2","3"=>"nafta_3","19"=>"diesel_2","21"=>"diesel_3",_=>null };
            if (fuel is null) { excluded++; continue; }
            if (!double.TryParse(Cell("latitud"),NumberStyles.Float,CultureInfo.InvariantCulture,out var lat) ||
                !double.TryParse(Cell("longitud"),NumberStyles.Float,CultureInfo.InvariantCulture,out var lon) ||
                !double.IsFinite(lat) || !double.IsFinite(lon) || lat is <-55.2 or >-21.7 || lon is <-73.6 or >-53.5 ||
                !decimal.TryParse(Cell("precio"),NumberStyles.Number,CultureInfo.InvariantCulture,out var amount) || amount is <=0 or >1000000 ||
                !DateTime.TryParseExact(Cell("fecha_vigencia"),"yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date) ||
                Cell("idempresa").Length is <1 or >40 || Cell("empresa").Length>200)
            { rejected++; continue; }
            // Source values lack an offset. Argentine local time (-03:00) is an explicit adapter assumption.
            var declared=new DateTimeOffset(DateTime.SpecifyKind(date,DateTimeKind.Unspecified),TimeSpan.FromHours(-3));
            if (declared>now.AddMinutes(5)) { rejected++; continue; }
            var point=new Point(lat,lon); var key=(Cell("idempresa"),fuel);
            var price=new StationPrice(amount,declared,Cell("tipohorario"));
            if (stations.TryGetValue(key,out var prior))
            {
                if (Geo.Distance(prior.Point,point)>100) { rejected++; continue; }
                var prices=prior.Prices.Append(price).GroupBy(p=>p.Schedule).Select(g=>g.OrderByDescending(p=>p.DeclaredAt).First()).ToArray();
                stations[key]=prior with { Prices=prices };
            }
            else stations[key]=new(key.Item1,Cell("empresa"),Cell("empresabandera"),$"{Cell("direccion")}, {Cell("localidad")}, {Cell("provincia")}",point,fuel,[price]);
        }
        if (stations.Count==0 || rejected>(rows-excluded)*.2) throw new FormatException("CSV quality check failed; previous snapshot retained");
        return new(stations.Values.ToArray(),now,rows,rejected,excluded,transport);
    }
}

public sealed record EnergyOptions(string? CsvPath,bool AllowPublicHttp);
public sealed class EnergyProvider(HttpClient http,IStore store,EnergyOptions options)
{
    public const string SourceUrl="http://datos.energia.gob.ar/dataset/1c181390-5045-475e-94dc-410429be4b17/resource/80ac25de-a44a-4445-9215-090cf55cfda5/download/precios-en-surtidor-resolucin-3142016.csv";
    EnergySnapshot? snapshot;
    public EnergySnapshot? Snapshot=>Volatile.Read(ref snapshot);
    public async Task Restore(CancellationToken ct)
    {
        var json=await store.ReadSource("energia-stations",ct);
        if (json is not null) Volatile.Write(ref snapshot,JsonSerializer.Deserialize<EnergySnapshot>(json));
    }
    public async Task Refresh(CancellationToken ct)
    {
        string csv,transport;
        if (!string.IsNullOrWhiteSpace(options.CsvPath))
        {
            var file=new FileInfo(options.CsvPath);
            if (file.Length>25_000_000) throw new FormatException("CSV too large");
            csv=await File.ReadAllTextAsync(options.CsvPath,ct); transport="local_import";
        }
        else if (options.AllowPublicHttp)
        {
            // Official download currently redirects HTTPS to HTTP. No tokens or user locations are sent.
            // Enabled only in Development unless an operator explicitly chooses otherwise.
            using var response=await http.GetAsync(SourceUrl,HttpCompletionOption.ResponseHeadersRead,ct);
            response.EnsureSuccessStatusCode(); await response.Content.LoadIntoBufferAsync(25_000_000,ct);
            csv=await response.Content.ReadAsStringAsync(ct); transport="public_http";
        }
        else return;
        var next=EnergyParser.Parse(csv,DateTimeOffset.UtcNow,transport);
        await store.WriteSource("energia-stations",JsonSerializer.Serialize(next),ct);
        Volatile.Write(ref snapshot,next);
    }
    public Coverage Coverage(DateTimeOffset now)=>new("stations",Snapshot is null ? "unavailable" : Snapshot.FetchedAt<now.AddDays(-7) ? "stale" : "partial",
        Snapshot is null ? "No hay inventario de estaciones descargado." : "Catálogo público de Energía. Hasta 20 candidatos a 5 km del trazado; acceso vial, operación y stock sin verificar. La fecha del precio se conserva.",Snapshot?.FetchedAt);
    public Assessment Apply(Assessment assessment,DateTimeOffset now)
    {
        if (assessment.Route.Synthetic) return assessment;
        var candidates=StationEngine.Candidates(assessment,Snapshot?.Stations ?? []);
        var coverage=assessment.Coverage.Select(c=>c.Dimension=="stations" ? Coverage(now) : c.Dimension=="prices" ?
            new Coverage("prices",Snapshot is null && assessment.Input.ManualPriceArsPerLitre is null ? "unavailable" : "partial",
                "Precios declarados por estación con fecha y horario. Mayores a 7 días no se usan para comparar. El costo del viaje usa solo tu referencia manual.",Snapshot?.FetchedAt) : c).ToArray();
        return assessment with { Stations=candidates,Coverage=coverage };
    }
}
