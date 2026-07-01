using Api.Application.Features.Usuarios.CreateUsuario;
using Api.Application.Features.Usuarios.GetUsuarios;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly ISender _sender;

    public UsuariosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUsuarioCommand command, 
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { 
                Error = result.Error.Code, 
                Message = result.Error.Message 
            });
        }

        return CreatedAtAction(nameof(Create), new { id = result.Value }, result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUsuariosQuery(), cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { 
                Error = result.Error.Code, 
                Message = result.Error.Message 
            });
        }

        return Ok(result.Value);
    }
}
