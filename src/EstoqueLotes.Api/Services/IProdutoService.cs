using EstoqueLotes.Api.Dtos;

namespace EstoqueLotes.Api.Services;

// O Service conversa com o Controller só por DTOs; a entidade não sai daqui para a API.
public interface IProdutoService
{
    Task<List<ProdutoDto>> ListarAsync();
    Task<ProdutoDto?> ObterPorIdAsync(int id);   // null = não encontrado
    Task<ProdutoDto> CriarAsync(CriarProdutoDto dto);
    Task<bool> AtualizarAsync(int id, CriarProdutoDto dto);   // false = não encontrado
    Task<bool> RemoverAsync(int id);                          // false = não encontrado
}
