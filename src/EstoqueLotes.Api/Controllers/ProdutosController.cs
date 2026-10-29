using EstoqueLotes.Api.Dtos;
using EstoqueLotes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EstoqueLotes.Api.Controllers;

// O Controller só cuida de HTTP: recebe a requisição, chama o Service e escolhe o status code.
// [ApiController] liga a validação automática dos DTOs (400) e lê o corpo JSON sozinho.
// [ProducesResponseType] não muda o comportamento; só documenta os status no Swagger.
[ApiController]
[Route("api/produtos")]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService _service;

    public ProdutosController(IProdutoService service) => _service = service;

    // GET /api/produtos
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProdutoDto>>> Listar()
    {
        return Ok(await _service.ListarAsync());
    }

    // GET /api/produtos/5
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProdutoDto>> ObterPorId(int id)
    {
        var produto = await _service.ObterPorIdAsync(id);
        return produto is null ? NotFound() : Ok(produto);
    }

    // POST /api/produtos
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProdutoDto>> Criar(CriarProdutoDto dto)
    {
        var criado = await _service.CriarAsync(dto);
        // 201 + cabeçalho Location apontando para GET /api/produtos/{id}
        return CreatedAtAction(nameof(ObterPorId), new { id = criado.Id }, criado);
    }

    // PUT /api/produtos/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(int id, CriarProdutoDto dto)
    {
        return await _service.AtualizarAsync(id, dto) ? NoContent() : NotFound();
    }

    // DELETE /api/produtos/5
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(int id)
    {
        return await _service.RemoverAsync(id) ? NoContent() : NotFound();
    }
}
