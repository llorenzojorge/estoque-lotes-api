using EstoqueLotes.Api.Models;

namespace EstoqueLotes.Api.Repositories;

public interface ILoteRepository
{
    Task<Lote?> ObterPorIdAsync(int id);              // rastreado: alterar Quantidade + salvar gera o UPDATE
    Task<Lote?> ObterComProdutoAsync(int id);         // só leitura, com o Produto carregado (para o nome)
    Task<bool> CodigoExisteAsync(string codigoLote);
    void Adicionar(Lote lote);
    Task SalvarAlteracoesAsync();
}
