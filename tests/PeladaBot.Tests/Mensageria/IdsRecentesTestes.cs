using Microsoft.Extensions.Time.Testing;
using PeladaBot.Mensageria;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.Mensageria;

public sealed class IdsRecentesTestes
{
    private const string IdDaMensagem = "false_120363000000001234@g.us_3EB0C767D26A1D8E0A01_35600000101@c.us";

    private readonly FakeTimeProvider _relogio = new(ConstrutorDeCasoDeUso.Agora);

    [Fact]
    public void DevePegarAMensagemQuandoOIdEhNovo()
    {
        IdsRecentes recentes = new(_relogio);

        bool pegou = recentes.TentarLembrar(IdDaMensagem);

        pegou.Should().BeTrue();
    }

    [Fact]
    public void DeveRecusarAMensagemQuandoOIdFoiPegoMenosDeDezMinutosAtras()
    {
        IdsRecentes recentes = new(_relogio);
        recentes.TentarLembrar(IdDaMensagem);
        _relogio.Advance(TimeSpan.FromMinutes(10) - TimeSpan.FromSeconds(1));

        bool pegou = recentes.TentarLembrar(IdDaMensagem);

        pegou.Should().BeFalse();
    }

    // Os IDs ficam lembrados por 10 minutos
    [Fact]
    public void DevePegarAMensagemDeNovoQuandoDezMinutosPassaram()
    {
        IdsRecentes recentes = new(_relogio);
        recentes.TentarLembrar(IdDaMensagem);
        _relogio.Advance(TimeSpan.FromMinutes(10));

        bool pegou = recentes.TentarLembrar(IdDaMensagem);

        pegou.Should().BeTrue();
    }

    // Cada ID tem a sua lembrança
    [Fact]
    public void DevePegarOutraMensagemQuandoSoAPrimeiraFoiPega()
    {
        IdsRecentes recentes = new(_relogio);
        recentes.TentarLembrar(IdDaMensagem);

        bool pegou = recentes.TentarLembrar("false_120363000000001234@g.us_3EB0C767D26A1D8E0A02_35600000101@c.us");

        pegou.Should().BeTrue();
    }
}
