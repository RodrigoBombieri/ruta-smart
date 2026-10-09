using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using NetTopologySuite.Geometries;
using RutaSmart.Core;
using Point = RutaSmart.Core.Point;

namespace RutaSmart.Infrastructure;

public sealed record SmnSnapshot(WeatherAlert[] Alerts, DateTimeOffset? LastSuccessAt, string State, int Rejected);

public static partial class CapParser
{
    public static XDocument Xml(string input)
    {
        using var reader = XmlReader.Create(new StringReader(input),new XmlReaderSettings {
            DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=2_000_000 });
        return XDocument.Load(reader);
    }
    public static WeatherAlert Parse(string xml,string url)
    {
        XNamespace ns="urn:oasis:names:tc:emergency:cap:1.2";
        var root=Xml(xml).Root ?? throw new FormatException("Missing CAP root");
        string Value(XElement e,string name) => e.Element(ns+name)?.Value.Trim() ?? "";
        if (root.Name!=ns+"alert" || Value(root,"status")!="Actual" || Value(root,"scope")!="Public") throw new FormatException("Not a public actual CAP alert");
        var id=Value(root,"identifier"); var sender=Value(root,"sender");
        if (id.Length is < 1 or > 300 || !sender.EndsWith("@smn.gob.ar",StringComparison.OrdinalIgnoreCase)) throw new FormatException("Unexpected CAP issuer");
        DateTimeOffset Instant(string value) => DateTimeOffset.TryParse(value,CultureInfo.InvariantCulture,DateTimeStyles.None,out var d) && Regex.IsMatch(value,@"(Z|[+-]\d{2}:\d{2})$") ? d : throw new FormatException("Invalid CAP date");
        var sent=Instant(Value(root,"sent"));
        var type=Value(root,"msgType");
        if (type is not ("Alert" or "Update" or "Cancel")) throw new FormatException("Unsupported CAP type");
        var references=Value(root,"references").Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries)
            .Select(r=>r.Split(',')).Where(parts=>parts.Length==3 && parts[0]==sender).Select(parts=>parts[1]).ToArray();
        var info=root.Elements(ns+"info").FirstOrDefault(i=>Value(i,"language").StartsWith("es")) ?? root.Element(ns+"info");
        if (type=="Cancel") return new(id,sender,sent,type,references,"","","","",sent,sent,url,[]);
        if (info is null) throw new FormatException("Missing CAP info");
        var from=string.IsNullOrWhiteSpace(Value(info,"onset")) ? sent : Instant(Value(info,"onset"));
        DateTimeOffset? to=string.IsNullOrWhiteSpace(Value(info,"expires")) ? null : Instant(Value(info,"expires"));
        if (to<=from) throw new FormatException("Invalid CAP interval");
        var polygons=info.Elements(ns+"area").SelectMany(a=>a.Elements(ns+"polygon")).Select(p=>
        {
            var points=p.Value.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries).Select(pair=>
            {
                var xy=pair.Split(','); if (xy.Length!=2) throw new FormatException("Invalid CAP coordinate");
                var lat=double.Parse(xy[0],CultureInfo.InvariantCulture); var lon=double.Parse(xy[1],CultureInfo.InvariantCulture);
                if (!double.IsFinite(lat) || !double.IsFinite(lon) || lat is < -90 or > 90 || lon is < -180 or > 180) throw new FormatException("Invalid CAP coordinate");
                return new Point(lat,lon);
            }).ToArray();
            if (points.Length is < 4 or > 20000 || points[0]!=points[^1]) throw new FormatException("Invalid CAP polygon");
            var polygon=new Polygon(new LinearRing(points.Select(p=>new Coordinate(p.Longitude,p.Latitude)).ToArray()));
            if (!polygon.IsValid || polygon.IsEmpty) throw new FormatException("Invalid CAP geometry");
            return points;
        }).ToArray();
        if (polygons.Length is 0 or > 100) throw new FormatException("No usable CAP polygon");
        string Limited(string name) => Value(info,name) is { Length: <=8000 } value ? value : throw new FormatException("CAP text too long");
        return new(id,sender,sent,type,references,Limited("event"),Limited("severity"),Limited("description"),Limited("instruction"),from,to,url,polygons);
    }
    public static Uri[] Links(string index)
    {
        var candidates=index.TrimStart().StartsWith("<?xml",StringComparison.Ordinal) || index.TrimStart().StartsWith("<rss",StringComparison.Ordinal)
            ? Xml(index).Descendants("item").Select(i=>i.Element("link")?.Value ?? "")
            : Regex.Matches(index,"href=[\"']([^\"']+)[\"']",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1)).Select(m=>WebUtility.HtmlDecode(m.Groups[1].Value));
        return candidates.Where(s=>Uri.TryCreate(s,UriKind.Absolute,out var u) && u.Scheme=="https" && u.Host=="ssl.smn.gob.ar" && u.IsDefaultPort &&
            u.AbsolutePath.StartsWith("/feeds/CAP/",StringComparison.Ordinal) && u.AbsolutePath.EndsWith(".xml",StringComparison.Ordinal) && string.IsNullOrEmpty(u.Query))
            .Select(s=>new Uri(s)).Distinct().ToArray();
    }
    public static WeatherAlert[] Merge(IEnumerable<WeatherAlert> previous,IEnumerable<WeatherAlert> incoming,DateTimeOffset now)
    {
        var all=previous.Concat(incoming).GroupBy(a=>a.Id).Select(g=>g.OrderByDescending(a=>a.Sent).First()).ToArray();
        var replaced=all.SelectMany(a=>a.References).ToHashSet();
        // Preserve update/cancel tombstones so an older alert cannot reappear on a partial feed.
        return all.Where(a=>a.Sent>=now.AddDays(-7) && (a.MessageType=="Cancel" || a.References.Length>0 || !replaced.Contains(a.Id) && (a.To is null || a.To>now))).ToArray();
    }
    public static WeatherAlert[] Active(IEnumerable<WeatherAlert> alerts,DateTimeOffset now)
    {
        var all=alerts.ToArray(); var replaced=all.SelectMany(a=>a.References).ToHashSet();
        return all.Where(a=>a.MessageType!="Cancel" && !replaced.Contains(a.Id) && (a.To is null || a.To>now)).ToArray();
    }
}

