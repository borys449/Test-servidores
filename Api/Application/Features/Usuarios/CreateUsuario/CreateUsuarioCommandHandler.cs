using Api.Application.Common;
using Api.Domain.Entities;
using Api.Domain.ValueObjects;
using Api.Infrastructure.Data;
using MediatR;

namespace Api.Application.Features.Usuarios.CreateUsuario;

public class CreateUsuarioCommandHandler : IRequestHandler<CreateUsuarioCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;

    public CreateUsuarioCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateUsuarioCommand request, CancellationToken cancellationToken)
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

        var usuario = new Usuario(
            Guid.NewGuid(),
            request.Nombre,
            request.Apellido,
            email
        );

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(usuario.Id);
    }
}
