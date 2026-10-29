namespace EstoqueLotes.Api.Models;

// Entidade: representa uma linha da tabela Produtos.
// O EF Core liga cada propriedade à coluna de mesmo nome.
public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
}
