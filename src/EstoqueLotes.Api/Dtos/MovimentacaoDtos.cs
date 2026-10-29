using System.ComponentModel.DataAnnotations;
using EstoqueLotes.Api.Models;

namespace EstoqueLotes.Api.Dtos;

public record MovimentacaoDto(int Id, int LoteId, TipoMovimentacao Tipo, int Quantidade, DateTime Data);

// Entrada do POST /api/movimentacoes. No JSON o tipo vai como texto: "Entrada" ou "Saida".
public class CriarMovimentacaoDto
{
    [Range(1, int.MaxValue)]
    public int LoteId { get; set; }

    // Nullable + [Required]: se o campo faltar no JSON, dá 400. Sem o "?", faltar o campo
    // viraria o valor padrão do enum (Entrada) sem ninguém perceber.
    [Required]
    public TipoMovimentacao? Tipo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
    public int Quantidade { get; set; }
}
