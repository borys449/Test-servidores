using Api.Application.Features.Usuarios.GetUsuarioById;
using Api.Tests.Configuration;
using Api.Tests.Factories;
using FluentValidation;
using Shouldly;
using Xunit;

namespace Api.Tests.Features.Usuarios.Queries;

public class GetUsuarioByIdTests : FeatureTestBase
{
    public GetUsuarioByIdTests(TestDatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task GetUsuarioById_WithValidId_ShouldReturnUsuarioDetails()
    {
        // Arrange: Crear un usuario usando el factory de comandos
        var command = UsuarioFactory.CreateValidCommand();
        var createResult = await SendAsync(command);
        createResult.IsSuccess.ShouldBeTrue();
        var usuarioId = createResult.Value;

        // Act: Consultar el usuario creado por su Id
        var query = new GetUsuarioByIdQuery(usuarioId);
        var result = await SendAsync(query);

        // Assert: Validar los detalles del usuario
        result.IsSuccess.ShouldBeTrue();
        var usuario = result.Value;
        usuario.Id.ShouldBe(usuarioId);
        usuario.Nombre.ShouldBe(command.Nombre);
        usuario.Apellido.ShouldBe(command.Apellido);
        usuario.Email.ShouldBe(command.Email.Trim().ToLowerInvariant());
    }

    [Fact]
    public async Task GetUsuarioById_WhenUsuarioDoesNotExist_ShouldReturnFailureResult()
    {
        // Arrange: Generar un Id aleatorio que no existe en BD
        var nonExistentId = Guid.NewGuid();
        var query = new GetUsuarioByIdQuery(nonExistentId);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Usuario.NotFound");
        result.Error.Message.ShouldBe($"El usuario con Id '{nonExistentId}' no fue encontrado.");
    }

    [Fact]
    public async Task GetUsuarioById_WithEmptyGuid_ShouldThrowValidationException()
    {
        // Arrange: Usar un Guid vacío
        var query = new GetUsuarioByIdQuery(Guid.Empty);

        // Act & Assert: Verificar que arroja error de validación
        var exception = await Should.ThrowAsync<ValidationException>(() => SendAsync(query));
        exception.Errors.ShouldContain(e => e.ErrorMessage == "El identificador del usuario es requerido.");
    }
}
