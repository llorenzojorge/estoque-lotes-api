using EstoqueLotes.Api.Dtos;
using EstoqueLotes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EstoqueLotes.Api.Controllers;

[ApiController]
[Route("api/movimentacoes")]
public class MovimentacoesController : ControllerBase
{
    private readonly IMovimentacaoService _service;

    public MovimentacoesController(IMovimentacaoService service) => _service = service;

    // POST /api/movimentacoes
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovimentacaoDto>> Registrar(CriarMovimentacaoDto dto)
    {
        var movimentacao = await _service.RegistrarAsync(dto);
        // 201 com Location apontando para o histórico do lote
        return Created($"/api/lotes/{movimentacao.LoteId}/movimentacoes", movimentacao);
    }
}
