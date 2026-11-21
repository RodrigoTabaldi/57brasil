using System.Net;
using System.Net.Http.Json;
using Brasil57.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Brasil57.IntegrationTests;

public sealed class CustomerAddressTests
{
    [Fact]
    public async Task Addresses_are_validated_and_owned_by_the_authenticated_customer()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE")
            ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.UseSetting("ConnectionStrings:Default", connection);
            builder.UseSetting("DataProtection:KeyPath", Path.Combine(Path.GetTempPath(), $"brasil57-address-{Guid.NewGuid():N}"));
        });
        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<StoreDbContext>().Database.MigrateAsync();

        using var owner = factory.CreateClient();
        using var stranger = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await owner.GetAsync("/api/v1/account/addresses")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Post(owner, "/api/v1/account/register", new {
            fullName = "Cliente Teste", email = $"owner-{Guid.NewGuid():N}@example.test", phone = "11999999999",
            password = "StrongPassword1!", confirmPassword = "StrongPassword1!"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Post(stranger, "/api/v1/account/register", new {
            fullName = "Outro Cliente", email = $"stranger-{Guid.NewGuid():N}@example.test", phone = "11999999998",
            password = "StrongPassword1!", confirmPassword = "StrongPassword1!"
        })).StatusCode);

        var input = new { recipient = "Cliente Teste", phone = "11999999999", cep = "01001000", street = "Praça da Sé",
            number = "1", complement = "Sala 2", neighborhood = "Sé", city = "São Paulo", state = "SP" };
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(owner, "/api/v1/account/addresses", input with { cep = "123" })).StatusCode);
        var created = await Post(owner, "/api/v1/account/addresses", input);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var address = await created.Content.ReadFromJsonAsync<AddressResponse>();
        Assert.NotNull(address);
        Assert.Equal("01001000", address.Cep);
        Assert.Single((await owner.GetFromJsonAsync<List<AddressResponse>>("/api/v1/account/addresses"))!);
        Assert.Empty((await stranger.GetFromJsonAsync<List<AddressResponse>>("/api/v1/account/addresses"))!);

        using var foreignDelete = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/account/addresses/{address.Id}");
        foreignDelete.Headers.Add("Origin", "http://localhost");
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.SendAsync(foreignDelete)).StatusCode);
        using var update = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/account/addresses/{address.Id}") { Content = JsonContent.Create(input with { number = "2" }) };
        update.Headers.Add("Origin", "http://localhost");
        Assert.Equal(HttpStatusCode.OK, (await owner.SendAsync(update)).StatusCode);
        Assert.Equal("2", (await owner.GetFromJsonAsync<List<AddressResponse>>("/api/v1/account/addresses"))!.Single().Number);
    }

    static async Task<HttpResponseMessage> Post(HttpClient client, string path, object input)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(input) };
        request.Headers.Add("Origin", "http://localhost");
        return await client.SendAsync(request);
    }

    sealed record AddressResponse(Guid Id, string Cep, string Number);
}
