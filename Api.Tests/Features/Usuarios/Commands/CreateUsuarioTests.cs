using Api.Application.Features.Usuarios.CreateUsuario;
using Api.Tests.Configuration;
using Api.Tests.Factories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Api.Tests.Features.Usuarios.Commands;

public class CreateUsuarioTests : FeatureTestBase
{
    public CreateUsuarioTests(TestDatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task CreateUsuario_WithValidData_ShouldCreateUsuarioInDatabase()
    {
        // Arrange
        var command = UsuarioFactory.CreateValidCommand();

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var usuarioId = result.Value;
        usuarioId.ShouldNotBe(Guid.Empty);

        await ExecuteDbContextAsync(async dbContext =>
        {
            var usuarioDb = await dbContext.Usuarios.SingleOrDefaultAsync(u => u.Id == usuarioId);
            usuarioDb.ShouldNotBeNull();
            usuarioDb.Nombre.ShouldBe(command.Nombre);
            usuarioDb.Apellido.ShouldBe(command.Apellido);
            usuarioDb.Email.Value.ShouldBe(command.Email.Trim().ToLowerInvariant());
        });
    }

    [Fact]
    public async Task CreateUsuario_WithNombreExactly100Characters_ShouldSucceed()
    {
        // Arrange (Límite máximo permitido de 100 caracteres)
        var command = UsuarioFactory.CreateCommandWithNombreOfLength(100);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await ExecuteDbContextAsync(async dbContext =>
        {
            var usuarioDb = await dbContext.Usuarios.SingleOrDefaultAsync(u => u.Id == result.Value);
            usuarioDb.ShouldNotBeNull();
            usuarioDb.Nombre.Length.ShouldBe(100);
        });
    }

    [Fact]
    public async Task CreateUsuario_WithNombre101Characters_ShouldThrowValidationException()
    {
        // Arrange (Excede el límite de 100 caracteres)
        var command = UsuarioFactory.CreateCommandWithNombreOfLength(101);

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(() => SendAsync(command));
        exception.Errors.ShouldContain(e => e.ErrorMessage == "El nombre no puede superar los 100 caracteres.");
    }

    [Fact]
    public async Task CreateUsuario_WithApellidoExactly100Characters_ShouldSucceed()
    {
        // Arrange (Límite máximo permitido de 100 caracteres)
        var command = UsuarioFactory.CreateCommandWithApellidoOfLength(100);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateUsuario_WithApellido101Characters_ShouldThrowValidationException()
    {
        // Arrange (Excede el límite de 100 caracteres)
        var command = UsuarioFactory.CreateCommandWithApellidoOfLength(101);

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(() => SendAsync(command));
        exception.Errors.ShouldContain(e => e.ErrorMessage == "El apellido no puede superar los 100 caracteres.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateUsuario_WithEmptyNombre_ShouldThrowValidationException(string emptyNombre)
    {
        // Arrange
        var command = new CreateUsuarioCommand(emptyNombre, "Pérez", "juan.perez@example.com");

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(() => SendAsync(command));
        exception.Errors.ShouldContain(e => e.ErrorMessage == "El nombre es requerido.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateUsuario_WithEmptyApellido_ShouldThrowValidationException(string emptyApellido)
    {
        // Arrange
        var command = new CreateUsuarioCommand("Juan", emptyApellido, "juan.perez@example.com");

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(() => SendAsync(command));
        exception.Errors.ShouldContain(e => e.ErrorMessage == "El apellido es requerido.");
    }

    [Theory]
    [InlineData("email-sin-arroba.com")]
    [InlineData("email@sin-punto")]
    [InlineData("@sin-usuario.com")]
    public async Task CreateUsuario_WithInvalidEmailFormat_ShouldThrowValidationException(string invalidEmail)
    {
        // Arrange
        var command = UsuarioFactory.CreateCommandWithEmail(invalidEmail);

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(() => SendAsync(command));
        exception.Errors.ShouldContain(e => e.ErrorMessage == "El formato del correo electrónico es inválido.");
    }

    [Fact]
    public async Task CreateUsuario_WithEmailContainingSpacesAndUppercase_ShouldNormalizeAndSucceed()
    {
        // Arrange
        var rawEmail = "  JUAN.perez@Example.COM  ";
        var command = UsuarioFactory.CreateCommandWithEmail(rawEmail);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await ExecuteDbContextAsync(async dbContext =>
        {
            var usuarioDb = await dbContext.Usuarios.SingleOrDefaultAsync(u => u.Id == result.Value);
            usuarioDb.ShouldNotBeNull();
            usuarioDb.Email.Value.ShouldBe("juan.perez@example.com"); // Debe estar normalizado y recortado
        });
    }
}
