namespace RutaSmart.Core;

public static class RoadEngine
{
    public static Assessment Apply(Assessment assessment,IEnumerable<RoadNotice> notices,Coverage coverage,DateTimeOffset now)
    {
        if(assessment.Route.Synthetic) return assessment;
        var matches=new List<RoadMatch>();
        foreach(var notice in notices.Where(n=>n.Status!="HABILITADA" || !string.IsNullOrWhiteSpace(n.Description)))
        {
            var road=ProvinceCatalog.NationalRoad(notice.Road,true);
            if (road is null) continue;
            var sections=assessment.Route.Roads.Where(r=>r.Reference==road && ProvinceCatalog.Key(r.Province)==ProvinceCatalog.Key(notice.Province)).ToArray();
            if(sections.Length==0) continue;
            matches.Add(new(notice,"road_and_province_only",sections.Min(s=>s.DistanceFromM),sections.Max(s=>s.DistanceToM),
                notice.UpdatedAt is null || notice.UpdatedAt>now || notice.UpdatedAt<now.AddHours(-48) || coverage.Availability is "stale" or "unavailable"));
        }
        var selected=matches.OrderBy(m=>m.Notice.Status=="CORTE TOTAL" ? 0 : m.Notice.Status=="HABILITADA" ? 2 : 1)
            .ThenBy(m=>m.DistanceFromM).ThenBy(m=>m.Notice.Id).Take(40).ToArray();
        var reasons=assessment.Reasons.Concat(selected.Select(m=>new Reason("vialidad_notice","roads","caution",
            $"Vialidad: {m.Notice.Status} en RN {m.Notice.Road}, {m.Notice.Province}, {m.Notice.Section}. Coincide la ruta y provincia; tramo exacto, sentido y vigencia al pasar sin confirmar.",
            m.Notice.Url,m.Notice.Id,m.DistanceFromM,m.DistanceToM,m.Notice.UpdatedAt,null,"approximate_section"))).ToArray();
        var detail=coverage with { Message=coverage.Message+(assessment.Route.Roads.Length==0 ? " El proveedor no entregó referencias RN identificables." : " Coincidencia por RN y provincia; no acredita tránsito por el tramo exacto ni su sentido.") };
        var dimensions=assessment.Coverage.Select(c=>c.Dimension=="roads" ? detail : c).ToArray();
        return assessment with { RoadNotices=selected,Reasons=reasons,Coverage=dimensions,Status=AssessmentEngine.Status(reasons,dimensions) };
    }
}
