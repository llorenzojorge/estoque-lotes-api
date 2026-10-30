using EstoqueLotes.Api.Dtos;
using EstoqueLotes.Api.Errors;
using EstoqueLotes.Api.Models;
using EstoqueLotes.Api.Repositories;
using EstoqueLotes.Api.Services;
using Moq;

namespace EstoqueLotes.Tests.Services;

// Testes unitários da regra de movimentação. Não há banco: os repositories são MOCKS (objetos falsos
// criados pelo Moq a partir das interfaces). Isso só é possível porque o MovimentacaoService depende
// de ILoteRepository e IMovimentacaoRepository, e não das classes concretas que usam o EF Core.
// Padrão de cada teste: Arrange (preparar) / Act (executar) / Assert (conferir).
public class MovimentacaoServiceTests
{
    private readonly Mock<ILoteRepository> _loteRepositoryMock = new();
    private readonly Mock<IMovimentacaoRepository> _movimentacaoRepositoryMock = new();
    private readonly MovimentacaoService _service;

    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    // O xUnit cria uma instância NOVA da classe para cada teste: os mocks nunca são compartilhados.
    public MovimentacaoServiceTests()
    {
        _service = new MovimentacaoService(_loteRepositoryMock.Object, _movimentacaoRepositoryMock.Object);
    }

    // Lote de teste: por padrão válido (vence daqui a 30 dias) e com 100 unidades.
    private static Lote CriarLote(int quantidade = 100, int diasParaVencer = 30) => new()
    {
        Id = 1,
        ProdutoId = 1,
        CodigoLote = "TESTE-001",
        DataFabricacao = Hoje.AddDays(-60),
        DataValidade = Hoje.AddDays(diasParaVencer),
        Quantidade = quantidade
    };

    // "Quando o service pedir o lote 1 ao repository, devolva este objeto."
    private void ConfigurarLote(Lote lote) =>
        _loteRepositoryMock.Setup(r => r.ObterPorIdAsync(lote.Id)).ReturnsAsync(lote);

    // ---------- Exemplo 1: caminho feliz ----------

    [Fact]
    public async Task Entrada_SomaNaQuantidadeDoLote()
    {
        // Arrange
        var lote = CriarLote(quantidade: 100);
        ConfigurarLote(lote);
        var dto = new CriarMovimentacaoDto { LoteId = 1, Tipo = TipoMovimentacao.Entrada, Quantidade = 30 };

        // Act
        await _service.RegistrarAsync(dto);

        // Assert
        Assert.Equal(130, lote.Quantidade);
        // E a movimentação foi adicionada e salva (uma vez só = uma transação)
        _movimentacaoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Movimentacao>()), Times.Once);
        _movimentacaoRepositoryMock.Verify(r => r.SalvarAlteracoesAsync(), Times.Once);
    }

    // ---------- Exemplo 2: exceção ----------

    [Fact]
    public async Task LoteInexistente_LancaNaoEncontradoException()
    {
        // Arrange: sem Setup, o mock devolve null para qualquer id (é o padrão do Moq)
        var dto = new CriarMovimentacaoDto { LoteId = 999, Tipo = TipoMovimentacao.Saida, Quantidade = 10 };

        // Act + Assert: o teste passa SE a exceção for lançada
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _service.RegistrarAsync(dto));

        // Nada pode ter sido salvo
        _movimentacaoRepositoryMock.Verify(r => r.SalvarAlteracoesAsync(), Times.Never);
    }

    [Fact]
    public async Task Saida_SubtraiDaQuantidadeDoLote()
    {
        // Arrange: lote com 100, saída de 40
        var lote = CriarLote(quantidade: 100);
        ConfigurarLote(lote);
        var dto = new CriarMovimentacaoDto { LoteId = 1, Tipo = TipoMovimentacao.Saida, Quantidade = 40 };

        // Act
        await _service.RegistrarAsync(dto);

        // Assert: quanto deve sobrar no lote?
        Assert.Equal(60, lote.Quantidade);

        // Garante que a movimentação foi add e salva uma única vez.
        _movimentacaoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Movimentacao>()), Times.Once);
        _movimentacaoRepositoryMock.Verify(r => r.SalvarAlteracoesAsync(), Times.Once);
    }

    [Fact]
    public async Task SaidaMaiorQueEstoque_LancaRegraDeNegocioException()
    {
        // Arrange: lote com POUCO estoque e uma saída maior que ele
        var lote = CriarLote(quantidade: 50);
        ConfigurarLote(lote);
        var dto = new CriarMovimentacaoDto { LoteId = 1, Tipo = TipoMovimentacao.Saida, Quantidade = 100 };

        // Act + Assert: qual exceção? (veja o Exemplo 2)
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => _service.RegistrarAsync(dto));

        Assert.Equal(50, lote.Quantidade);

        // Bônus: o saldo do lote não pode ter mudado. Como conferir?
        _movimentacaoRepositoryMock.Verify(r => r.SalvarAlteracoesAsync(), Times.Never);

    }

    [Fact]
    public async Task SaidaDeLoteVencido_LancaRegraDeNegocioException()
    {
        // Arrange: como criar um lote que venceu ONTEM? (olhe os parâmetros do CriarLote)
        var lote = CriarLote(quantidade: 100, diasParaVencer: -1);   // venceu ontem
        ConfigurarLote(lote);
        var dto = new CriarMovimentacaoDto { LoteId = 1, Tipo = TipoMovimentacao.Saida, Quantidade = 10 };

        // Act + Assert
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => _service.RegistrarAsync(dto));

    }
}