public sealed class SmnCapProvider(HttpClient http,IStore store)
{
    SmnSnapshot snapshot=new([],null,"unavailable",0);
    readonly SemaphoreSlim gate=new(1,1);
    public SmnSnapshot Snapshot => Volatile.Read(ref snapshot);
    public async Task Restore(CancellationToken ct)
    {
        var json=await store.ReadSource("smn-cap",ct);
        if (json is not null && System.Text.Json.JsonSerializer.Deserialize<SmnSnapshot>(json) is { } previous)
            Volatile.Write(ref snapshot,previous);
    }
    public Coverage Coverage(DateTimeOffset now) => new("weather",
        Snapshot.LastSuccessAt is null ? "unavailable" : Snapshot.LastSuccessAt<now.AddMinutes(-15) ? "stale" : "partial",
        Snapshot.LastSuccessAt is null ? "SMN CAP: todavía no hay una descarga válida." :
        "SMN CAP público: se evalúan avisos con polígono y vigencia. Cobertura parcial; no acredita ausencia de alertas ni cubre cualquier fecha futura.",Snapshot.LastSuccessAt);
    public async Task Refresh(CancellationToken ct)
    {
        if (!await gate.WaitAsync(0,ct)) return;
        try
        {
            var index=await Download(new Uri("https://ssl.smn.gob.ar/CAP/AR.php"),ct);
            var links=CapParser.Links(index);
            if (links.Length is 0 or > 300) throw new FormatException("CAP index empty or over budget");
            var collected=new System.Collections.Concurrent.ConcurrentBag<WeatherAlert>();
            int rejected=0;
            await Parallel.ForEachAsync(links,new ParallelOptions { MaxDegreeOfParallelism=3,CancellationToken=ct },async (link,token)=>
            {
                try { collected.Add(CapParser.Parse(await Download(link,token),link.AbsoluteUri)); }
                catch (Exception e) when (e is HttpRequestException or FormatException or XmlException or ArgumentException or OperationCanceledException)
                { Interlocked.Increment(ref rejected); }
            });
            if (collected.IsEmpty) throw new FormatException("No usable CAP records");
            var now=DateTimeOffset.UtcNow;
            var next=new SmnSnapshot(CapParser.Merge(Snapshot.Alerts,collected,now),now,rejected>0 ? "partial" : "downloaded",rejected);
            await store.WriteSource("smn-cap",System.Text.Json.JsonSerializer.Serialize(next),ct);
            Volatile.Write(ref snapshot,next);
        }
        finally { gate.Release(); }
    }
    async Task<string> Download(Uri uri,CancellationToken ct)
    {
        using var response=await http.GetAsync(uri,HttpCompletionOption.ResponseHeadersRead,ct);
        response.EnsureSuccessStatusCode();
        await response.Content.LoadIntoBufferAsync(2_000_000,ct);
        return await response.Content.ReadAsStringAsync(ct);
    }
}
