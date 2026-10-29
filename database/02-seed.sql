-- =============================================================
-- 02-seed.sql
-- Popula o banco EstoqueLotes com dados de exemplo.
-- Rodar no SSMS (F5) depois do 01-schema.sql.
--
-- Características:
--   * Idempotente: pode rodar quantas vezes quiser; sempre termina
--     com exatamente os mesmos dados (apaga tudo e insere de novo).
--   * Transacional: se qualquer comando falhar, nada é gravado.
--   * Datas relativas a hoje: os cenários (vencido, vencendo, etc.)
--     continuam válidos em qualquer dia em que o script for rodado.
--   * Autoverificação: antes do COMMIT confere se o saldo de cada lote
--     bate com o histórico de movimentações.
--
-- Marcas são fictícias.
-- =============================================================

USE EstoqueLotes;
GO

SET NOCOUNT ON;     -- não mostrar "(N rows affected)" a cada comando
SET XACT_ABORT ON;  -- qualquer erro desfaz a transação inteira

BEGIN TRANSACTION;

-- -------------------------------------------------------------
-- 1. Limpeza
-- Apaga na ordem inversa das FKs: primeiro quem depende (netos),
-- por último quem é referenciado (pais).
-- -------------------------------------------------------------
DELETE FROM Movimentacoes;
DELETE FROM Lotes;
DELETE FROM Produtos;

-- Reinicia os contadores IDENTITY para os Ids voltarem a começar em 1.
-- Só quando a tabela já gerou algum Id (last_value não nulo): numa tabela
-- que nunca teve linhas, RESEED 0 faria o primeiro Id ser 0, e não 1.
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('Movimentacoes') AND last_value IS NOT NULL)
    DBCC CHECKIDENT ('Movimentacoes', RESEED, 0) WITH NO_INFOMSGS;
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('Lotes') AND last_value IS NOT NULL)
    DBCC CHECKIDENT ('Lotes', RESEED, 0) WITH NO_INFOMSGS;
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('Produtos') AND last_value IS NOT NULL)
    DBCC CHECKIDENT ('Produtos', RESEED, 0) WITH NO_INFOMSGS;

-- Data de referência. Todas as datas abaixo são calculadas a partir dela.
-- CAST para DATE remove a hora (as colunas de lote são do tipo DATE).
DECLARE @hoje DATE = CAST(GETDATE() AS DATE);

-- -------------------------------------------------------------
-- 2. Produtos
-- Os Ids são gerados na ordem de inserção (1 a 8) e usados nos lotes.
-- -------------------------------------------------------------
INSERT INTO Produtos (Nome, Categoria, Marca) VALUES
    (N'Presunto Cozido Fatiado 200g',     N'Frios',      N'Sabor do Campo'),  -- 1
    (N'Mortadela Defumada Fatiada 200g',  N'Frios',      N'Fazenda Boa'),     -- 2
    (N'Salsicha Hot Dog 500g',            N'Embutidos',  N'Fazenda Boa'),     -- 3
    (N'Linguiça Toscana 1kg',             N'Embutidos',  N'Sabor do Campo'),  -- 4
    (N'Peito de Frango Resfriado 1kg',    N'Aves',       N'Granja Serena'),   -- 5
    (N'Coxa e Sobrecoxa Congelada 1kg',   N'Aves',       N'Granja Serena'),   -- 6
    (N'Lasanha à Bolonhesa 600g',         N'Congelados', N'Fazenda Boa'),     -- 7
    (N'Requeijão Cremoso 200g',           N'Laticínios', N'Granja Serena');   -- 8: propositalmente SEM lote

-- -------------------------------------------------------------
-- 3. Lotes
-- Cada lote cobre um cenário de teste das consultas e das regras da API.
-- Regra de vencimento usada no projeto: o lote é VENCIDO quando
-- DataValidade < hoje. No próprio dia da validade ele ainda é válido.
-- -------------------------------------------------------------
INSERT INTO Lotes (ProdutoId, CodigoLote, DataFabricacao, DataValidade, Quantidade) VALUES
    -- Vence em 7 dias: aparece no relatório "vencendo em 30 dias"
    (1, N'L2026-0001', DATEADD(DAY,   -20, @hoje), DATEADD(DAY,    7, @hoje), 120),
    -- Vence em 40 dias: NÃO aparece no relatório de 30 dias
    (1, N'L2026-0002', DATEADD(DAY,    -5, @hoje), DATEADD(DAY,   40, @hoje), 300),
    -- VENCIDO há 3 dias e ainda com estoque: saída deve ser bloqueada
    (2, N'L2026-0003', DATEADD(DAY,   -45, @hoje), DATEADD(DAY,   -3, @hoje),  50),
    -- Vence HOJE: ainda válido (caso de borda da comparação de datas)
    (2, N'L2026-0004', DATEADD(DAY,   -30, @hoje), @hoje,                      40),
    -- Estoque ZERADO: qualquer saída deve falhar por estoque insuficiente
    (3, N'L2026-0005', DATEADD(DAY,   -30, @hoje), DATEADD(DAY,   25, @hoje),   0),
    -- Lote normal
    (3, N'L2026-0006', DATEADD(DAY,   -10, @hoje), DATEADD(DAY,   50, @hoje), 180),
    -- Lote normal
    (4, N'L2026-0007', DATEADD(DAY,   -15, @hoje), DATEADD(DAY,   45, @hoje),  95),
    -- Resfriado, validade curta: vence em 8 dias
    (5, N'L2026-0008', DATEADD(DAY,    -2, @hoje), DATEADD(DAY,    8, @hoje),  90),
    -- Congelado, validade longa
    (6, N'L2026-0009', DATEADD(MONTH,  -1, @hoje), DATEADD(MONTH, 11, @hoje), 400),
    -- Dois lotes do mesmo produto, um deles fabricado hoje
    (7, N'L2026-0010', DATEADD(MONTH,  -2, @hoje), DATEADD(MONTH, 10, @hoje), 250),
    (7, N'L2026-0011', @hoje,                      DATEADD(MONTH, 12, @hoje), 500);

