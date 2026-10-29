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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produto>(entity =>
        {
            entity.ToTable("Produtos");
            entity.Property(p => p.Nome).HasMaxLength(100);
            entity.Property(p => p.Categoria).HasMaxLength(50);
            entity.Property(p => p.Marca).HasMaxLength(50);
        });
    }
}
