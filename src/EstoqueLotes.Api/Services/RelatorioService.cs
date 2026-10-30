using EstoqueLotes.Api.Dtos;
using EstoqueLotes.Api.Repositories;

namespace EstoqueLotes.Api.Services;

public class RelatorioService : IRelatorioService
{
    private readonly IRelatorioRepository _repository;

    public RelatorioService(IRelatorioRepository repository) => _repository = repository;

    // "Hoje" é decidido aqui, do mesmo jeito que na regra de movimentação (DateTime.Today da API),
    // e não com GETDATE() no banco. Assim API e relatório sempre concordam sobre que dia é hoje.
    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Today);

    public Task<List<LoteVencendoDto>> LotesVencendoAsync(int dias) =>
        _repository.LotesVencendoAsync(Hoje, dias);

    public Task<List<EstoquePorCategoriaDto>> EstoquePorCategoriaAsync() =>
        _repository.EstoquePorCategoriaAsync(Hoje);
}
