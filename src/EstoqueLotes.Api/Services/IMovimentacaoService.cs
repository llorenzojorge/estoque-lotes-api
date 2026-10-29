using EstoqueLotes.Api.Dtos;

namespace EstoqueLotes.Api.Services;

public interface IMovimentacaoService
{
    Task<MovimentacaoDto> RegistrarAsync(CriarMovimentacaoDto dto);
    Task<List<MovimentacaoDto>> ListarPorLoteAsync(int loteId);
}
