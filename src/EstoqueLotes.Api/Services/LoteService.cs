using EstoqueLotes.Api.Dtos;
using EstoqueLotes.Api.Errors;
using EstoqueLotes.Api.Models;
using EstoqueLotes.Api.Repositories;

namespace EstoqueLotes.Api.Services;

public class LoteService : ILoteService
{
    private readonly ILoteRepository _loteRepository;
    private readonly IProdutoRepository _produtoRepository;

    public LoteService(ILoteRepository loteRepository, IProdutoRepository produtoRepository)
    {
        _loteRepository = loteRepository;
        _produtoRepository = produtoRepository;
    }

    public async Task<LoteDto> ObterPorIdAsync(int id)
    {
        var lote = await _loteRepository.ObterComProdutoAsync(id)
            ?? throw new NaoEncontradoException($"Lote {id} não encontrado.");

        return ParaDto(lote, lote.Produto.Nome);
    }

    public async Task<LoteDto> CriarAsync(CriarLoteDto dto)
    {
        // Regra 1: validade depois da fabricação (o banco também tem um CHECK, mas aqui a mensagem é clara)
        if (dto.DataValidade <= dto.DataFabricacao)
            throw new RegraDeNegocioException("A data de validade deve ser posterior à data de fabricação.");

        var produto = await _produtoRepository.ObterPorIdAsync(dto.ProdutoId)
            ?? throw new RegraDeNegocioException($"Produto {dto.ProdutoId} não existe.");

        var codigo = dto.CodigoLote.Trim();
        if (await _loteRepository.CodigoExisteAsync(codigo))
            throw new RegraDeNegocioException($"Já existe um lote com o código {codigo}.");

        var lote = new Lote
        {
            ProdutoId = produto.Id,
            CodigoLote = codigo,
            DataFabricacao = dto.DataFabricacao,
            DataValidade = dto.DataValidade,
            Quantidade = dto.Quantidade
        };

        // A quantidade inicial entra no histórico como ENTRADA, para o saldo do lote
        // sempre bater com a soma das movimentações (é o que a consulta 8 audita).
        if (dto.Quantidade > 0)
        {
            lote.Movimentacoes.Add(new Movimentacao
            {
                Tipo = TipoMovimentacao.Entrada,
                Quantidade = dto.Quantidade,
                Data = DateTime.Now
            });
        }

        _loteRepository.Adicionar(lote);
        await _loteRepository.SalvarAlteracoesAsync();   // INSERT do lote + INSERT da entrada, numa transação

        return ParaDto(lote, produto.Nome);
    }

    private static LoteDto ParaDto(Lote l, string produtoNome) => new(
        l.Id, l.ProdutoId, produtoNome, l.CodigoLote, l.DataFabricacao, l.DataValidade, l.Quantidade,
        l.EstaVencido(DateOnly.FromDateTime(DateTime.Today)));
}
