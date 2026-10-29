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
        // 1. Buscar o lote: await _loteRepository.ObterPorIdAsync(dto.LoteId)
        //    Se vier null -> throw new NaoEncontradoException("...")              (vira 404)
        //    Dica: dá para usar "?? throw", como no LoteService.ObterPorIdAsync.

        var lote = await _loteRepository.ObterPorIdAsync(dto.LoteId)
            ?? throw new NaoEncontradoException($"Lote {dto.LoteId} não encontrado.");

        // 2. Se dto.Tipo for TipoMovimentacao.Saida:
        //    a) lote vencido -> throw new RegraDeNegocioException("...")          (vira 400)
        //       "Hoje" só com a data: DateOnly.FromDateTime(DateTime.Today)
        //       O Lote já tem um método pronto para isso. Qual?
        //    b) dto.Quantidade maior que lote.Quantidade -> RegraDeNegocioException (vira 400)

        if (dto.Tipo == TipoMovimentacao.Saida)
        {
            if (lote.EstaVencido(DateOnly.FromDateTime(DateTime.Today)))
                throw new RegraDeNegocioException($"O Lote {dto.LoteId} está vencido e não pode ser movimentado.");
            if (dto.Quantidade > lote.Quantidade)
                throw new RegraDeNegocioException($"Estoque insuficiente no lote {dto.LoteId}: disponível {lote.Quantidade}, solicitado {dto.Quantidade}.");

        // 3. Atualizar o saldo: Entrada soma, Saida subtrai (lote.Quantidade).
            lote.Quantidade -= dto.Quantidade;
        }
        if (dto.Tipo == TipoMovimentacao.Entrada)
            lote.Quantidade += dto.Quantidade;

        // 4. Criar a Movimentacao (LoteId, Tipo, Quantidade, Data = DateTime.Now)
        //    e chamar _movimentacaoRepository.Adicionar(...).
        //    Atenção: dto.Tipo é "TipoMovimentacao?" (nullable). Use dto.Tipo!.Value ou dto.Tipo.Value.

        var movimentacao = new Movimentacao
        {
            LoteId = dto.LoteId,
            Tipo = dto.Tipo!.Value,
            Quantidade = dto.Quantidade,
            Data = DateTime.Now
        };

        _movimentacaoRepository.Adicionar(movimentacao);

        // 5. UM único await _movimentacaoRepository.SalvarAlteracoesAsync();
        //    Ele grava o INSERT da movimentação e o UPDATE do lote na mesma transação.
        await _movimentacaoRepository.SalvarAlteracoesAsync();

        // 6. return ParaDto(movimentacao);
        return ParaDto(movimentacao);
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
