using Api.Application.Features.Usuarios.CreateUsuario;
using Bogus;

namespace Api.Tests.Factories;

public static class UsuarioFactory
{
    private static readonly Faker FakerInstance = new();

    /// <summary>
    /// Crea un comando válido para crear un usuario con datos aleatorios de Bogus.
    /// </summary>
    public static CreateUsuarioCommand CreateValidCommand()
    {
        return new CreateUsuarioCommand(
            FakerInstance.Name.FirstName(),
            FakerInstance.Name.LastName(),
            FakerInstance.Internet.Email()
        );
    }

    /// <summary>
    /// Crea un comando con un Nombre vacío.
    /// </summary>
    public static CreateUsuarioCommand CreateCommandWithEmptyNombre()
    {
        return new CreateUsuarioCommand(
            string.Empty,
            FakerInstance.Name.LastName(),
            FakerInstance.Internet.Email()
        );
    }

    /// <summary>
    /// Crea un comando con un Apellido vacío.
    /// </summary>
    public static CreateUsuarioCommand CreateCommandWithEmptyApellido()
    {
        return new CreateUsuarioCommand(
            FakerInstance.Name.FirstName(),
            string.Empty,
            FakerInstance.Internet.Email()
        );
    }

    /// <summary>
    /// Crea un comando con un formato de email inválido.
    /// </summary>
    public static CreateUsuarioCommand CreateCommandWithInvalidEmail()
    {
        return new CreateUsuarioCommand(
            FakerInstance.Name.FirstName(),
            FakerInstance.Name.LastName(),
            "email-invalido"
        );
    }

    /// <summary>
    /// Crea un comando con un Nombre de longitud específica.
    /// </summary>
    public static CreateUsuarioCommand CreateCommandWithNombreOfLength(int length)
    {
        var nombre = new string('A', length);
        return new CreateUsuarioCommand(
            nombre,
            FakerInstance.Name.LastName(),
            FakerInstance.Internet.Email()
        );
    }

    /// <summary>
    /// Crea un comando con un Apellido de longitud específica.
    /// </summary>
    public static CreateUsuarioCommand CreateCommandWithApellidoOfLength(int length)
    {
        var apellido = new string('B', length);
        return new CreateUsuarioCommand(
            FakerInstance.Name.FirstName(),
            apellido,
            FakerInstance.Internet.Email()
        );
    }

    /// <summary>
    /// Crea un comando con un email específico.
    /// </summary>
    public static CreateUsuarioCommand CreateCommandWithEmail(string email)
    {
        return new CreateUsuarioCommand(
            FakerInstance.Name.FirstName(),
            FakerInstance.Name.LastName(),
            email
        );
    }
}
