namespace EstoqueLotes.Api.Dtos;

// DTO de saída: o formato que a API devolve.
// "record" é uma classe feita para carregar dados: imutável e com igualdade por valor.
public record ProdutoDto(int Id, string Nome, string Categoria, string Marca);
