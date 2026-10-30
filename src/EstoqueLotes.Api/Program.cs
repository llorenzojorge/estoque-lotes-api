// Program.cs é o ponto de entrada da API. Tem duas fases:
//   1) builder: registrar serviços na injeção de dependência (o que a aplicação PODE usar);
//   2) app: montar o pipeline de middlewares (por onde CADA requisição passa, em ordem).

using System.Text.Json.Serialization;
using Dapper;
using EstoqueLotes.Api.Data;
using EstoqueLotes.Api.Errors;
using EstoqueLotes.Api.Repositories;
using EstoqueLotes.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// CreateBuilder já carrega a configuração: appsettings.json, appsettings.{Ambiente}.json,
// user-secrets (só em Development) e variáveis de ambiente. É daí que vem a connection string.

// ---------- 1) Serviços (injeção de dependência) ----------

// DbContext do EF Core. AddDbContext já registra como Scoped: uma instância por requisição HTTP.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// "Quando alguém pedir a interface, entregue esta classe." Scoped = uma instância por requisição.
// Todos os repositories da mesma requisição recebem o mesmo AppDbContext.
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<ILoteRepository, LoteRepository>();
builder.Services.AddScoped<IMovimentacaoRepository, MovimentacaoRepository>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<ILoteService, LoteService>();
builder.Services.AddScoped<IMovimentacaoService, MovimentacaoService>();

// Relatórios: Dapper com SQL puro (abre a própria SqlConnection, não usa o AppDbContext)
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());   // ensina o Dapper a converter DATE <-> DateOnly
builder.Services.AddScoped<IRelatorioRepository, RelatorioRepository>();
builder.Services.AddScoped<IRelatorioService, RelatorioService>();

// Tratamento global de erros: exceções -> ProblemDetails (404, 400 ou 500)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers()            // habilita os Controllers (classes com [ApiController])
    .AddJsonOptions(options =>
        // enums no JSON como texto ("Saida") em vez de número (1)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();  // descobre os endpoints para documentar
builder.Services.AddSwaggerGen();            // gera o documento OpenAPI usado pela tela do Swagger

var app = builder.Build();

// ---------- 2) Pipeline de middlewares (a ordem importa) ----------

app.UseExceptionHandler();  // primeiro: envolve todo o resto e captura exceções de qualquer etapa

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();    // expõe /swagger/v1/swagger.json
    app.UseSwaggerUI();  // expõe a tela /swagger
}

app.UseHttpsRedirection();  // redireciona http -> https
app.UseAuthorization();     // ainda sem autenticação; fica pronto para o futuro
app.MapControllers();       // liga as rotas ([Route], [HttpGet]...) aos métodos dos Controllers

app.Run();
