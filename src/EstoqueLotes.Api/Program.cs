// Program.cs é o ponto de entrada da API. Tem duas fases:
//   1) builder: registrar serviços na injeção de dependência (o que a aplicação PODE usar);
//   2) app: montar o pipeline de middlewares (por onde CADA requisição passa, em ordem).

var builder = WebApplication.CreateBuilder(args);
// CreateBuilder já carrega a configuração: appsettings.json, appsettings.{Ambiente}.json,
// user-secrets (só em Development) e variáveis de ambiente. É daí que vem a connection string.

// ---------- 1) Serviços (injeção de dependência) ----------

builder.Services.AddControllers();           // habilita os Controllers (classes com [ApiController])
builder.Services.AddEndpointsApiExplorer();  // descobre os endpoints para documentar
builder.Services.AddSwaggerGen();            // gera o documento OpenAPI usado pela tela do Swagger

var app = builder.Build();

// ---------- 2) Pipeline de middlewares (a ordem importa) ----------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();    // expõe /swagger/v1/swagger.json
    app.UseSwaggerUI();  // expõe a tela /swagger
}

app.UseHttpsRedirection();  // redireciona http -> https
app.UseAuthorization();     // ainda sem autenticação; fica pronto para o futuro
app.MapControllers();       // liga as rotas ([Route], [HttpGet]...) aos métodos dos Controllers

app.Run();
