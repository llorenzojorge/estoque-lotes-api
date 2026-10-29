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

## Sessão 3: solução .NET e configuração

### O que foi feito
- Criei a solução `EstoqueLotes.sln` e o projeto `src/EstoqueLotes.Api` (ASP.NET Core Web API com Controllers).
- Instalei pelo NuGet o `Microsoft.EntityFrameworkCore.SqlServer` (8.0.11) e o `Dapper` (2.1.35).
- Guardei a connection string no **user-secrets**, fora do repositório.
- Apaguei o exemplo `WeatherForecast` e comentei o `Program.cs`.
- Rodei a API e abri o Swagger em `/swagger`.

### O que aprendi
- **Solution × project:** o `.csproj` descreve um projeto e suas dependências (parecido com o `package.json`).
  A `.sln` agrupa vários projetos; aqui vão ser a API e os testes.
- **NuGet** é o gerenciador de pacotes do .NET: `dotnet add package` equivale a `npm install`.
- **Configuração em camadas:** o `CreateBuilder` junta `appsettings.json`, `appsettings.Development.json`,
  user-secrets e variáveis de ambiente. A chave `ConnectionStrings:DefaultConnection` pode vir de qualquer
  uma delas, e o código lê sempre do mesmo jeito.
- **User-secrets:** fica num arquivo no perfil do usuário (`%APPDATA%\Microsoft\UserSecrets`), só é carregado
  em Development e nunca entra no Git. O `UserSecretsId` no `.csproj` é só um identificador, não é segredo.
- **`Program.cs` tem duas fases:** registrar serviços na injeção de dependência (`builder.Services`) e montar
  o pipeline de middlewares (`app.Use...`), por onde cada requisição passa em ordem.

### Problemas que resolvi

**5. Commit sem a pasta do projeto**
- Sintoma: depois do push, o `git status` ainda mostrava `src/` como *untracked*. O commit tinha levado só o `.sln`.
- Solução: um commit novo com a pasta. Não usei `--amend` + `push --force`, porque o commit já estava no
  GitHub e reescrever histórico publicado é má prática.
- Lição: rodar `git status` **entre** o `git add` e o `git commit`.

---

## Sessão 4: CRUD de produtos em camadas

### O que foi feito
- Entidade `Produto`, `AppDbContext` (só mapeia a tabela existente, sem migrations) e os DTOs
  `ProdutoDto` (saída) e `CriarProdutoDto` (entrada, com `[Required]` e `[MaxLength]`).
- `IProdutoRepository`/`ProdutoRepository`, `IProdutoService`/`ProdutoService` e `ProdutosController`.
- Registro do `AppDbContext`, do repository e do service na injeção de dependência.
- Endpoints: `GET /api/produtos`, `GET /api/produtos/{id}`, `POST`, `PUT` e `DELETE`.

### O que aprendi
- **Camadas:** o Controller só lida com HTTP, o Service converte DTO ↔ entidade e terá as regras, e o
  Repository só fala com o banco.
- **Entidade × DTO:** a entidade espelha a tabela; o DTO é o contrato da API. O DTO de entrada não tem `Id`,
  porque quem gera o Id é o banco.
- **Validação automática:** com `[ApiController]`, um DTO inválido devolve `400` com os erros de cada campo,
  antes de chegar no meu código.
- **Change tracking do EF Core:** ao buscar uma entidade sem `AsNoTracking`, o EF vigia as alterações. No PUT
  basta mudar as propriedades e chamar `SaveChangesAsync`, que ele gera o `UPDATE` só com o que mudou.
- **`SaveChangesAsync` é a transação:** `Adicionar`/`Remover` só marcam a mudança. Tudo vai para o banco junto,
  numa transação, no `SaveChangesAsync`. Isso vai ser a base da regra de movimentação.
- **Status codes:** `201 Created` com o cabeçalho `Location` (`CreatedAtAction`), `204 No Content` no PUT e no
  DELETE, `404` quando o Id não existe.

### Problemas que encontrei

**6. Excluir um produto que tem lotes devolvia 500**
- Sintoma: `DELETE /api/produtos/1` respondeu `500 Internal Server Error`.
- Causa: o produto 1 tem lotes, e a `FOREIGN KEY` de `Lotes.ProdutoId` impede a exclusão. O banco protegeu
  os dados (é a defesa em profundidade funcionando), mas a API não traduziu o erro.
- Correção planejada: quando a entidade `Lote` existir, o service vai conferir se o produto tem lotes antes
  de excluir e responder com uma mensagem clara em vez de `500`.

**7. Id 1001 em vez de 9**
- Sintoma: o primeiro produto criado pela API recebeu o Id 1001.
- Causa: o SQL Server reserva valores de `IDENTITY` em cache (blocos de 1000) e os perde quando o serviço é
  reiniciado sem um desligamento limpo, como acontece com o container. É um comportamento documentado.
- Lição: `IDENTITY` garante valores únicos e crescentes, não sem buracos. Nunca usar o Id como contagem.

