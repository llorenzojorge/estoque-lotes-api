using EstoqueLotes.Api.Dtos;
using EstoqueLotes.Api.Errors;
using EstoqueLotes.Api.Models;
using EstoqueLotes.Api.Repositories;

namespace EstoqueLotes.Api.Services;

public class ProdutoService : IProdutoService
{
    private readonly IProdutoRepository _repository;

    // Injeção de dependência: o container entrega um IProdutoRepository pronto.
    public ProdutoService(IProdutoRepository repository) => _repository = repository;

    public async Task<List<ProdutoDto>> ListarAsync()
    {
        var produtos = await _repository.ListarAsync();
        return produtos.Select(ParaDto).ToList();
    }

    public async Task<ProdutoDto?> ObterPorIdAsync(int id)
    {
        var produto = await _repository.ObterPorIdAsync(id);
        return produto is null ? null : ParaDto(produto);
    }

    public async Task<ProdutoDto> CriarAsync(CriarProdutoDto dto)
    {
        var produto = new Produto
        {
            Nome = dto.Nome.Trim(),
            Categoria = dto.Categoria.Trim(),
            Marca = dto.Marca.Trim()
        };

        _repository.Adicionar(produto);
        await _repository.SalvarAlteracoesAsync();   // depois do INSERT, o EF preenche produto.Id

        return ParaDto(produto);
    }

    public async Task<bool> AtualizarAsync(int id, CriarProdutoDto dto)
    {
        var produto = await _repository.ObterPorIdAsync(id);
        if (produto is null) return false;

        produto.Nome = dto.Nome.Trim();
        produto.Categoria = dto.Categoria.Trim();
        produto.Marca = dto.Marca.Trim();

        await _repository.SalvarAlteracoesAsync();   // o EF detecta o que mudou e gera o UPDATE
        return true;
    }

    public async Task<bool> RemoverAsync(int id)
    {
        var produto = await _repository.ObterPorIdAsync(id);
        if (produto is null) return false;

        // Sem esta checagem, a FK de Lotes barrava o DELETE no banco e a API devolvia 500.
        if (await _repository.PossuiLotesAsync(id))
            throw new RegraDeNegocioException("Não é possível excluir um produto que possui lotes.");

        _repository.Remover(produto);
        await _repository.SalvarAlteracoesAsync();
        return true;
    }

    // Converte entidade -> DTO. Fica num lugar só para não repetir em cada método.
    private static ProdutoDto ParaDto(Produto p) => new(p.Id, p.Nome, p.Categoria, p.Marca);
}
