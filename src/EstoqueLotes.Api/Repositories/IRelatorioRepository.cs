using EstoqueLotes.Api.Dtos;

namespace EstoqueLotes.Api.Repositories;

// Relatórios são só leitura e devolvem formatos que não são entidades,
// por isso o repository já devolve os DTOs prontos.
public interface IRelatorioRepository
{
    Task<List<LoteVencendoDto>> LotesVencendoAsync(DateOnly hoje, int dias);
    Task<List<EstoquePorCategoriaDto>> EstoquePorCategoriaAsync(DateOnly hoje);
}
