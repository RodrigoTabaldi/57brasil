using System.Security.Claims;
using System.Net;
using System.Net.Mail;
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
    public static void MapCustomers(this IEndpointRouteBuilder app, IConfiguration config)
    {
        var group = app.MapGroup("/api/v1/account");
        group.MapPost("/register", async (HttpContext c, RegisterInput input, UserManager<StoreUser> users, SignInManager<StoreUser> signIn, CancellationToken ct) => {
            if (!SameOrigin(c)) return Results.StatusCode(403);
            var email = input.Email?.Trim().ToLowerInvariant(); var phone = PhoneDigits(input.Phone); var cpf = Digits(input.Cpf);
            if (input.FullName?.Trim().Length is < 2 or > 160 || string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) || phone.Length is < 10 or > 11 || (cpf.Length > 0 && !ValidCpf(cpf)) || input.Password != input.ConfirmPassword)
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
            if (input.FullName?.Trim().Length is < 2 or > 160 || phone.Length is < 10 or > 11 || (cpf.Length > 0 && !ValidCpf(cpf))) return Results.BadRequest(new { message = "Dados de perfil inválidos." });
            user.FullName = input.FullName!.Trim(); user.PhoneNumber = phone; user.Cpf = cpf.Length == 0 ? null : cpf;
            try { var result = await users.UpdateAsync(user); return result.Succeeded ? Results.Ok(Profile(user)) : Results.Conflict(new { message = "CPF já cadastrado." }); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "CPF já cadastrado." }); }
        }).RequireAuthorization();
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
public sealed record EmailInput(string Email);
public sealed record ResetInput(string Email, string Token, string Password);
