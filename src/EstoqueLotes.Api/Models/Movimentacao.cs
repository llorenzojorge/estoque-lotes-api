namespace EstoqueLotes.Api.Models;

public class Movimentacao
{
    public int Id { get; set; }
    public int LoteId { get; set; }
    public Lote Lote { get; set; } = null!;
    public TipoMovimentacao Tipo { get; set; }
    public int Quantidade { get; set; }
    public DateTime Data { get; set; }   // aqui sim é data + hora (coluna DATETIME2): quando aconteceu
}
