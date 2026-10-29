using EstoqueLotes.Api.Data;
using EstoqueLotes.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EstoqueLotes.Api.Repositories;

public class LoteRepository : ILoteRepository
{
    private readonly AppDbContext _context;

    public LoteRepository(AppDbContext context) => _context = context;

    public Task<Lote?> ObterPorIdAsync(int id) =>
        _context.Lotes.FirstOrDefaultAsync(l => l.Id == id);

    // Include = JOIN com Produtos
    public Task<Lote?> ObterComProdutoAsync(int id) =>
        _context.Lotes.AsNoTracking().Include(l => l.Produto).FirstOrDefaultAsync(l => l.Id == id);

    public Task<bool> CodigoExisteAsync(string codigoLote) =>
        _context.Lotes.AnyAsync(l => l.CodigoLote == codigoLote);

    public void Adicionar(Lote lote) => _context.Lotes.Add(lote);

    public Task SalvarAlteracoesAsync() => _context.SaveChangesAsync();
}
