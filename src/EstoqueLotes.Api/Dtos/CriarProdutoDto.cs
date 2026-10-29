using System.ComponentModel.DataAnnotations;

namespace EstoqueLotes.Api.Dtos;

// DTO de entrada, usado no POST e no PUT. Não tem Id: quem gera o Id é o banco (IDENTITY).
// Com [ApiController], se alguma validação falhar a API devolve 400 sozinha, antes de chegar ao Controller.
// Os tamanhos máximos são os mesmos das colunas do banco.
public class CriarProdutoDto
{
    [Required, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Categoria { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Marca { get; set; } = string.Empty;
}