-- -------------------------------------------------------------
-- 4. Movimentações (histórico de cada lote)
-- Todo lote tem uma ENTRADA (a produção) na data de fabricação.
-- Saldo do lote = soma das ENTRADAS - soma das SAÍDAS.
-- As saídas ficam entre a fabricação e hoje (e antes da validade).
-- -------------------------------------------------------------
INSERT INTO Movimentacoes (LoteId, Tipo, Quantidade, Data) VALUES
    ( 1, N'ENTRADA', 200, DATEADD(DAY,   -20, @hoje)),
    ( 1, N'SAIDA',    50, DATEADD(DAY,   -12, @hoje)),
    ( 1, N'SAIDA',    30, DATEADD(DAY,    -4, @hoje)),   -- saldo 120

    ( 2, N'ENTRADA', 300, DATEADD(DAY,    -5, @hoje)),   -- saldo 300

    ( 3, N'ENTRADA', 180, DATEADD(DAY,   -45, @hoje)),
    ( 3, N'SAIDA',   130, DATEADD(DAY,   -10, @hoje)),   -- saldo 50

    ( 4, N'ENTRADA',  60, DATEADD(DAY,   -30, @hoje)),
    ( 4, N'SAIDA',    20, DATEADD(DAY,    -2, @hoje)),   -- saldo 40

    ( 5, N'ENTRADA', 150, DATEADD(DAY,   -30, @hoje)),
    ( 5, N'SAIDA',   100, DATEADD(DAY,   -20, @hoje)),
    ( 5, N'SAIDA',    50, DATEADD(DAY,    -6, @hoje)),   -- saldo 0

    ( 6, N'ENTRADA', 200, DATEADD(DAY,   -10, @hoje)),
    ( 6, N'SAIDA',    20, DATEADD(DAY,    -3, @hoje)),   -- saldo 180

    ( 7, N'ENTRADA', 120, DATEADD(DAY,   -15, @hoje)),
    ( 7, N'SAIDA',    25, DATEADD(DAY,    -7, @hoje)),   -- saldo 95

    ( 8, N'ENTRADA', 140, DATEADD(DAY,    -2, @hoje)),
    ( 8, N'SAIDA',    50, DATEADD(DAY,    -1, @hoje)),   -- saldo 90

    ( 9, N'ENTRADA', 400, DATEADD(MONTH,  -1, @hoje)),   -- saldo 400

    (10, N'ENTRADA', 300, DATEADD(MONTH,  -2, @hoje)),
    (10, N'SAIDA',    50, DATEADD(DAY,   -15, @hoje)),   -- saldo 250

    (11, N'ENTRADA', 500, @hoje);                        -- saldo 500

-- -------------------------------------------------------------
-- 5. Autoverificação
-- Se algum lote tiver saldo diferente do histórico, aborta tudo
-- (THROW + XACT_ABORT desfazem a transação).
-- -------------------------------------------------------------
IF EXISTS (
    SELECT 1
    FROM Lotes l
    LEFT JOIN Movimentacoes m ON m.LoteId = l.Id
    GROUP BY l.Id, l.Quantidade
    HAVING l.Quantidade <> COALESCE(SUM(CASE WHEN m.Tipo = 'ENTRADA' THEN m.Quantidade ELSE -m.Quantidade END), 0)
)
    THROW 50001, N'Seed inconsistente: saldo de algum lote não bate com as movimentações.', 1;

COMMIT TRANSACTION;

-- -------------------------------------------------------------
-- 6. Conferência visual
-- -------------------------------------------------------------
SELECT * FROM Produtos;
SELECT * FROM Lotes ORDER BY DataValidade;
SELECT * FROM Movimentacoes ORDER BY LoteId, Data;
