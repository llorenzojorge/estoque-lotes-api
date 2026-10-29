namespace EstoqueLotes.Api.Errors;

// Erro esperado: uma regra de negócio foi violada (ex.: saída maior que o estoque).
// O GlobalExceptionHandler transforma esta exceção em 400 Bad Request.
public class RegraDeNegocioException : Exception
{
    public RegraDeNegocioException(string mensagem) : base(mensagem) { }
}
