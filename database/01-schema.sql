-- =============================================================
-- 01-schema.sql
-- Cria o banco EstoqueLotes e as 3 tabelas do projeto.
-- Rodar no SSMS (F5) conectado em localhost,1433 com o usuário sa.
-- =============================================================

CREATE DATABASE EstoqueLotes;
GO
USE EstoqueLotes;
GO

-- Um produto (ex.: "Presunto Fatiado 200g")
CREATE TABLE Produtos (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    Nome      NVARCHAR(100) NOT NULL,
    Categoria NVARCHAR(50)  NOT NULL,
    Marca     NVARCHAR(50)  NOT NULL
);

-- Um lote de produção de um produto. Relação 1:N (um produto tem vários lotes)
CREATE TABLE Lotes (
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    ProdutoId      INT NOT NULL FOREIGN KEY REFERENCES Produtos(Id),
    CodigoLote     NVARCHAR(30) NOT NULL UNIQUE,
    DataFabricacao DATE NOT NULL,
    DataValidade   DATE NOT NULL,
    Quantidade     INT  NOT NULL CHECK (Quantidade >= 0),

    -- Constraint de tabela (compara duas colunas): validade sempre depois da fabricação
    CONSTRAINT CK_Lotes_Validade CHECK (DataValidade > DataFabricacao)
);

-- Histórico de entradas e saídas de um lote (rastreabilidade)
CREATE TABLE Movimentacoes (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    LoteId     INT NOT NULL FOREIGN KEY REFERENCES Lotes(Id),
    Tipo       NVARCHAR(10) NOT NULL CHECK (Tipo IN ('ENTRADA', 'SAIDA')),
    Quantidade INT NOT NULL CHECK (Quantidade > 0),
    Data       DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

-- Índice para acelerar o relatório de lotes perto do vencimento
CREATE INDEX IX_Lotes_DataValidade ON Lotes(DataValidade);
GO
