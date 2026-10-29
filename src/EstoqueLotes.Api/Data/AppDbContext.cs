using EstoqueLotes.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EstoqueLotes.Api.Data;

// O DbContext é a "sessão" com o banco: rastreia as entidades carregadas e,
// no SaveChangesAsync, gera os INSERT/UPDATE/DELETE numa única transação.
// O schema foi criado à mão (database/01-schema.sql), então aqui só mapeamos: sem migrations.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Lote> Lotes => Set<Lote>();
    public DbSet<Movimentacao> Movimentacoes => Set<Movimentacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produto>(entity =>
        {
            entity.ToTable("Produtos");
            entity.Property(p => p.Nome).HasMaxLength(100);
            entity.Property(p => p.Categoria).HasMaxLength(50);
            entity.Property(p => p.Marca).HasMaxLength(50);
        });

        modelBuilder.Entity<Lote>(entity =>
        {
            entity.ToTable("Lotes");
            entity.Property(l => l.CodigoLote).HasMaxLength(30);

            // 1:N: um Produto tem vários Lotes (Produto não precisa de lista de lotes)
            entity.HasOne(l => l.Produto)
                  .WithMany()
                  .HasForeignKey(l => l.ProdutoId);
        });

        modelBuilder.Entity<Movimentacao>(entity =>
        {
            entity.ToTable("Movimentacoes");

            // Enum <-> texto do banco: Entrada <-> 'ENTRADA', Saida <-> 'SAIDA'
            entity.Property(m => m.Tipo)
                  .HasMaxLength(10)
                  .HasConversion(
                      tipo => tipo.ToString().ToUpper(),
                      texto => Enum.Parse<TipoMovimentacao>(texto, true));

            // 1:N: um Lote tem várias Movimentações
            entity.HasOne(m => m.Lote)
                  .WithMany(l => l.Movimentacoes)
                  .HasForeignKey(m => m.LoteId);
        });
    }
}
