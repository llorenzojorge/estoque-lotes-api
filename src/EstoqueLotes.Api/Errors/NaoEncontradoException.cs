namespace EstoqueLotes.Api.Errors;

// O recurso pedido não existe (ex.: lote com Id inexistente).
// O GlobalExceptionHandler transforma esta exceção em 404 Not Found.
public class NaoEncontradoException : Exception
{
    public NaoEncontradoException(string mensagem) : base(mensagem) { }
}
