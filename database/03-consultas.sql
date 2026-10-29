-- =============================================================
-- 03-consultas.sql
-- Consultas de relatório e conferência sobre o banco EstoqueLotes.
-- Rodar no SSMS depois do 02-seed.sql. Dá para rodar tudo (F5) ou
-- selecionar o bloco de DECLAREs + uma consulta e rodar só ela.
--
-- As consultas 1 e 3 são a base dos endpoints de relatório da API.
-- =============================================================

USE EstoqueLotes;
GO

SET NOCOUNT ON;

-- Parâmetros. Na API eles viram parâmetros da requisição
-- (ex.: GET /api/relatorios/vencendo?dias=30).
DECLARE @hoje          DATE        = CAST(GETDATE() AS DATE);
DECLARE @dias          INT         = 30;
DECLARE @estoqueMinimo INT         = 100;
DECLARE @codigoLote    NVARCHAR(30) = N'L2026-0001';

-- -------------------------------------------------------------
-- 1. Lotes que vencem nos próximos @dias dias (e ainda têm estoque)
--    Uso: alerta para priorizar a venda antes do vencimento.
--    Esperado com o seed: L2026-0004 (hoje), L2026-0001 (7), L2026-0008 (8).
--
--    O filtro compara a coluna DataValidade "pura" com um intervalo.
--    Se a coluna fosse envolvida numa função, ex.:
--    WHERE DATEDIFF(DAY, @hoje, l.DataValidade) <= @dias
--    o SQL Server não conseguiria usar o índice IX_Lotes_DataValidade.
--    Obs.: com poucas linhas o otimizador pode preferir ler a tabela
--    inteira (é mais barato); o índice faz diferença com volume real.
-- -------------------------------------------------------------
SELECT
    p.Nome                                  AS Produto,
    l.CodigoLote,
    l.DataValidade,
    DATEDIFF(DAY, @hoje, l.DataValidade)    AS DiasParaVencer,
    l.Quantidade
FROM Lotes l
INNER JOIN Produtos p ON p.Id = l.ProdutoId
WHERE l.DataValidade BETWEEN @hoje AND DATEADD(DAY, @dias, @hoje)  -- BETWEEN inclui as duas pontas
  AND l.Quantidade > 0
ORDER BY l.DataValidade;

-- -------------------------------------------------------------
-- 2. Lotes vencidos que ainda têm estoque (perda)
--    Vencido = DataValidade < hoje. Esperado: L2026-0003.
-- -------------------------------------------------------------
SELECT
    p.Nome                                  AS Produto,
    l.CodigoLote,
    l.DataValidade,
    DATEDIFF(DAY, l.DataValidade, @hoje)    AS DiasVencido,
    l.Quantidade                            AS QuantidadePerdida
FROM Lotes l
INNER JOIN Produtos p ON p.Id = l.ProdutoId
WHERE l.DataValidade < @hoje
  AND l.Quantidade > 0
ORDER BY l.DataValidade;

-- -------------------------------------------------------------
-- 3. Estoque por categoria
--    LEFT JOIN para incluir categorias cujos produtos não têm lote
--    (Laticínios aparece com 0). COALESCE troca o NULL da soma por 0.
--    EstoqueValido desconta os lotes vencidos.
-- -------------------------------------------------------------
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

-- -------------------------------------------------------------
-- 4. Produtos com estoque válido abaixo do mínimo
--    WHERE filtra linhas ANTES de agrupar; HAVING filtra grupos DEPOIS.
--    Aqui o filtro é sobre a soma, então só pode ser no HAVING.
--    Esperado: Requeijão (0), Mortadela (40), Peito de Frango (90), Linguiça (95).
-- -------------------------------------------------------------
SELECT
    p.Nome                                                                      AS Produto,
    COALESCE(SUM(CASE WHEN l.DataValidade >= @hoje THEN l.Quantidade END), 0)   AS EstoqueValido
FROM Produtos p
LEFT JOIN Lotes l ON l.ProdutoId = p.Id
GROUP BY p.Id, p.Nome
HAVING COALESCE(SUM(CASE WHEN l.DataValidade >= @hoje THEN l.Quantidade END), 0) < @estoqueMinimo
ORDER BY EstoqueValido;

-- -------------------------------------------------------------
-- 5. Produtos sem nenhum lote cadastrado
--    LEFT JOIN traz todos os produtos; onde não há lote, as colunas
--    de Lotes vêm NULL. Esperado: Requeijão Cremoso 200g.
-- -------------------------------------------------------------
SELECT p.Id, p.Nome, p.Categoria
FROM Produtos p
LEFT JOIN Lotes l ON l.ProdutoId = p.Id
WHERE l.Id IS NULL;

-- -------------------------------------------------------------
-- 6. Top 5 lotes com mais saídas
--    TOP é o equivalente do LIMIT no SQL Server.
-- -------------------------------------------------------------
SELECT TOP 5
    l.CodigoLote,
    p.Nome              AS Produto,
    COUNT(*)            AS QtdSaidas,
    SUM(m.Quantidade)   AS TotalSaido
FROM Movimentacoes m
INNER JOIN Lotes l    ON l.Id = m.LoteId
INNER JOIN Produtos p ON p.Id = l.ProdutoId
WHERE m.Tipo = 'SAIDA'
GROUP BY l.CodigoLote, p.Nome
ORDER BY TotalSaido DESC;

-- -------------------------------------------------------------
-- 7. Rastreabilidade: histórico completo de um lote
--    SUM(...) OVER (ORDER BY ...) é uma "window function": soma
--    acumulada linha a linha, mostrando o saldo depois de cada movimento.
-- -------------------------------------------------------------
SELECT
    m.Data,
    m.Tipo,
    m.Quantidade,
    SUM(CASE WHEN m.Tipo = 'ENTRADA' THEN m.Quantidade ELSE -m.Quantidade END)
        OVER (ORDER BY m.Data, m.Id)    AS SaldoApos
FROM Movimentacoes m
INNER JOIN Lotes l ON l.Id = m.LoteId
WHERE l.CodigoLote = @codigoLote
ORDER BY m.Data, m.Id;

-- -------------------------------------------------------------
-- 8. Conferência de consistência
--    Lotes cujo saldo não bate com o histórico de movimentações.
--    O resultado esperado é VAZIO. Se aparecer algo, há um bug
--    na regra de movimentação (ela deve atualizar as duas tabelas
--    na mesma transação).
-- -------------------------------------------------------------
SELECT
    l.CodigoLote,
    l.Quantidade                                                                            AS SaldoNoLote,
    COALESCE(SUM(CASE WHEN m.Tipo = 'ENTRADA' THEN m.Quantidade ELSE -m.Quantidade END), 0) AS SaldoPeloHistorico
FROM Lotes l
LEFT JOIN Movimentacoes m ON m.LoteId = l.Id
GROUP BY l.Id, l.CodigoLote, l.Quantidade
HAVING l.Quantidade <> COALESCE(SUM(CASE WHEN m.Tipo = 'ENTRADA' THEN m.Quantidade ELSE -m.Quantidade END), 0);
