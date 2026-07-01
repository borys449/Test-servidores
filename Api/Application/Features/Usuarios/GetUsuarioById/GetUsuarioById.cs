using Api.Application.Common;
using Api.Infrastructure.Data;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Features.Usuarios.GetUsuarioById;

[ApiController]
[Route("api/usuarios")]
public class GetUsuarioByIdController : ControllerBase
{
    private readonly ISender _sender;

    public GetUsuarioByIdController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken
    )
    {
        var query = new GetUsuarioByIdQuery(id);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Usuario.NotFound")
            {
                return NotFound(new { Error = result.Error.Code, Message = result.Error.Message });
            }
            return BadRequest(new { Error = result.Error.Code, Message = result.Error.Message });
        }

        return Ok(result.Value);
    }
}

public record GetUsuarioByIdQuery(Guid Id) : IRequest<Result<UsuarioDetailResponse>>;

public class GetUsuarioByIdQueryValidator : AbstractValidator<GetUsuarioByIdQuery>
{
    public GetUsuarioByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El identificador del usuario es requerido.");
    }
}

public record UsuarioDetailResponse(
    Guid Id,
    string Nombre,
    string Apellido,
    string Email,
    DateTime CreatedAt
);

public class GetUsuarioByIdQueryHandler : IRequestHandler<GetUsuarioByIdQuery, Result<UsuarioDetailResponse>>
{
    private readonly ApplicationDbContext _context;

    public GetUsuarioByIdQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UsuarioDetailResponse>> Handle(
        GetUsuarioByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (usuario is null)
        {
            return Result.Failure<UsuarioDetailResponse>(
                new Error("Usuario.NotFound", $"El usuario con Id '{request.Id}' no fue encontrado.")
            );
        }

        var response = new UsuarioDetailResponse(
            usuario.Id,
            usuario.Nombre,
            usuario.Apellido,
            usuario.Email.Value,
            usuario.CreatedAt
        );

        return Result.Success(response);
    }
}
