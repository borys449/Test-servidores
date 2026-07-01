using Api.Application.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Api.Tests.Features.Common;

public class LoggingBehaviorTests
{
    private class TestLogger<T> : ILogger<T>
    {
        public List<string> Logs { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, 
            EventId eventId, 
            TState state, 
            Exception? exception, 
            Func<TState, Exception?, string> formatter)
        {
            Logs.Add($"{logLevel}: {formatter(state, exception)}");
        }
    }

    private record PingRequest : IRequest<string>;

    private class PingHandler : IRequestHandler<PingRequest, string>
    {
        public Task<string> Handle(PingRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult("Pong");
        }
    }

    private record FailRequest : IRequest<string>;

    private class FailHandler : IRequestHandler<FailRequest, string>
    {
        public Task<string> Handle(FailRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Fallo intencional");
        }
    }

    [Fact]
    public async Task LoggingBehavior_WhenRequestSucceeds_ShouldLogStartAndCompletionWithTime()
    {
        // Arrange
        var services = new ServiceCollection();
        var logger = new TestLogger<LoggingBehavior<PingRequest, string>>();
        
        services.AddSingleton<ILogger<LoggingBehavior<PingRequest, string>>>(logger);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(LoggingBehaviorTests).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new PingRequest());

        // Assert
        result.ShouldBe("Pong");
        logger.Logs.Count.ShouldBe(2);
        logger.Logs[0].ShouldContain("Iniciando solicitud: PingRequest");
        logger.Logs[1].ShouldContain("Solicitud completada exitosamente: PingRequest");
        logger.Logs[1].ShouldContain("ms)");
    }

    [Fact]
    public async Task LoggingBehavior_WhenRequestFails_ShouldLogErrorWithExceptionMessage()
    {
        // Arrange
        var services = new ServiceCollection();
        var logger = new TestLogger<LoggingBehavior<FailRequest, string>>();
        
        services.AddSingleton<ILogger<LoggingBehavior<FailRequest, string>>>(logger);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(LoggingBehaviorTests).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => mediator.Send(new FailRequest()));

        logger.Logs.Count.ShouldBe(2);
        logger.Logs[0].ShouldContain("Iniciando solicitud: FailRequest");
        logger.Logs[1].ShouldContain("Error en la ejecución de la solicitud: FailRequest");
        logger.Logs[1].ShouldContain("Fallo intencional");
        logger.Logs[1].ShouldContain("ms)");
    }
}
