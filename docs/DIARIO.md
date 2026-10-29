# Diário de desenvolvimento

Registro do que foi feito, do que aprendi e dos problemas que resolvi ao longo do projeto.

---

## Sessão 1: ambiente e banco de dados

### O que foi feito
- Instalei o **.NET SDK 8** (versão 8.0.427) com `winget install Microsoft.DotNet.SDK.8`.
  Escolhi o 8 por ser **LTS** (suporte longo), o mais usado nas empresas.
- Instalei a extensão **C# Dev Kit** no VS Code.
- Instalei o **SSMS 20** (SQL Server Management Studio), a ferramenta padrão para administrar o SQL Server.
- Baixei a imagem do **SQL Server 2022** no Docker e subi o banco com um `docker-compose.yml`.
- Guardei a senha do usuário `sa` num arquivo `.env`, que está no `.gitignore`.
- Conectei no banco pelo SSMS e criei o banco `EstoqueLotes` com 3 tabelas, escrevendo o SQL à mão
  (`database/01-schema.sql`).

### O que aprendi
- **Container:** um ambiente isolado e descartável. Rodo o SQL Server sem instalar nada no Windows.
  Se der problema, apago e subo de novo.
- **docker-compose.yml:** descreve os serviços. `image` (o software), `environment` (configurações como a senha),
  `ports` (`1433:1433` liga a porta do container à do meu PC) e `volumes` (dados persistem mesmo se o container for recriado).
- **Comandos:** `docker compose up -d` liga, `docker compose down` desliga, `docker ps` lista os containers e
  `docker logs <nome>` mostra os logs.
- **Modelagem:** `PRIMARY KEY`, `FOREIGN KEY` (relacionamento 1:N), `IDENTITY` (auto incremento),
  `NOT NULL`, `UNIQUE`, `CHECK` (regra validada pelo próprio banco), `DEFAULT`, `NVARCHAR` (texto com acentos), `GO`.
- **Índice:** criei `IX_Lotes_DataValidade` porque o relatório de vencimento filtra por essa coluna.

### Problemas que resolvi

**1. "Login failed for user 'sa'" logo depois de subir o container**
- Sintoma: a primeira tentativa de conexão falhou com erro 18456.
- Investigação: li os logs com `docker logs estoque-sqlserver` e vi que o servidor ainda estava inicializando.
  A mensagem "SQL Server is now ready for client connections" só apareceu alguns segundos depois.
- Solução: esperar o servidor ficar pronto. Na segunda tentativa conectou.
- Lição: **ler os logs antes de sair mudando configuração**.

**2. "The certificate chain was issued by an authority that is not trusted" no SSMS**
- Sintoma: o SSMS não conectava.
- Investigação: a mensagem dizia que a conexão **foi estabelecida** e o problema estava no certificado.
  Então rede, porta e senha estavam certas.
- Causa: o SQL Server do container usa um certificado **autoassinado**, e o SSMS exige conexão criptografada
  com certificado confiável.
- Solução: marcar **Trust server certificate** (só em ambiente local; em produção o servidor teria um certificado válido).
  Na API, o equivalente é `TrustServerCertificate=True` na connection string.
- Lição: ler a mensagem de erro inteira me disse exatamente onde estava o problema.

---

## Sessão 2: integridade, dados de exemplo e consultas

### O que foi feito
- Adicionei ao schema a constraint `CK_Lotes_Validade CHECK (DataValidade > DataFabricacao)`.
- Escrevi `database/02-seed.sql`: 8 produtos, 11 lotes e 21 movimentações, cada lote planejado para
  cobrir um cenário (vencido, vence hoje, vencendo em poucos dias, estoque zerado, produto sem lote…).
- Escrevi `database/03-consultas.sql` com 8 consultas: lotes vencendo, lotes vencidos com estoque,
  estoque por categoria, produtos abaixo do estoque mínimo, produtos sem lote, top 5 lotes com mais saídas,
  histórico de um lote com saldo acumulado e conferência de consistência.

