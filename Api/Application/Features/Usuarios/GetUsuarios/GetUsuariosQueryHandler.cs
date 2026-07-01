using Api.Application.Common;
using Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Features.Usuarios.GetUsuarios;

public class GetUsuariosQueryHandler : IRequestHandler<GetUsuariosQuery, Result<List<UsuarioResponse>>>
{
    private readonly ApplicationDbContext _context;

    public GetUsuariosQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<UsuarioResponse>>> Handle(GetUsuariosQuery request, CancellationToken cancellationToken)
    {
        var usuarios = await _context.Usuarios
            .AsNoTracking()
            .Select(u => new UsuarioResponse(
                u.Id,
                u.Nombre,
                u.Apellido,
                u.Email.Value,
                u.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(usuarios);
    }
}
