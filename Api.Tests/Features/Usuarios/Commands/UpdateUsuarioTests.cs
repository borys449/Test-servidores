using Api.Application.Features.Usuarios.UpdateUsuario;
using Api.Tests.Configuration;
using Api.Tests.Factories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Api.Tests.Features.Usuarios.Commands;

public class UpdateUsuarioTests : FeatureTestBase
{
    public UpdateUsuarioTests(TestDatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task UpdateUsuario_WithValidData_ShouldUpdateUsuarioInDatabase()
    {
        // Arrange - Crear un usuario primero
        var createCommand = UsuarioFactory.CreateValidCommand();
        var createResult = await SendAsync(createCommand);
        createResult.IsSuccess.ShouldBeTrue();
        var usuarioId = createResult.Value;

        // Act - Actualizar sus datos
        var updateCommand = new UpdateUsuarioCommand(
            usuarioId,
            "Carlos Nuevo",
            "Gómez Nuevo",
            "carlos.nuevo@example.com"
        );
        var updateResult = await SendAsync(updateCommand);

        // Assert
        updateResult.IsSuccess.ShouldBeTrue();

        // Verificar en BD limpia
        await ExecuteDbContextAsync(async dbContext =>
        {
            var usuarioDb = await dbContext.Usuarios.SingleOrDefaultAsync(u => u.Id == usuarioId);
            usuarioDb.ShouldNotBeNull();
            usuarioDb.Nombre.ShouldBe("Carlos Nuevo");
            usuarioDb.Apellido.ShouldBe("Gómez Nuevo");
            usuarioDb.Email.Value.ShouldBe("carlos.nuevo@example.com");
        });
    }

    [Fact]
    public async Task UpdateUsuario_WhenUsuarioDoesNotExist_ShouldReturnFailureResult()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateCommand = new UpdateUsuarioCommand(
            nonExistentId,
            "Nombre",
            "Apellido",
            "email@example.com"
        );

        // Act
        var result = await SendAsync(updateCommand);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Usuario.NotFound");
    }

    [Fact]
    public async Task UpdateUsuario_WithEmptyId_ShouldThrowValidationException()
    {
        // Arrange
        var command = new UpdateUsuarioCommand(
            Guid.Empty,
            "Nombre",
            "Apellido",
            "email@example.com"
        );

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(() => SendAsync(command));
        exception.Errors.ShouldContain(e => e.ErrorMessage == "El identificador del usuario es requerido.");
    }

    [Fact]
    public async Task UpdateUsuario_WithNombre101Characters_ShouldThrowValidationException()
    {
        // Arrange
        var createCommand = UsuarioFactory.CreateValidCommand();
        var createResult = await SendAsync(createCommand);
        var usuarioId = createResult.Value;

        var longNombre = new string('A', 101);
        var command = new UpdateUsuarioCommand(
            usuarioId,
            longNombre,
            "Apellido",
            "email@example.com"
        );

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(() => SendAsync(command));
        exception.Errors.ShouldContain(e => e.ErrorMessage == "El nombre no puede superar los 100 caracteres.");
    }

    [Theory]
    [InlineData("email-invalido")]
    [InlineData("test@sin-dominio")]
    public async Task UpdateUsuario_WithInvalidEmail_ShouldThrowValidationException(string invalidEmail)
    {
        // Arrange
        var createCommand = UsuarioFactory.CreateValidCommand();
        var createResult = await SendAsync(createCommand);
        var usuarioId = createResult.Value;

        var command = new UpdateUsuarioCommand(
            usuarioId,
            "Nombre",
            "Apellido",
            invalidEmail
        );

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(() => SendAsync(command));
        exception.Errors.ShouldContain(e => e.ErrorMessage == "El formato del correo electrónico es inválido.");
    }
}
