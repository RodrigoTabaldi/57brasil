using System.Security.Claims;
using System.Net;
using System.Net.Mail;
using Brasil57.Domain;
using Brasil57.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Brasil57.Api;

public static class CustomerEndpoints
{
    static bool SameOrigin(HttpContext c) => Uri.TryCreate(c.Request.Headers.Origin.ToString(), UriKind.Absolute, out var u) && u.Host.Equals(c.Request.Host.Host, StringComparison.OrdinalIgnoreCase) && u.Scheme == c.Request.Scheme && u.Port == (c.Request.Host.Port ?? (u.Scheme == "https" ? 443 : 80));
    static object Profile(StoreUser u) => new { u.Id, u.FullName, u.Email, u.PhoneNumber, u.Cpf };
    static string Digits(string? value) => new((value ?? "").Where(char.IsDigit).ToArray());
    static string PhoneDigits(string? value)
    {
        var digits = Digits(value);
        return digits.Length is 12 or 13 && digits.StartsWith("55", StringComparison.Ordinal) ? digits[2..] : digits;
    }
    static bool ValidCpf(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1) return false;
        for (var n = 9; n <= 10; n++) { var sum = 0; for (var i = 0; i < n; i++) sum += (cpf[i] - '0') * (n + 1 - i); var check = (sum * 10) % 11; if (check == 10) check = 0; if (check != cpf[n] - '0') return false; }
        return true;
    }
    static readonly HashSet<string> States = ["AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"];
    static CustomerAddress? Address(Guid userId, AddressInput input)
    {
        var recipient = input.Recipient?.Trim(); var phone = PhoneDigits(input.Phone); var cep = input.Cep?.Trim().Replace("-", "");
        var street = input.Street?.Trim(); var number = input.Number?.Trim(); var complement = input.Complement?.Trim();
        var neighborhood = input.Neighborhood?.Trim(); var city = input.City?.Trim(); var state = input.State?.Trim().ToUpperInvariant();
        if (recipient is null || recipient.Length is < 2 or > 160 || phone.Length is < 10 or > 11 || cep is null || cep.Length != 8 || !cep.All(c => c is >= '0' and <= '9') ||
            street is null || street.Length is < 2 or > 160 || number is null || number.Length is < 1 or > 20 ||
            complement?.Length > 120 || neighborhood is null || neighborhood.Length is < 2 or > 120 ||
            city is null || city.Length is < 2 or > 120 || state is null || !States.Contains(state)) return null;
        return new CustomerAddress { UserId = userId, Recipient = recipient, Phone = phone, Cep = cep, Street = street,
            Number = number, Complement = string.IsNullOrEmpty(complement) ? null : complement,
            Neighborhood = neighborhood, City = city, State = state };
    }
    static object AddressView(CustomerAddress a) => new { a.Id, a.Recipient, a.Phone, a.Cep, a.Street, a.Number, a.Complement, a.Neighborhood, a.City, a.State };
    public static void MapCustomers(this IEndpointRouteBuilder app, IConfiguration config)
    {
        var group = app.MapGroup("/api/v1/account");
        group.MapPost("/register", async (HttpContext c, RegisterInput input, UserManager<StoreUser> users, SignInManager<StoreUser> signIn, CancellationToken ct) => {
            if (!SameOrigin(c)) return Results.StatusCode(403);
            var email = input.Email?.Trim().ToLowerInvariant(); var phone = PhoneDigits(input.Phone); var cpf = Digits(input.Cpf);
            if (string.IsNullOrWhiteSpace(input.FullName) || input.FullName.Trim().Length is < 2 or > 160 || string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) || phone.Length is < 10 or > 11 || (cpf.Length > 0 && !ValidCpf(cpf)) || input.Password != input.ConfirmPassword)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["form"] = ["Confira nome, e-mail, telefone, CPF e confirmação da senha."] });
            var user = new StoreUser { UserName = email, Email = email, FullName = input.FullName!.Trim(), PhoneNumber = phone, Cpf = cpf.Length == 0 ? null : cpf };
            try { var result = await users.CreateAsync(user, input.Password ?? ""); if (!result.Succeeded) return Results.ValidationProblem(new Dictionary<string, string[]> { ["form"] = [result.Errors.Any(x => x.Code.Contains("Password")) ? "A senha deve ter 12 caracteres, maiúscula, minúscula, número e símbolo." : "E-mail ou CPF já cadastrado."] }); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "E-mail ou CPF já cadastrado." }); }
            await signIn.SignInAsync(user, false); return Results.Ok(Profile(user));
        }).RequireRateLimiting("account");
        group.MapPost("/login", async (HttpContext c, LoginInput input, UserManager<StoreUser> users, SignInManager<StoreUser> signIn) => {
            if (!SameOrigin(c)) return Results.StatusCode(403);
            var user = await users.FindByEmailAsync(input.Email?.Trim().ToLowerInvariant() ?? "");
            if (user is null) return Results.Unauthorized();
            var result = await signIn.CheckPasswordSignInAsync(user, input.Password ?? "", true);
            if (!result.Succeeded) return Results.Unauthorized();
            await signIn.SignInAsync(user, false); return Results.Ok(Profile(user));
        }).RequireRateLimiting("account");
        group.MapGet("/me", async (ClaimsPrincipal principal, UserManager<StoreUser> users) => { var user = await users.GetUserAsync(principal); return user is null ? Results.Unauthorized() : Results.Ok(Profile(user)); }).RequireAuthorization();
        group.MapPatch("/me", async (HttpContext c, ClaimsPrincipal principal, ProfileInput input, UserManager<StoreUser> users) => {
            if (!SameOrigin(c)) return Results.StatusCode(403);
            var user = await users.GetUserAsync(principal); if (user is null) return Results.Unauthorized();
            var phone = PhoneDigits(input.Phone); var cpf = Digits(input.Cpf);
            if (string.IsNullOrWhiteSpace(input.FullName) || input.FullName.Trim().Length is < 2 or > 160 || phone.Length is < 10 or > 11 || (cpf.Length > 0 && !ValidCpf(cpf))) return Results.BadRequest(new { message = "Dados de perfil inválidos." });
            user.FullName = input.FullName!.Trim(); user.PhoneNumber = phone; user.Cpf = cpf.Length == 0 ? null : cpf;
            try { var result = await users.UpdateAsync(user); return result.Succeeded ? Results.Ok(Profile(user)) : Results.Conflict(new { message = "CPF já cadastrado." }); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "CPF já cadastrado." }); }
        }).RequireAuthorization();
        group.MapGet("/addresses", async (ClaimsPrincipal principal, UserManager<StoreUser> users, StoreDbContext db, CancellationToken ct) => {
            var user = await users.GetUserAsync(principal); if (user is null) return Results.Unauthorized();
            var addresses = await db.CustomerAddresses.AsNoTracking().Where(x => x.UserId == user.Id).OrderBy(x => x.Street).ToListAsync(ct);
            return Results.Ok(addresses.Select(AddressView));
        }).RequireAuthorization();
        group.MapPost("/addresses", async (HttpContext c, AddressInput input, UserManager<StoreUser> users, StoreDbContext db, CancellationToken ct) => {
            if (!SameOrigin(c)) return Results.StatusCode(403);
            var user = await users.GetUserAsync(c.User); if (user is null) return Results.Unauthorized();
            var address = Address(user.Id, input);
            if (address is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["address"] = ["Confira destinatário, telefone, CEP, logradouro, número, bairro, cidade e UF."] });
            db.CustomerAddresses.Add(address);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/account/addresses/{address.Id}", AddressView(address));
        }).RequireAuthorization().RequireRateLimiting("account");
        group.MapPut("/addresses/{id:guid}", async (Guid id, HttpContext c, AddressInput input, UserManager<StoreUser> users, StoreDbContext db, CancellationToken ct) => {
            if (!SameOrigin(c)) return Results.StatusCode(403);
            var user = await users.GetUserAsync(c.User); if (user is null) return Results.Unauthorized();
            var updated = Address(user.Id, input);
            if (updated is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["address"] = ["Confira os dados do endereço."] });
            var address = await db.CustomerAddresses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id, ct);
            if (address is null) return Results.NotFound();
            address.Recipient = updated.Recipient; address.Phone = updated.Phone; address.Cep = updated.Cep;
            address.Street = updated.Street; address.Number = updated.Number; address.Complement = updated.Complement;
            address.Neighborhood = updated.Neighborhood; address.City = updated.City; address.State = updated.State;
            await db.SaveChangesAsync(ct);
            return Results.Ok(AddressView(address));
        }).RequireAuthorization().RequireRateLimiting("account");
        group.MapDelete("/addresses/{id:guid}", async (Guid id, HttpContext c, UserManager<StoreUser> users, StoreDbContext db, CancellationToken ct) => {
            if (!SameOrigin(c)) return Results.StatusCode(403);
            var user = await users.GetUserAsync(c.User); if (user is null) return Results.Unauthorized();
            var address = await db.CustomerAddresses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id, ct);
            if (address is null) return Results.NotFound();
            db.CustomerAddresses.Remove(address);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting("account");
        group.MapPost("/logout", async (HttpContext c, SignInManager<StoreUser> signIn) => { if (!SameOrigin(c)) return Results.StatusCode(403); await signIn.SignOutAsync(); return Results.NoContent(); }).RequireAuthorization();
        group.MapPost("/forgot-password", async (HttpContext c, EmailInput input, UserManager<StoreUser> users) => {
            if (!SameOrigin(c)) return Results.StatusCode(403);
            if (string.IsNullOrWhiteSpace(config["Smtp:Host"]) || string.IsNullOrWhiteSpace(config["Smtp:From"]) ||
                !Uri.TryCreate(config["Storefront:Url"], UriKind.Absolute, out var storefrontUrl) ||
                (!c.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment() && storefrontUrl.Scheme != Uri.UriSchemeHttps))
                return Results.Problem("Recuperação de senha indisponível.", statusCode: 503);
            var user = await users.FindByEmailAsync(input.Email?.Trim().ToLowerInvariant() ?? "");
            if (user is not null) { var token = await users.GeneratePasswordResetTokenAsync(user); var link = $"{storefrontUrl.ToString().TrimEnd('/')}/conta#email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";
                using var message = new MailMessage(config["Smtp:From"]!, user.Email!, "Redefinir senha - 57 Brasil", $"Para redefinir sua senha, acesse: {link}");
                using var smtp = new SmtpClient(config["Smtp:Host"], int.TryParse(config["Smtp:Port"], out var port) ? port : 587) { EnableSsl = true, Credentials = new NetworkCredential(config["Smtp:User"], config["Smtp:Password"]) };
                try { await smtp.SendMailAsync(message); } catch (SmtpException) { return Results.Problem("Recuperação de senha indisponível.", statusCode: 503); }
            }
            return Results.Accepted();
        }).RequireRateLimiting("account");
        group.MapPost("/reset-password", async (HttpContext c, ResetInput input, UserManager<StoreUser> users) => {
            if (!SameOrigin(c)) return Results.StatusCode(403);
            var user = await users.FindByEmailAsync(input.Email?.Trim().ToLowerInvariant() ?? ""); if (user is null) return Results.BadRequest(new { message = "Link inválido ou expirado." });
            var result = await users.ResetPasswordAsync(user, input.Token ?? "", input.Password ?? ""); if (!result.Succeeded) return Results.BadRequest(new { message = "Link inválido ou senha fora dos requisitos." });
            await users.UpdateSecurityStampAsync(user); return Results.NoContent();
        }).RequireRateLimiting("account");
    }
}
public sealed record RegisterInput(string FullName, string Email, string Phone, string? Cpf, string Password, string ConfirmPassword);
public sealed record LoginInput(string Email, string Password);
public sealed record ProfileInput(string FullName, string Phone, string? Cpf);
public sealed record AddressInput(string? Recipient, string? Phone, string? Cep, string? Street, string? Number, string? Complement, string? Neighborhood, string? City, string? State);
public sealed record EmailInput(string Email);
public sealed record ResetInput(string Email, string Token, string Password);
