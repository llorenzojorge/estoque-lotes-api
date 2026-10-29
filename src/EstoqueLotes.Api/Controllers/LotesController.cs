using EstoqueLotes.Api.Dtos;
using EstoqueLotes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EstoqueLotes.Api.Controllers;

// Sem try/catch: se o Service lançar NaoEncontradoException ou RegraDeNegocioException,
// o GlobalExceptionHandler devolve 404 ou 400.
[ApiController]
[Route("api/lotes")]
public class LotesController : ControllerBase
{
    private readonly ILoteService _loteService;
    private readonly IMovimentacaoService _movimentacaoService;

    public LotesController(ILoteService loteService, IMovimentacaoService movimentacaoService)
    {
        _loteService = loteService;
        _movimentacaoService = movimentacaoService;
    }

    // GET /api/lotes/5
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoteDto>> ObterPorId(int id)
    {
        return Ok(await _loteService.ObterPorIdAsync(id));
    }

    // POST /api/lotes
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoteDto>> Criar(CriarLoteDto dto)
    {
        var criado = await _loteService.CriarAsync(dto);
        return CreatedAtAction(nameof(ObterPorId), new { id = criado.Id }, criado);
    }

    // GET /api/lotes/5/movimentacoes  (histórico do lote: rastreabilidade)
    [HttpGet("{id:int}/movimentacoes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<MovimentacaoDto>>> ListarMovimentacoes(int id)
    {
        return Ok(await _movimentacaoService.ListarPorLoteAsync(id));
    }
}