### O que aprendi
- **Constraint de coluna × constraint de tabela:** a de coluna só pode olhar a própria coluna. Para comparar
  duas colunas (validade × fabricação), a constraint precisa ser de tabela, declarada depois das colunas.
- **Defesa em profundidade:** a API vai validar as regras e responder com uma mensagem amigável, mas o banco
  também valida, como última barreira contra dados inválidos.
- **Script idempotente:** o seed apaga tudo (na ordem inversa das FKs) e insere de novo. Rodar 1 ou 10 vezes
  dá o mesmo resultado.
- **`IDENTITY` não volta sozinho:** depois de um `DELETE`, o próximo Id continua de onde parou.
  `DBCC CHECKIDENT (..., RESEED, 0)` reinicia o contador. Pegadinha: numa tabela que **nunca** teve linhas,
  `RESEED 0` faz o primeiro Id ser 0, e não 1. Por isso o seed só reinicia quando `sys.identity_columns.last_value`
  não é nulo.
- **Transação no script:** `BEGIN TRANSACTION` + `SET XACT_ABORT ON` + `COMMIT`. Se qualquer comando falhar,
  nada é gravado pela metade.
- **Datas relativas:** `DATEADD(DAY, n, @hoje)` em vez de datas fixas, para os cenários continuarem válidos
  em qualquer dia. `CAST(GETDATE() AS DATE)` tira a hora; comparar `DATE` com `DATETIME` faria um lote que
  vence hoje "sumir" do relatório depois da meia-noite.
- **Denormalização:** `Lotes.Quantidade` é um dado derivado (entradas − saídas). Ler fica rápido, mas a
  consistência precisa ser garantida. O seed confere isso antes do `COMMIT` e a consulta 8 audita.
- **`WHERE` × `HAVING`:** `WHERE` filtra linhas antes de agrupar; `HAVING` filtra os grupos depois.
- **`LEFT JOIN` + `COALESCE`:** traz linhas sem correspondência (categoria sem lote) e troca o `NULL` da soma por 0.
- **Consultas que aproveitam índice:** comparar a coluna "pura" (`DataValidade BETWEEN ...`) permite usar o
  índice. Envolver a coluna numa função (`DATEDIFF(..., DataValidade)`) impede. Com poucas linhas o
  otimizador pode preferir ler a tabela toda, e isso é normal.
- **Window function:** `SUM(...) OVER (ORDER BY ...)` calcula o saldo acumulado movimento a movimento.

### Problemas que resolvi

**3. Constraint de validade sem vírgula**
- Sintoma: ao adicionar a constraint dentro do `CREATE TABLE`, esqueci a vírgula depois da última coluna.
- Causa: sem a vírgula, o SQL Server entende a constraint como sendo da coluna `Quantidade`, e uma constraint
  de coluna não pode citar outras colunas.
- Solução: vírgula depois da coluna. Validei o script inteiro num banco descartável antes de dar como pronto.

**4. Índice faltando no banco local**
- Sintoma: comparando o banco com o `01-schema.sql`, o índice `IX_Lotes_DataValidade` não existia.
- Causa: o banco local tinha sido criado com uma versão do script anterior ao índice.
- Solução: criei o índice. Lição: o arquivo do repositório é a fonte da verdade, e o banco precisa ser conferido contra ele.

### Como testei
- Rodei o seed 2 vezes seguidas: mesmas contagens e Ids começando em 1 nas duas execuções.
- Provoquei dois erros de propósito numa cópia do seed (saldo inconsistente e validade antes da fabricação):
  nos dois casos o script abortou e os dados anteriores ficaram intactos.
- Conferi o resultado de cada consulta contra o esperado pelos cenários do seed.

---

## Próximas sessões

(Novas sessões são registradas aqui.)
