using EstoqueLotes.Api.Models;

namespace EstoqueLotes.Api.Repositories;

// Contrato do acesso a dados de Produto. O Service depende desta interface, não da classe concreta:
// assim dá para trocar a implementação ou usar um mock nos testes.
public interface IProdutoRepository
{
    Task<List<Produto>> ListarAsync();
    Task<Produto?> ObterPorIdAsync(int id);
    void Adicionar(Produto produto);
    void Remover(Produto produto);

    // Adicionar/Remover só marcam a mudança; nada vai para o banco até aqui.
    Task SalvarAlteracoesAsync();
}
