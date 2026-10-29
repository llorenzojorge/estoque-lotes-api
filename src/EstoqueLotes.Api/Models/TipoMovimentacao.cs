namespace EstoqueLotes.Api.Models;

// Enum em vez de string solta: o compilador impede um tipo inválido como "SAIDAA".
// No banco a coluna continua NVARCHAR com 'ENTRADA'/'SAIDA' (a conversão fica no AppDbContext).
public enum TipoMovimentacao
{
    Entrada,
    Saida
}
