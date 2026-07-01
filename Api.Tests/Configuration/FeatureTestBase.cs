using Api.Infrastructure.Data;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Tests.Configuration;

public abstract class FeatureTestBase : IClassFixture<TestDatabaseFixture>, IAsyncLifetime
{
    protected readonly TestDatabaseFixture Fixture;

    protected FeatureTestBase(TestDatabaseFixture fixture)
    {
        Fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Reiniciar la base de datos antes de cada prueba usando Respawn
        await Fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Envía un request a través del pipeline de MediatR en un contenedor scoped.
    /// </summary>
    protected async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = Fixture.ServiceProvider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send(request);
    }

    /// <summary>
    /// Ejecuta una acción en un DbContext scoped limpio para verificar persistencia.
    /// </summary>
    protected async Task ExecuteDbContextAsync(Func<ApplicationDbContext, Task> action)
    {
        using var scope = Fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await action(context);
    }

    /// <summary>
    /// Ejecuta una función en un DbContext scoped limpio y retorna el resultado.
    /// </summary>
    protected async Task<TResult> ExecuteDbContextAsync<TResult>(Func<ApplicationDbContext, Task<TResult>> action)
    {
        using var scope = Fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(context);
    }
}
