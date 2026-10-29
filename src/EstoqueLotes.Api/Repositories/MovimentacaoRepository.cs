using EstoqueLotes.Api.Data;
using EstoqueLotes.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EstoqueLotes.Api.Repositories;

public class MovimentacaoRepository : IMovimentacaoRepository
{
    private readonly AppDbContext _context;

    public MovimentacaoRepository(AppDbContext context) => _context = context;

    public Task<List<Movimentacao>> ListarPorLoteAsync(int loteId) =>
        _context.Movimentacoes
            .AsNoTracking()
            .Where(m => m.LoteId == loteId)
            .OrderBy(m => m.Data).ThenBy(m => m.Id)
            .ToListAsync();

    public void Adicionar(Movimentacao movimentacao) => _context.Movimentacoes.Add(movimentacao);

    public Task SalvarAlteracoesAsync() => _context.SaveChangesAsync();
}
