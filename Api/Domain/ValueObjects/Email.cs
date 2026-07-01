using System.Text.RegularExpressions;
using Vogen;

namespace Api.Domain.ValueObjects;

[ValueObject<string>(conversions: Conversions.EfCoreValueConverter)]
public partial struct Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static string NormalizeInput(string value) =>
        value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("El correo electrónico no puede estar vacío.");

        if (!EmailRegex.IsMatch(value))
            return Validation.Invalid("El formato del correo electrónico es inválido.");

        return Validation.Ok;
    }
}
