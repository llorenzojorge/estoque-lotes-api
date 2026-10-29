namespace EstoqueLotes.Api.Models;

public class Lote
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }                 // chave estrangeira (coluna)
    public Produto Produto { get; set; } = null!;      // navegação: o EF preenche com Include
    public string CodigoLote { get; set; } = string.Empty;

    // DateOnly = só a data, sem hora (coluna DATE). Evita o erro de comparar data com data+hora.
    public DateOnly DataFabricacao { get; set; }
    public DateOnly DataValidade { get; set; }
    public int Quantidade { get; set; }

    public List<Movimentacao> Movimentacoes { get; set; } = new();   // 1:N

    // Regra 4: vencido = validade ANTES de hoje. No próprio dia da validade ainda é válido.
    public bool EstaVencido(DateOnly hoje) => DataValidade < hoje;
}
