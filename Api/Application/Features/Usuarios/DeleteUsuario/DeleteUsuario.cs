using Api.Application.Common;
using Api.Infrastructure.Data;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Features.Usuarios.DeleteUsuario;

[ApiController]
[Route("api/usuarios")]
public class DeleteUsuarioController : ControllerBase
{
    private readonly ISender _sender;

    public DeleteUsuarioController(ISender sender)
    {
        _sender = sender;
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteUsuarioCommand(id);
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

public record DeleteUsuarioCommand(Guid Id) : IRequest<Result>;

public class DeleteUsuarioCommandValidator : AbstractValidator<DeleteUsuarioCommand>
{
    public DeleteUsuarioCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El identificador del usuario es requerido.");
    }
}

public class DeleteUsuarioCommandHandler : IRequestHandler<DeleteUsuarioCommand, Result>
{
    private readonly ApplicationDbContext _context;

    public DeleteUsuarioCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteUsuarioCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _context.Usuarios
            .SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (usuario is null)
        {
            return Result.Failure(
                new Error("Usuario.NotFound", $"El usuario con Id '{request.Id}' no fue encontrado.")
            );
        }

        usuario.Delete();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
