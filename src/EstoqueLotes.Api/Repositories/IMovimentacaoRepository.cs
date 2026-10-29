using EstoqueLotes.Api.Models;

namespace EstoqueLotes.Api.Repositories;

public interface IMovimentacaoRepository
{
    Task<List<Movimentacao>> ListarPorLoteAsync(int loteId);
    void Adicionar(Movimentacao movimentacao);

    // Os repositories recebem o MESMO AppDbContext na requisição (Scoped). Por isso um único
    // SalvarAlteracoesAsync grava a movimentação nova E a quantidade alterada do lote,
    // numa só transação.
    Task SalvarAlteracoesAsync();
}
