using EstoqueLotes.Api.Dtos;
using EstoqueLotes.Api.Errors;
using EstoqueLotes.Api.Models;
using EstoqueLotes.Api.Repositories;

namespace EstoqueLotes.Api.Services;

public class MovimentacaoService : IMovimentacaoService
{
    private readonly ILoteRepository _loteRepository;
    private readonly IMovimentacaoRepository _movimentacaoRepository;

    public MovimentacaoService(ILoteRepository loteRepository, IMovimentacaoRepository movimentacaoRepository)
    {
        _loteRepository = loteRepository;
        _movimentacaoRepository = movimentacaoRepository;
    }

    public async Task<MovimentacaoDto> RegistrarAsync(CriarMovimentacaoDto dto)
    {
        // TODO (Lorenzo): escrever a regra de movimentação. Passos:
        //
        // 1. Buscar o lote: await _loteRepository.ObterPorIdAsync(dto.LoteId)
        //    Se vier null -> throw new NaoEncontradoException("...")              (vira 404)
        //    Dica: dá para usar "?? throw", como no LoteService.ObterPorIdAsync.
        //
        // 2. Se dto.Tipo for TipoMovimentacao.Saida:
        //    a) lote vencido -> throw new RegraDeNegocioException("...")          (vira 400)
        //       "Hoje" só com a data: DateOnly.FromDateTime(DateTime.Today)
        //       O Lote já tem um método pronto para isso. Qual?
        //    b) dto.Quantidade maior que lote.Quantidade -> RegraDeNegocioException (vira 400)
        //
        // 3. Atualizar o saldo: Entrada soma, Saida subtrai (lote.Quantidade).
        //
        // 4. Criar a Movimentacao (LoteId, Tipo, Quantidade, Data = DateTime.Now)
        //    e chamar _movimentacaoRepository.Adicionar(...).
        //    Atenção: dto.Tipo é "TipoMovimentacao?" (nullable). Use dto.Tipo!.Value ou dto.Tipo.Value.
        //
        // 5. UM único await _movimentacaoRepository.SalvarAlteracoesAsync();
        //    Ele grava o INSERT da movimentação e o UPDATE do lote na mesma transação.
        //
        // 6. return ParaDto(movimentacao);
        throw new NotImplementedException();
    }

    public async Task<List<MovimentacaoDto>> ListarPorLoteAsync(int loteId)
    {
        _ = await _loteRepository.ObterPorIdAsync(loteId)
            ?? throw new NaoEncontradoException($"Lote {loteId} não encontrado.");

        var movimentacoes = await _movimentacaoRepository.ListarPorLoteAsync(loteId);
        return movimentacoes.Select(ParaDto).ToList();
    }

    private static MovimentacaoDto ParaDto(Movimentacao m) => new(m.Id, m.LoteId, m.Tipo, m.Quantidade, m.Data);
}
