using Api.Application.Common;
using Api.Infrastructure.Data;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Features.Usuarios.GetUsuarios;

[ApiController]
[Route("api/usuarios")]
public class GetUsuariosController : ControllerBase
{
    private readonly ISender _sender;

    public GetUsuariosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default
    )
    {
        var query = new GetUsuariosQuery(pageNumber, pageSize);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { Error = result.Error.Code, Message = result.Error.Message });
        }

        return Ok(result.Value);
    }
}

public record GetUsuariosQuery(int PageNumber, int PageSize)
    : IRequest<Result<PagedResult<UsuarioResponse>>>;

public class GetUsuariosQueryValidator : AbstractValidator<GetUsuariosQuery>
{
    public GetUsuariosQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("El número de página debe ser mayor o igual a 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("El tamaño de página debe ser mayor o igual a 1.")
            .LessThanOrEqualTo(100)
            .WithMessage("El tamaño de página no puede exceder 100 registros.");
    }
}

public record UsuarioResponse(
    Guid Id,
    string Nombre,
    string Apellido,
    string Email,
    DateTime CreatedAt
);

public class GetUsuariosQueryHandler
    : IRequestHandler<GetUsuariosQuery, Result<PagedResult<UsuarioResponse>>>
{
    private readonly ApplicationDbContext _context;

    public GetUsuariosQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<UsuarioResponse>>> Handle(
        GetUsuariosQuery request,
        CancellationToken cancellationToken
    )
    {
        var queryable = _context.Usuarios.AsNoTracking();

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .OrderBy(u => u.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UsuarioResponse(
                u.Id,
                u.Nombre,
                u.Apellido,
                u.Email.Value,
                u.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        var pagedResult = new PagedResult<UsuarioResponse>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize
        );

        return Result.Success(pagedResult);
    }
}
