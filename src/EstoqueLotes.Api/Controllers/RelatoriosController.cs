using System.ComponentModel.DataAnnotations;
using EstoqueLotes.Api.Dtos;
using EstoqueLotes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EstoqueLotes.Api.Controllers;

[ApiController]
[Route("api/relatorios")]
public class RelatoriosController : ControllerBase
{
    private readonly IRelatorioService _service;

    public RelatoriosController(IRelatorioService service) => _service = service;

    // GET /api/relatorios/vencendo?dias=30
    // [FromQuery] = o valor vem da query string. Se não vier, usa 30. Fora de 0..365 -> 400 automático.
    [HttpGet("vencendo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<LoteVencendoDto>>> LotesVencendo(
        [FromQuery, Range(0, 365)] int dias = 30)
    {
        return Ok(await _service.LotesVencendoAsync(dias));
    }

    // GET /api/relatorios/estoque-por-categoria
    [HttpGet("estoque-por-categoria")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EstoquePorCategoriaDto>>> EstoquePorCategoria()
    {
        return Ok(await _service.EstoquePorCategoriaAsync());
    }
}
