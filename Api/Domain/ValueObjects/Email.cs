using System.Text.RegularExpressions;

namespace Api.Domain.ValueObjects;

public record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El correo electrónico no puede estar vacío.", nameof(value));

        if (!EmailRegex.IsMatch(value))
            throw new ArgumentException("El formato del correo electrónico es inválido.", nameof(value));

        Value = value.ToLowerInvariant();
    }

    public static Email Create(string value)
    {
        return new Email(value);
    }

    public override string ToString() => Value;
}
