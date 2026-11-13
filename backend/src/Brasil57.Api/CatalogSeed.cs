using Brasil57.Domain;
using Brasil57.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Brasil57.Api;

public static class CatalogSeed
{
    private static readonly (string Name, string Slug)[] Departments =
    [
        ("Carros", "carros"), ("Caminhonetes e SUVs", "caminhonetes-e-suvs"),
        ("Caminhões e veículos pesados", "caminhoes-e-veiculos-pesados"),
        ("Motocicletas", "motocicletas"), ("Motor", "motor"),
        ("Suspensão e direção", "suspensao-e-direcao"), ("Freios", "freios"),
        ("Pneus e rodas", "pneus-e-rodas"), ("Sistema elétrico", "sistema-eletrico"),
        ("Iluminação", "iluminacao"), ("Transmissão e câmbio", "transmissao-e-cambio"),
        ("Filtros", "filtros"), ("Ignição", "ignicao"),
        ("Arrefecimento", "arrefecimento"), ("Escapamento", "escapamento"),
        ("Carroceria", "carroceria"), ("Acessórios automotivos", "acessorios-automotivos"),
        ("Áudio e vídeo", "audio-e-video"), ("Manutenção e ferramentas", "manutencao-e-ferramentas"),
        ("Lubrificantes e fluidos", "lubrificantes-e-fluidos"), ("Kits automotivos", "kits-automotivos")
    ];

    public static async Task EnsureDepartmentsAsync(StoreDbContext db)
    {
        var existing = await db.Categories.Select(x => x.Slug).ToListAsync();
        var slugs = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, slug) in Departments)
            if (!slugs.Contains(slug)) db.Categories.Add(new Category { Name = name, Slug = slug });
        await db.SaveChangesAsync();
    }
}
