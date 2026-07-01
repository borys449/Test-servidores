using Api.Application.Common;
using Api.Domain.Entities;
using Api.Domain.ValueObjects;
using Api.Infrastructure.Data;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Application.Features.Usuarios.CreateUsuario;

[ApiController]
[Route("api/usuarios")]
public class CreateUsuarioController : ControllerBase
{
    private readonly ISender _sender;

    public CreateUsuarioController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUsuarioCommand command,
        CancellationToken cancellationToken
    )
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { Error = result.Error.Code, Message = result.Error.Message });
        }

        return CreatedAtAction(nameof(Create), new { id = result.Value }, result.Value);
    }
}

public record CreateUsuarioCommand(string Nombre, string Apellido, string Email)
    : IRequest<Result<Guid>>;

public class CreateUsuarioCommandValidator : AbstractValidator<CreateUsuarioCommand>
{
    public CreateUsuarioCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty()
            .WithMessage("El nombre es requerido.")
            .MaximumLength(100)
            .WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.Apellido)
            .NotEmpty()
            .WithMessage("El apellido es requerido.")
            .MaximumLength(100)
            .WithMessage("El apellido no puede superar los 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El correo electrónico es requerido.")
            .EmailAddress()
            .WithMessage("El formato del correo electrónico es inválido.");
    }
}

public class CreateUsuarioCommandHandler : IRequestHandler<CreateUsuarioCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;

    public CreateUsuarioCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(
        CreateUsuarioCommand request,
        CancellationToken cancellationToken
    )
    {
        Email email;
        try
        {
            email = Email.From(request.Email);
        }
        catch (Exception ex)
        {
            return Result.Failure<Guid>(new Error("Usuario.InvalidEmail", ex.Message));
        }

        var usuario = new Usuario(Guid.NewGuid(), request.Nombre, request.Apellido, email);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(usuario.Id);
    }
}
