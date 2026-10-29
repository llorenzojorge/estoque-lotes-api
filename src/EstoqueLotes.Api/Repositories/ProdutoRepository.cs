using EstoqueLotes.Api.Data;
using EstoqueLotes.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EstoqueLotes.Api.Repositories;

public class ProdutoRepository : IProdutoRepository
{
    private readonly AppDbContext _context;

    public ProdutoRepository(AppDbContext context) => _context = context;

    // AsNoTracking: só leitura, o EF não precisa vigiar mudanças nesses objetos (mais leve).
    public Task<List<Produto>> ListarAsync() =>
        _context.Produtos.AsNoTracking().OrderBy(p => p.Nome).ToListAsync();

    // Sem AsNoTracking: o objeto volta rastreado, então alterar uma propriedade e
    // chamar SalvarAlteracoesAsync já gera o UPDATE (é o que o PUT usa).
    public Task<Produto?> ObterPorIdAsync(int id) =>
        _context.Produtos.FirstOrDefaultAsync(p => p.Id == id);

    public void Adicionar(Produto produto) => _context.Produtos.Add(produto);

    public void Remover(Produto produto) => _context.Produtos.Remove(produto);

    public Task SalvarAlteracoesAsync() => _context.SaveChangesAsync();
}