**8. Acento quebrado ao testar pelo terminal**
- Sintoma: um POST com "Laticínios" feito com `curl` no Git Bash voltou `400` dizendo que o JSON era inválido.
- Causa: o terminal não enviou o texto em UTF-8. No Swagger funcionou normalmente.
- Solução: escapar o caractere no JSON (`í`) ao testar pelo terminal.

---

## Sessão 5: lotes, movimentações e a regra de estoque

### O que foi feito
- Entidades `Lote` e `Movimentacao`, com os relacionamentos 1:N mapeados no `AppDbContext`, e o enum
  `TipoMovimentacao` (`Entrada`/`Saida`), convertido para `'ENTRADA'`/`'SAIDA'` no banco.
- `POST /api/lotes`: valida validade × fabricação, se o produto existe e se o código é único. A quantidade
  inicial vira uma `ENTRADA` no histórico, gravada junto com o lote.
- `GET /api/lotes/{id}` (com o nome do produto e se está vencido) e `GET /api/lotes/{id}/movimentacoes`.
- Tratamento global de erros: `RegraDeNegocioException` → 400, `NaoEncontradoException` → 404, qualquer outra
  → 500 sem expor detalhes internos, sempre no formato `ProblemDetails`.
- Corrigido o 500 ao excluir produto com lotes: agora responde 400 com mensagem.
- **Escrevi a regra de movimentação** (`MovimentacaoService.RegistrarAsync`): lote inexistente → 404, saída de
  lote vencido → 400, saída maior que o estoque → 400, entrada soma, saída subtrai, e tudo é gravado com um
  único `SaveChangesAsync`.

### O que aprendi
- **Classe × objeto na prática:** `Lote` é a classe; `lote` é o objeto que o repository devolveu. Eu não fiz
  `new Lote()`: quem instancia é o EF Core, que roda o `SELECT` e preenche um objeto com a linha do banco.
  Como o objeto é um `Lote`, ele tem os métodos da classe, como `EstaVencido`.
- **Namespace × import do TypeScript:** no C# o módulo é o namespace, não o arquivo. Um `using` traz todas as
  classes do namespace, e o `public` faz o papel do `export`. O `ImplicitUsings` do `.csproj` importa sozinho
  o básico (`System`, `System.Linq`...).
- **Encapsulamento:** a regra "vencido = validade antes de hoje" fica num método do próprio `Lote`, num lugar só.
- **`DateOnly` × `DateTime`:** o método pedia `DateOnly` e eu passei `DateTime.Today`. O compilador não converte
  sozinho: `DateOnly.FromDateTime(DateTime.Today)`. É a regra de comparar sempre data com data.
- **Nullable e o operador `!`:** `dto.Tipo` é `TipoMovimentacao?`. O `[Required]` garante que vem preenchido,
  mas o compilador não sabe disso; `dto.Tipo!.Value` diz "aqui não é nulo" (igual ao `!` do TypeScript).
- **Teste de mesa:** simulando a regra com números (lote com 120, entrada de 20), descobri que meu código
  subtraía sempre e depois somava na entrada, ou seja, a entrada não mudava nada. Resolvi separando os casos
  num `if`/`else`.
- **Transação pelo change tracking:** não chamo nada para salvar o lote. O EF percebe que a `Quantidade` mudou
  e, no mesmo `SaveChangesAsync`, gera o `INSERT` da movimentação e o `UPDATE` do lote numa transação.
- **Duas camadas de validação:** o formato é validado no DTO (`quantidade: 0` volta 400 antes de chegar ao
  Service); a regra de negócio, que depende do lote no banco, é validada no Service.
- **Ler erro do compilador:** `Arquivo.cs(58,20)` é linha e coluna; `CS0103` é o código do erro. C# diferencia
  maiúsculas de minúsculas (`Datetime` ≠ `DateTime`).
- **Staging area do Git:** ela é uma "foto" do próximo commit e não precisa ser igual ao disco. Usei isso para
  separar em dois commits o esqueleto do service e a regra que escrevi.

### Problemas que resolvi

**9. Rodar só um pedaço da consulta no SSMS**
- Sintoma: ao conferir a consulta de consistência, apareceram `Incorrect syntax near 'FROM'` e
  `The multi-part identifier "l.CodigoLote" could not be bound`.
- Causa: selecionei só o começo da consulta. Sem o `FROM Lotes l`, o apelido `l` não existe.
- Lição: o SQL Server processa o `FROM`/`JOIN` antes do `SELECT`, e é o `FROM` que cria os apelidos.

### Como testei
- No Swagger: saída e entrada no lote 1, saída no lote zerado (400), no lote vencido (400), no lote que vence
  hoje (201, caso de borda), lote inexistente (404) e quantidade zero (400 pela validação do DTO).
- No SSMS: a consulta de consistência voltou vazia, ou seja, o saldo de todos os lotes bate com o histórico.
  Depois rodei o seed para voltar ao estado original.

---

## Próximas sessões

(Novas sessões são registradas aqui.)
