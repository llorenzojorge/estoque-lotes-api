namespace EstoqueLotes.Api.Dtos;

// O Dapper preenche estas classes casando o NOME de cada coluna do SELECT (ou do alias "AS ...")
// com o nome da propriedade. Por isso os nomes daqui são iguais aos aliases do SQL.

public class LoteVencendoDto
{
    public string Produto { get; init; } = string.Empty;
    public string CodigoLote { get; init; } = string.Empty;
    public DateOnly DataValidade { get; init; }
    public int DiasParaVencer { get; init; }
    public int Quantidade { get; init; }
}

public class EstoquePorCategoriaDto
{
    public string Categoria { get; init; } = string.Empty;
    public int Produtos { get; init; }
    public int Lotes { get; init; }
    public int EstoqueTotal { get; init; }
    public int EstoqueValido { get; init; }
}
