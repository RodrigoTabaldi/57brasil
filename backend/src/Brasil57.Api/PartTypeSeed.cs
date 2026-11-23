using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using Brasil57.Domain;
using Brasil57.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Brasil57.Api;

public static class PartTypeSeed
{
    public const string AwaitingSupplier = "aguardando dados do fornecedor";

    // The image illustrates the group, never a confirmed make, model, or fitment.
    private static readonly (string Group, string Image)[] Groups =
    [
        ("Filtros", "/demo-parts/oil-filter.jpg"),
        ("Freios", "/demo-parts/brake-pad.jpg"),
        ("Suspensão e direção", "/demo-parts/shock.jpg"),
        ("Motor", "/demo-parts/belt.jpg"),
        ("Elétrica e ignição", "/demo-parts/spark-plug.jpg"),
        ("Arrefecimento", "/demo-parts/radiator.jpg"),
        ("Transmissão e embreagem", "/demo-parts/clutch.jpg"),
        ("Iluminação e visibilidade", "/demo-parts/lamp.jpg"),
        ("Combustível e escape", "/demo-parts/fuel-pump.jpg"),
        ("Manutenção e outros", "/demo-parts/wiper.jpg")
    ];

    private static readonly string[][] Names =
    [
        ["filtro de óleo", "filtro de ar do motor", "filtro de combustível", "filtro de cabine", "filtro de transmissão", "filtro do respiro do cárter", "separador de água do diesel", "filtro de direção hidráulica", "filtro de ar secundário", "filtro de óleo do câmbio CVT"],
        ["pastilha de freio dianteira", "pastilha de freio traseira", "disco de freio dianteiro", "disco de freio traseiro", "sapata de freio", "tambor de freio", "cilindro mestre de freio", "cilindro de roda", "flexível de freio", "fluido de freio"],
        ["amortecedor dianteiro", "amortecedor traseiro", "kit batente do amortecedor", "coxim do amortecedor", "bieleta", "pivô de suspensão", "bandeja de suspensão", "bucha da bandeja", "terminal de direção", "barra axial"],
        ["correia dentada", "kit de correia dentada", "correia de acessórios", "tensor da correia", "junta do cabeçote", "junta da tampa de válvulas", "coxim do motor", "bomba de óleo", "retentor do virabrequim", "corrente de comando"],
        ["bateria automotiva", "vela de ignição", "bobina de ignição", "cabo de vela", "alternador", "motor de partida", "sensor de rotação", "sensor de fase", "sonda lambda", "fusível automotivo"],
        ["radiador", "bomba d’água", "reservatório de expansão", "tampa do reservatório", "mangueira superior do radiador", "mangueira inferior do radiador", "eletroventilador", "sensor de temperatura", "válvula termostática", "aditivo de arrefecimento"],
        ["kit de embreagem", "platô de embreagem", "disco de embreagem", "rolamento de embreagem", "cilindro mestre da embreagem", "atuador da embreagem", "semieixo", "junta homocinética", "coifa da homocinética", "óleo de transmissão"],
        ["farol dianteiro", "lanterna traseira", "lâmpada de farol", "lâmpada de seta", "palheta dianteira", "palheta traseira", "máquina do vidro elétrico", "interruptor do vidro elétrico", "retrovisor externo", "buzina"],
        ["bomba de combustível", "bico injetor", "corpo de borboleta", "regulador de pressão de combustível", "sensor MAP", "sensor MAF", "catalisador", "silenciador do escapamento", "tubo do escapamento", "abraçadeira do escapamento"],
        ["óleo de motor", "fluido de direção hidráulica", "líquido do limpador de para-brisa", "rolamento de roda", "cubo de roda", "coxim do câmbio", "correia do alternador", "tampa de combustível", "chave de roda", "macaco automotivo"]
    ];

    public static IReadOnlyList<PartType> InitialTypes => Groups.SelectMany((group, index) => Names[index].Select(name =>
    {
        var slug = Slug(name);
        return new PartType { Id = StableId(slug), Name = name, Slug = slug, Group = group.Group,
            GroupSlug = Slug(group.Group), Status = AwaitingSupplier, ImageUrl = ImageFor(name, group.Image) };
    })).ToArray();

    public static async Task EnsureAsync(StoreDbContext db, CancellationToken ct = default)
    {
        var existing = (await db.PartTypes.Select(x => x.Id).ToListAsync(ct)).ToHashSet();
        foreach (var type in InitialTypes)
            if (!existing.Contains(type.Id)) db.PartTypes.Add(type);
        await db.SaveChangesAsync(ct);
    }

    internal static string Slug(string text)
    {
        var normalized = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var plain = new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray())
            .Replace('’', '-');
        return Regex.Replace(Regex.Replace(plain, "[^a-z0-9]+", "-"), "^-|-$", "");
    }

    private static string ImageFor(string name, string groupImage) => name switch
    {
        "bateria automotiva" => "/demo-parts/battery.jpg",
        "alternador" => "/demo-parts/alternator.jpg",
        "bomba d’água" => "/demo-parts/water-pump.jpg",
        "disco de freio dianteiro" or "disco de freio traseiro" => "/demo-parts/brake-disc.jpg",
        "farol dianteiro" => "/demo-parts/headlamp.jpg",
        "bomba de combustível" => "/demo-parts/fuel-pump.jpg",
        "silenciador do escapamento" or "tubo do escapamento" or "abraçadeira do escapamento" => "/demo-parts/exhaust.jpg",
        "palheta dianteira" or "palheta traseira" => "/demo-parts/wiper.jpg",
        "rolamento de roda" or "cubo de roda" => "/demo-parts/wheel-hub.jpg",
        _ => groupImage
    };

    private static Guid StableId(string slug) => new(SHA256.HashData(Encoding.UTF8.GetBytes($"57brasil:part-type:{slug}"))[..16]);
}
