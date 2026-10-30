using EstoqueLotes.Api.Dtos;

namespace EstoqueLotes.Api.Services;

public interface IRelatorioService
{
    Task<List<LoteVencendoDto>> LotesVencendoAsync(int dias);
    Task<List<EstoquePorCategoriaDto>> EstoquePorCategoriaAsync();
}
