using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using RutaSmart.Core;

namespace RutaSmart.Infrastructure;

public sealed record VialidadSnapshot(RoadNotice[] Notices,DateTimeOffset FetchedAt,int Rejected);
public static class VialidadParser
{
    public const string Page="https://www.argentina.gob.ar/transporte/vialidad-nacional/estado-de-rutas";
    static string Plain(string value)=>WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(value,@"<br\s*/?>","\n",RegexOptions.IgnoreCase),"<[^>]*>","" )).Trim();
    public static VialidadSnapshot Parse(string csv,DateTimeOffset now)
    {
        using var parser=new TextFieldParser(new StringReader(csv)) { TextFieldType=FieldType.Delimited,HasFieldsEnclosedInQuotes=true };
        parser.SetDelimiters(","); var headers=parser.ReadFields() ?? [];
        var required=new[]{"filtro-provincia","filtro-ruta","filtro-tramo","estado","calzada","observaciones","actualizado"};
        if(required.Any(h=>!headers.Contains(h))) throw new FormatException("Vialidad schema changed");
        var items=new Dictionary<string,RoadNotice>(); var rejected=0;
        while(!parser.EndOfData)
        {
            var row=parser.ReadFields() ?? [];
            if(row.All(string.IsNullOrWhiteSpace)) continue;
            string Cell(string name)=>Array.IndexOf(headers,name) is var i && i<row.Length ? Plain(row[i]) : "";
            if(Cell("filtro-provincia")=="Provincia") continue;
            var province=Cell("filtro-provincia"); var road=Cell("filtro-ruta"); var section=Cell("filtro-tramo"); var status=Cell("estado");
            if(province.Length is <1 or >150 || road.Length is <1 or >60 || section.Length is <1 or >500 ||
                status is not ("HABILITADA" or "RESTRINGIDA" or "CORTE PARCIAL" or "CORTE TOTAL") || Cell("observaciones").Length>20000) { rejected++; continue; }
            DateTimeOffset? updated=DateTime.TryParse(Cell("actualizado"),CultureInfo.GetCultureInfo("es-AR"),DateTimeStyles.None,out var date)
                ? new DateTimeOffset(DateTime.SpecifyKind(date,DateTimeKind.Unspecified),TimeSpan.FromHours(-3)) : null;
            var id=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{province}|{road}|{section}")))[..24];
            var notice=new RoadNotice(id,province,road,section,status,Cell("calzada"),Cell("observaciones"),updated,Page);
            if(!items.TryGetValue(id,out var prior) || updated.GetValueOrDefault()>=prior.UpdatedAt.GetValueOrDefault()) items[id]=notice;
        }
        if(items.Count==0 || rejected>items.Count*.2) throw new FormatException("Vialidad quality gate failed");
        return new(items.Values.ToArray(),now,rejected);
    }
}
public sealed class VialidadProvider(HttpClient http,IStore store)
{
    // Public spreadsheet and sheet name published in the official page's table configuration.
    public const string CsvUrl="https://docs.google.com/spreadsheets/d/17AqjqeNvM4nG6cOUsUFKFaKXMiNmztYfzHIxeM9FcXk/gviz/tq?tqx=out:csv&sheet=tablavisible";
    VialidadSnapshot? snapshot;
    public VialidadSnapshot? Snapshot=>Volatile.Read(ref snapshot);
    public async Task Restore(CancellationToken ct)
    {
        var saved=await store.ReadSource("vialidad",ct);
        if(saved is not null) Volatile.Write(ref snapshot,JsonSerializer.Deserialize<VialidadSnapshot>(saved));
    }
    public async Task Refresh(CancellationToken ct)
    {
        using var response=await http.GetAsync(CsvUrl,HttpCompletionOption.ResponseHeadersRead,ct);
        response.EnsureSuccessStatusCode(); await response.Content.LoadIntoBufferAsync(8_000_000,ct);
        var next=VialidadParser.Parse(await response.Content.ReadAsStringAsync(ct),DateTimeOffset.UtcNow);
        if(Snapshot is { } old && next.Notices.Length<old.Notices.Length*.7) throw new FormatException("Vialidad truncated inventory; previous snapshot retained");
        await store.WriteSource("vialidad",JsonSerializer.Serialize(next),ct);
        Volatile.Write(ref snapshot,next);
    }
    public Coverage Coverage(DateTimeOffset now)=>new("roads",Snapshot is null ? "unavailable" : Snapshot.FetchedAt<now.AddHours(-24) ? "stale" : "partial",
        Snapshot is null ? "Vialidad: todavía no hay una descarga válida." : "Parte público de Vialidad Nacional, con fechas por tramo. Cobertura parcial; no incluye toda la red provincial ni garantiza el estado a la hora de salida.",Snapshot?.FetchedAt);
    public Assessment Apply(Assessment a,DateTimeOffset now)=>RoadEngine.Apply(a,Snapshot?.Notices ?? [],Coverage(now),now);
}
