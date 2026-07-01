using Api.Application.Common;
using Api.Infrastructure.Data;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Xunit;

namespace Api.Tests.Configuration;

public class TestDatabaseFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private string? _connectionString;
    private Respawner? _respawner;
    private ServiceProvider? _serviceProvider;

    public IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("Fixture not initialized.");

    public async Task InitializeAsync()
    {
        // 1. Iniciar el AppHost de pruebas (que solo levanta la base de datos)
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Api_Tests_AppHost>();
        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        // 2. Obtener la cadena de conexión del recurso de base de datos "bd"
        _connectionString = await _app.GetConnectionStringAsync("bd");
        if (string.IsNullOrEmpty(_connectionString))
        {
            throw new InvalidOperationException("No se pudo obtener la cadena de conexión de la base de datos.");
        }

        // 3. Configurar el contenedor de dependencias (DI) para las pruebas
        var services = new ServiceCollection();

        services.AddLogging(); // Agregar logging para el LoggingBehavior

        // Registrar DbContext apuntando al SQL Server del AppHost
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(_connectionString));

        // Registrar MediatR y el comportamiento de validación (usando el Assembly de la API)
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Api.Application.Common.Result).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // Registrar los validadores de FluentValidation del mismo Assembly
        services.AddValidatorsFromAssembly(typeof(Api.Application.Common.Result).Assembly);

        _serviceProvider = services.BuildServiceProvider();

        // 4. Esperar a que el servidor de SQL Server esté listo para aceptar conexiones
        await WaitForDatabaseReadyAsync();

        // 5. Crear el esquema de la base de datos
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        // 6. Inicializar Respawn para la limpieza rápida entre pruebas
        _respawner = await Respawner.CreateAsync(_connectionString, new RespawnerOptions
        {
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
    }

    private async Task WaitForDatabaseReadyAsync()
    {
        using var scope = _serviceProvider!.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var retries = 30;
        var delay = TimeSpan.FromSeconds(1);

        for (int i = 0; i < retries; i++)
        {
            try
            {
                await context.Database.CanConnectAsync();
                return;
            }
            catch
            {
                if (i == retries - 1)
                {
                    throw;
                }
                await Task.Delay(delay);
            }
        }
    }

    public async Task ResetDatabaseAsync()
    {
        if (_connectionString != null && _respawner != null)
        {
            await _respawner.ResetAsync(_connectionString);
        }
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider != null)
        {
            await _serviceProvider.DisposeAsync();
        }

        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }
}
