using Api.Application.Common;
using Api.Domain.ValueObjects;
using Api.Infrastructure.Data;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Features.Usuarios.UpdateUsuario;

[ApiController]
[Route("api/usuarios")]
public class UpdateUsuarioController : ControllerBase
{
    private readonly ISender _sender;

    public UpdateUsuarioController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateUsuarioRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new UpdateUsuarioCommand(id, request.Nombre, request.Apellido, request.Email);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Usuario.NotFound")
            {
                return NotFound(new { Error = result.Error.Code, Message = result.Error.Message });
            }
            return BadRequest(new { Error = result.Error.Code, Message = result.Error.Message });
        }

        return NoContent();
    }
}

public record UpdateUsuarioRequest(string Nombre, string Apellido, string Email);

public record UpdateUsuarioCommand(Guid Id, string Nombre, string Apellido, string Email) : IRequest<Result>;

public class UpdateUsuarioCommandValidator : AbstractValidator<UpdateUsuarioCommand>
{
    public UpdateUsuarioCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El identificador del usuario es requerido.");

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

public class UpdateUsuarioCommandHandler : IRequestHandler<UpdateUsuarioCommand, Result>
{
    private readonly ApplicationDbContext _context;

    public UpdateUsuarioCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateUsuarioCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _context.Usuarios
            .SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (usuario is null)
        {
            return Result.Failure(
                new Error("Usuario.NotFound", $"El usuario con Id '{request.Id}' no fue encontrado.")
            );
        }

        Email email;
        try
        {
            email = Email.From(request.Email);
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error("Usuario.InvalidEmail", ex.Message));
        }

        usuario.UpdateInformation(request.Nombre, request.Apellido, email);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
