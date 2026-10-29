using System.ComponentModel.DataAnnotations;

namespace EstoqueLotes.Api.Dtos;

// Saída: inclui o nome do produto (vem do JOIN) e se o lote está vencido (calculado).
public record LoteDto(
    int Id,
    int ProdutoId,
    string ProdutoNome,
    string CodigoLote,
    DateOnly DataFabricacao,
    DateOnly DataValidade,
    int Quantidade,
    bool Vencido);

// Entrada do POST /api/lotes. A Quantidade é a produção inicial: vira uma ENTRADA no histórico.
public class CriarLoteDto
{
    [Range(1, int.MaxValue)]
    public int ProdutoId { get; set; }

    [Required, MaxLength(30)]
    public string CodigoLote { get; set; } = string.Empty;

    public DateOnly DataFabricacao { get; set; }
    public DateOnly DataValidade { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantidade { get; set; }
}
