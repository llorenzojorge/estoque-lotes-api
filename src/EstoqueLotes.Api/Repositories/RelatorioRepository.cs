using Dapper;
using EstoqueLotes.Api.Dtos;
using Microsoft.Data.SqlClient;

namespace EstoqueLotes.Api.Repositories;

// Relatórios com Dapper: o SQL é escrito à mão (é o mesmo de database/03-consultas.sql, consultas 1 e 3)
// e o Dapper só executa a consulta e transforma cada linha do resultado num objeto C#.
public class RelatorioRepository : IRelatorioRepository
{
    private readonly string _connectionString;

    public RelatorioRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não configurada.");
    }

    // Consulta 1: lotes com estoque que vencem entre hoje e hoje + @dias.
    // @hoje e @dias são PARÂMETROS: o valor vai separado do texto do SQL, então não há como
    // injetar SQL por eles (SQL Injection). Nunca montar a query concatenando strings.
    private const string SqlLotesVencendo = """
        SELECT
            p.Nome                                  AS Produto,
            l.CodigoLote,
            l.DataValidade,
            DATEDIFF(DAY, @hoje, l.DataValidade)    AS DiasParaVencer,
            l.Quantidade
        FROM Lotes l
        INNER JOIN Produtos p ON p.Id = l.ProdutoId
        WHERE l.DataValidade BETWEEN @hoje AND DATEADD(DAY, @dias, @hoje)
          AND l.Quantidade > 0
        ORDER BY l.DataValidade;
        """;

    // Consulta 3: estoque por categoria. LEFT JOIN para incluir categorias sem lote.
    private const string SqlEstoquePorCategoria = """
        SELECT
            p.Categoria,
            COUNT(DISTINCT p.Id)                                                        AS Produtos,
            COUNT(l.Id)                                                                 AS Lotes,
            COALESCE(SUM(l.Quantidade), 0)                                              AS EstoqueTotal,
            COALESCE(SUM(CASE WHEN l.DataValidade >= @hoje THEN l.Quantidade END), 0)   AS EstoqueValido
        FROM Produtos p
        LEFT JOIN Lotes l ON l.ProdutoId = p.Id
        GROUP BY p.Categoria
        ORDER BY EstoqueTotal DESC;
        """;

    public async Task<List<LoteVencendoDto>> LotesVencendoAsync(DateOnly hoje, int dias)
    {
        // "using": a conexão é fechada (devolvida ao pool) no fim do método, mesmo se der erro.
        await using var conexao = new SqlConnection(_connectionString);

        // O objeto anônimo vira os parâmetros: a propriedade "hoje" preenche @hoje e "dias" preenche @dias.
        // O valor viaja separado do texto da consulta.
        var linhas = await conexao.QueryAsync<LoteVencendoDto>(SqlLotesVencendo, new { hoje, dias });
        return linhas.ToList();
    }

    public async Task<List<EstoquePorCategoriaDto>> EstoquePorCategoriaAsync(DateOnly hoje)
    {
        await using var conexao = new SqlConnection(_connectionString);
        var linhas = await conexao.QueryAsync<EstoquePorCategoriaDto>(SqlEstoquePorCategoria, new { hoje });
        return linhas.ToList();
    }
}
