using EstoqueLotes.Api.Dtos;

namespace EstoqueLotes.Api.Services;

public interface ILoteService
{
    Task<LoteDto> ObterPorIdAsync(int id);       // lança NaoEncontradoException se não existir
    Task<LoteDto> CriarAsync(CriarLoteDto dto);  // lança RegraDeNegocioException se os dados violarem uma regra
}
