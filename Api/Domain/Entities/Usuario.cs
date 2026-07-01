using Api.Domain.ValueObjects;

namespace Api.Domain.Entities;

public class Usuario
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Apellido { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;

    // Constructor requerido por EF Core
    private Usuario() { }

    public Usuario(Guid id, string nombre, string apellido, Email email)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es requerido.", nameof(nombre));

        if (string.IsNullOrWhiteSpace(apellido))
            throw new ArgumentException("El apellido es requerido.", nameof(apellido));

        ArgumentNullException.ThrowIfNull(email, nameof(email));

        Id = id;
        Nombre = nombre;
        Apellido = apellido;
        Email = email;
    }

    public void UpdateInformation(string nombre, string apellido, Email email)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre no puede estar vacío.", nameof(nombre));

        if (string.IsNullOrWhiteSpace(apellido))
            throw new ArgumentException("El apellido no puede estar vacío.", nameof(apellido));

        ArgumentNullException.ThrowIfNull(email, nameof(email));

        Nombre = nombre;
        Apellido = apellido;
        Email = email;
    }
}
