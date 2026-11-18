using Microsoft.AspNetCore.Identity;
namespace Brasil57.Infrastructure;
public sealed class StoreUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = "";
    public string? Cpf { get; set; }
}
