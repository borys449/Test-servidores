using Api.Application.Features.Usuarios.DeleteUsuario;
using Api.Application.Features.Usuarios.GetUsuarioById;
using Api.Tests.Configuration;
using Api.Tests.Factories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Api.Tests.Features.Usuarios.Commands;

public class DeleteUsuarioTests : FeatureTestBase
{
    public DeleteUsuarioTests(TestDatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task DeleteUsuario_WithValidId_ShouldSoftDeleteUsuarioAndNotBeQueryable()
    {
        // Arrange - Crear un usuario
        var createCommand = UsuarioFactory.CreateValidCommand();
        var createResult = await SendAsync(createCommand);
        createResult.IsSuccess.ShouldBeTrue();
        var usuarioId = createResult.Value;

        // Act - Eliminar el usuario (Soft Delete)
        var deleteCommand = new DeleteUsuarioCommand(usuarioId);
        var deleteResult = await SendAsync(deleteCommand);

        // Assert
        deleteResult.IsSuccess.ShouldBeTrue();

        // 1. Validar que las consultas normales ya no devuelven al usuario (debido al Query Filter de EF Core)
        await ExecuteDbContextAsync(async dbContext =>
        {
            var usuarioDb = await dbContext.Usuarios.SingleOrDefaultAsync(u => u.Id == usuarioId);
            usuarioDb.ShouldBeNull();
        });

        // 2. Validar que el pipeline de MediatR para consultar el ID da NotFound
        var queryResult = await SendAsync(new GetUsuarioByIdQuery(usuarioId));
        queryResult.IsFailure.ShouldBeTrue();
        queryResult.Error.Code.ShouldBe("Usuario.NotFound");

        // 3. Validar en base de datos saltando el filtro que los datos siguen ahí pero marcados como IsDeleted
        await ExecuteDbContextAsync(async dbContext =>
        {
            var rawUsuarioDb = await dbContext.Usuarios
                .IgnoreQueryFilters() // Ignora el query filter global
                .SingleOrDefaultAsync(u => u.Id == usuarioId);

            rawUsuarioDb.ShouldNotBeNull();
            rawUsuarioDb.IsDeleted.ShouldBeTrue();
            rawUsuarioDb.DeletedAt.ShouldNotBeNull();
            rawUsuarioDb.DeletedAt.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        });
    }

    [Fact]
    public async Task DeleteUsuario_WhenUsuarioDoesNotExist_ShouldReturnFailureResult()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var deleteCommand = new DeleteUsuarioCommand(nonExistentId);

        // Act
        var result = await SendAsync(deleteCommand);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Usuario.NotFound");
    }

    [Fact]
    public async Task DeleteUsuario_WithEmptyId_ShouldThrowValidationException()
    {
        // Arrange
        var command = new DeleteUsuarioCommand(Guid.Empty);

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(() => SendAsync(command));
        exception.Errors.ShouldContain(e => e.ErrorMessage == "El identificador del usuario es requerido.");
    }
}
