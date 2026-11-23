using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Brasil57.Domain;
using Brasil57.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Brasil57.Api;

public static class SupplierRequestEndpoints
{
    public sealed record CreateInput(Guid ProductId, int Quantity, string? Vehicle, string? CustomerName, string? Contact, string? PostalCode, string? Street, string? Number, string? Complement, string? District, string? City, string? State, string? Notes);
    public sealed record SupplierAnswer(string? ExactPart, bool Available, decimal? UnitPrice, int? LeadDays, decimal? WeightKg, decimal? HeightCm, decimal? WidthCm, decimal? LengthCm, string? Source);
    private static string Clean(string? value) => value?.Trim() ?? "";
    private static bool Text(string? value, int min, int max) => Clean(value).Length >= min && Clean(value).Length <= max;
    private static bool Origin(HttpContext c) => Uri.TryCreate(c.Request.Headers.Origin.ToString(), UriKind.Absolute, out var u) && u.Host.Equals(c.Request.Host.Host, StringComparison.OrdinalIgnoreCase) && u.Scheme == c.Request.Scheme && u.Port == (c.Request.Host.Port ?? (u.Scheme == "https" ? 443 : 80));
    private static Guid? UserId(HttpContext c) => Guid.TryParse(c.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private static object View(SupplierRequest r, bool admin = false) => new { r.Id, PublicId = $"57-{r.Id:N}", Status = r.Status.ToString(), r.CustomerName, r.CustomerEmail, r.Contact, r.PostalCode, r.Street, r.Number, r.Complement, r.District, r.City, r.State, r.Vehicle, r.Notes, r.CreatedAt, Items = r.Items.Select(i => new { i.Id, i.ProductId, i.Sku, i.Name, i.Quantity, i.ListedUnitPrice, ListedPriceSource = admin ? i.ListedPriceSource : null, ExactPart = admin ? i.ExactPart : null, Available = admin ? i.Available : null, UnitPrice = admin ? i.UnitPrice : null, LeadDays = admin ? i.LeadDays : null, WeightKg = admin ? i.WeightKg : null, HeightCm = admin ? i.HeightCm : null, WidthCm = admin ? i.WidthCm : null, LengthCm = admin ? i.LengthCm : null, Source = admin ? i.Source : null }) };
    private static string Csv(string? value)
    {
        var s = (value ?? "").Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        if (s.TrimStart().StartsWith('=') || s.TrimStart().StartsWith('+') || s.TrimStart().StartsWith('-') || s.TrimStart().StartsWith('@')) s = "'" + s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }
    public static void MapSupplierRequests(this IEndpointRouteBuilder app)
    {
        var customer = app.MapGroup("/api/v1/supplier-requests").RequireAuthorization();
        customer.MapPost("", async (HttpContext c, CreateInput input, StoreDbContext db, CancellationToken ct) => {
            if (!Origin(c)) return Results.StatusCode(403);
            var userId = UserId(c); if (userId is null) return Results.Unauthorized();
            var key = c.Request.Headers["Idempotency-Key"].ToString();
            if (key.Length is < 16 or > 100 || !key.All(x => char.IsAsciiLetterOrDigit(x) || x is '-' or '_')) return Results.BadRequest(new { message = "Chave de operação inválida." });
            if (input.ProductId == Guid.Empty || input.Quantity is < 1 or > 99 || !Text(input.Vehicle, 3, 240) || !Text(input.CustomerName, 2, 160) || !Regex.IsMatch(Clean(input.Contact), @"^\+?[0-9 ()-]{10,20}$") || Regex.Replace(Clean(input.Contact), "[^0-9]", "").Length is < 10 or > 13 || !Regex.IsMatch(Clean(input.PostalCode), "^[0-9]{8}$") || !Text(input.Street, 2, 160) || !Text(input.Number, 1, 20) || !Text(input.District, 2, 100) || !Text(input.City, 2, 100) || !Regex.IsMatch(Clean(input.State), "^[A-Za-z]{2}$") || Clean(input.Complement).Length > 100 || Clean(input.Notes).Length > 1000)
                return Results.ValidationProblem(new Dictionary<string,string[]> { ["form"] = ["Confira peça, quantidade, veículo, contato e endereço completo."] });
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, ct);
            if (user?.Email is null) return Results.Unauthorized();
            var normalized = input with { Vehicle = Clean(input.Vehicle), CustomerName = Clean(input.CustomerName), Contact = Clean(input.Contact), PostalCode = Clean(input.PostalCode), Street = Clean(input.Street), Number = Clean(input.Number), Complement = Clean(input.Complement), District = Clean(input.District), City = Clean(input.City), State = Clean(input.State).ToUpperInvariant(), Notes = Clean(input.Notes) };
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(normalized))));
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({userId.Value.ToString()}))", ct);
            var prior = await db.SupplierRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.CustomerId == userId && x.IdempotencyKey == key, ct);
            if (prior is not null) return prior.RequestHash == hash ? Results.Ok(View(prior)) : Results.Conflict(new { message = "Chave já usada em outro pedido." });
            var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == input.ProductId && (x.CatalogVisible || x.Published), ct);
            if (product is null) return Results.NotFound();
            var verifiedPrice = product.Published && product.Price > 0 && ProductPublicationPolicy.ValidReference(product.PriceSourceReference);
            var request = new SupplierRequest { CustomerId = userId.Value, IdempotencyKey = key, RequestHash = hash, CustomerName = normalized.CustomerName!, CustomerEmail = user.Email, Contact = normalized.Contact!, PostalCode = normalized.PostalCode!, Street = normalized.Street!, Number = normalized.Number!, Complement = normalized.Complement, District = normalized.District!, City = normalized.City!, State = normalized.State!, Vehicle = normalized.Vehicle!, Notes = normalized.Notes, Items = [new SupplierRequestItem { ProductId = product.Id, Sku = product.Sku, Name = product.Name, Quantity = input.Quantity, ListedUnitPrice = verifiedPrice ? product.Price : null, ListedPriceSource = verifiedPrice ? product.PriceSourceReference!.Trim() : null }] };
            db.SupplierRequests.Add(request); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/supplier-requests/{request.Id}", View(request));
        });
        customer.MapGet("", async (HttpContext c, StoreDbContext db, CancellationToken ct) => UserId(c) is { } id ? Results.Ok((await db.SupplierRequests.AsNoTracking().Include(x => x.Items).Where(x => x.CustomerId == id).OrderByDescending(x => x.CreatedAt).ToListAsync(ct)).Select(x => View(x))) : Results.Unauthorized());
        customer.MapGet("/{id:guid}", async (Guid id, HttpContext c, StoreDbContext db, CancellationToken ct) => UserId(c) is { } userId && await db.SupplierRequests.AsNoTracking().Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == userId, ct) is { } request ? Results.Ok(View(request)) : Results.NotFound());

        var admin = app.MapGroup("/api/v1/admin/supplier-requests").RequireAuthorization(p => p.RequireRole("Administrator"));
        admin.MapGet("", async (StoreDbContext db, CancellationToken ct) => Results.Ok((await db.SupplierRequests.AsNoTracking().Include(x => x.Items).OrderByDescending(x => x.CreatedAt).ToListAsync(ct)).Select(x => View(x, true))));
        admin.MapGet("/{id:guid}", async (Guid id, StoreDbContext db, CancellationToken ct) => await db.SupplierRequests.AsNoTracking().Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct) is { } request ? Results.Ok(View(request, true)) : Results.NotFound());
        admin.MapGet("/export.csv", async (StoreDbContext db, CancellationToken ct) => {
            var requests = await db.SupplierRequests.AsNoTracking().Include(x => x.Items).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
            var lines = new List<string> { "pedido,status,data,nome,email,contato,cep,logradouro,numero,complemento,bairro,cidade,uf,veiculo,observacoes,sku,peca,quantidade" };
            foreach (var r in requests) foreach (var i in r.Items) lines.Add(string.Join(',', new[] { $"57-{r.Id:N}", r.Status.ToString(), r.CreatedAt.ToString("O"), r.CustomerName, r.CustomerEmail, r.Contact, r.PostalCode, r.Street, r.Number, r.Complement, r.District, r.City, r.State, r.Vehicle, r.Notes, i.Sku, i.Name, i.Quantity.ToString() }.Select(Csv)));
            return Results.File(Encoding.UTF8.GetBytes("\uFEFF" + string.Join("\r\n", lines) + "\r\n"), "text/csv; charset=utf-8", "pedidos-fornecedor.csv");
        });
        admin.MapPatch("/{id:guid}/answer", async (Guid id, HttpContext c, SupplierAnswer answer, StoreDbContext db, CancellationToken ct) => {
            if (!Origin(c)) return Results.StatusCode(403);
            var request = await db.SupplierRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (request is null) return Results.NotFound();
            if (request.Status != SupplierRequestStatus.AwaitingSupplier) return Results.Conflict(new { message = "Pedido já respondido." });
            if (!Text(answer.Source, 5, 500) || (answer.Available && (!Text(answer.ExactPart, 3, 240) || answer.UnitPrice is null or < 0 || answer.LeadDays is null or < 0 or > 365 || answer.WeightKg is null or <= 0 || answer.HeightCm is null or <= 0 || answer.WidthCm is null or <= 0 || answer.LengthCm is null or <= 0))) return Results.ValidationProblem(new Dictionary<string,string[]> { ["answer"] = ["Informe fonte e, se disponível, peça exata, preço, prazo e dimensões válidos."] });
            var item = request.Items.Single(); item.Source = Clean(answer.Source); item.Available = answer.Available;
            if (answer.Available) { item.ExactPart = Clean(answer.ExactPart); item.UnitPrice = answer.UnitPrice; item.LeadDays = answer.LeadDays; item.WeightKg = answer.WeightKg; item.HeightCm = answer.HeightCm; item.WidthCm = answer.WidthCm; item.LengthCm = answer.LengthCm; }
            request.Status = answer.Available ? SupplierRequestStatus.Quoted : SupplierRequestStatus.Declined;
            await db.SaveChangesAsync(ct); return Results.Ok(View(request, true));
        });
    }
}
