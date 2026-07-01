using Api.Domain.Entities;
using Api.Domain.ValueObjects;
using Bogus;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Asegura la creación de la base de datos y la creación del esquema
        await context.Database.EnsureCreatedAsync();

        if (!await context.Usuarios.AnyAsync())
        {
            var userFaker = new Faker<Usuario>().CustomInstantiator(f => new Usuario(
                Guid.NewGuid(),
                f.Name.FirstName(),
                f.Name.LastName(),
                Email.From(f.Internet.Email())
            ));

            var fakeUsers = userFaker.Generate(15);

            await context.Usuarios.AddRangeAsync(fakeUsers);
            await context.SaveChangesAsync();
        }
    }
}
