using Api.Application.Features.Usuarios.GetUsuarios;
using Api.Tests.Configuration;
using Api.Tests.Factories;
using FluentValidation;
using Shouldly;
using Xunit;

namespace Api.Tests.Features.Usuarios.Queries;

public class GetUsuariosTests : FeatureTestBase
{
    public GetUsuariosTests(TestDatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task GetUsuarios_WhenDatabaseIsEmpty_ShouldReturnZeroItems()
    {
        // Arrange
        var query = new GetUsuariosQuery(1, 10);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var pagedResult = result.Value;
        pagedResult.TotalCount.ShouldBe(0);
        pagedResult.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetUsuarios_WithValidPagination_ShouldReturnCorrectPageAndTotalCount()
    {
        // Arrange: Crear 15 usuarios en la base de datos
        var commandIds = new List<Guid>();
        for (int i = 0; i < 15; i++)
        {
            var result = await SendAsync(UsuarioFactory.CreateValidCommand());
            result.IsSuccess.ShouldBeTrue();
            commandIds.Add(result.Value);
        }

        // Act - Obtener Página 1 con tamaño 10
        var queryPage1 = new GetUsuariosQuery(1, 10);
        var resultPage1 = await SendAsync(queryPage1);

        // Assert Page 1
        resultPage1.IsSuccess.ShouldBeTrue();
        resultPage1.Value.TotalCount.ShouldBe(15);
        resultPage1.Value.Items.Count.ShouldBe(10);
        resultPage1.Value.PageNumber.ShouldBe(1);
        resultPage1.Value.PageSize.ShouldBe(10);

        // Act - Obtener Página 2 con tamaño 10
        var queryPage2 = new GetUsuariosQuery(2, 10);
        var resultPage2 = await SendAsync(queryPage2);

        // Assert Page 2
        resultPage2.IsSuccess.ShouldBeTrue();
        resultPage2.Value.TotalCount.ShouldBe(15);
        resultPage2.Value.Items.Count.ShouldBe(5);
        resultPage2.Value.PageNumber.ShouldBe(2);
        resultPage2.Value.PageSize.ShouldBe(10);

        // Act - Obtener Página 3 con tamaño 10 (fuera de los límites de datos existentes)
        var queryPage3 = new GetUsuariosQuery(3, 10);
        var resultPage3 = await SendAsync(queryPage3);

        // Assert Page 3
        resultPage3.IsSuccess.ShouldBeTrue();
        resultPage3.Value.TotalCount.ShouldBe(15);
        resultPage3.Value.Items.ShouldBeEmpty();
        resultPage3.Value.PageNumber.ShouldBe(3);
    }

    [Theory]
    [InlineData(0, 10, "El número de página debe ser mayor o igual a 1.")]
    [InlineData(-1, 10, "El número de página debe ser mayor o igual a 1.")]
    [InlineData(1, 0, "El tamaño de página debe ser mayor o igual a 1.")]
    [InlineData(1, -5, "El tamaño de página debe ser mayor o igual a 1.")]
    [InlineData(1, 101, "El tamaño de página no puede exceder 100 registros.")]
    public async Task GetUsuarios_WithInvalidPaginationParameters_ShouldThrowValidationException(
        int pageNumber, int pageSize, string expectedErrorMessage)
    {
        // Arrange
        var query = new GetUsuariosQuery(pageNumber, pageSize);

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(() => SendAsync(query));
        exception.Errors.ShouldContain(e => e.ErrorMessage == expectedErrorMessage);
    }
}
