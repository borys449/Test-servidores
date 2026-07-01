using Api.Application.Common;
using MediatR;

namespace Api.Application.Features.Usuarios.GetUsuarios;

public record GetUsuariosQuery() : IRequest<Result<List<UsuarioResponse>>>;
