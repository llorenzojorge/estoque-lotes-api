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

## Próximas sessões

(Novas sessões são registradas aqui.)
