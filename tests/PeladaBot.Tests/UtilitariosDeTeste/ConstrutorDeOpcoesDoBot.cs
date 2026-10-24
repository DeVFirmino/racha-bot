using PeladaBot.Configuracao;

namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>Constrói um OpcoesDoBot válido com os limites da spec. Cada método With muda uma parte, para um teste mostrar só o que quebra.</summary>
public sealed class ConstrutorDeOpcoesDoBot
{
    /// <summary>O id do grupo que o construtor usa quando o teste não escolhe outro.</summary>
    public const string IdDoGrupo = "120363000000001234@g.us";

    private int _quantidadeDeGrupos = 1;
    private string _idDoGrupo = IdDoGrupo;
    private OpcoesDoMembroDaPresidencia[] _presidencia =
        [Membro("35600000101@c.us", "100000000000202@lid"), Membro("35600000303@c.us")];
    private string[] _frasesDoRodape = ["⏰ Sem atraso hoje, hein!"];
    private string[] _mensagensDeCobranca = ["🚨 *Quem ainda não pagou, favor acertar!*"];
    private OpcoesDoModeloDaLegenda[] _modelosDaLegenda = [ModeloDaLegenda("⚽ *PELADA*", "{relogio} *SORTEIO — {hora}*")];
    private OpcoesDosApelidosAvulsos _apelidosAvulsos = new();
    private string _tituloDoCard = string.Empty;
    private string _caminhoDoEscudo = string.Empty;
    private TimeSpan _janelaDeRespostaNaoPresidente = TimeSpan.FromMinutes(10);
    private TimeSpan _janelaDeCobranca = TimeSpan.FromMinutes(30);
    private int _maximoDeMensagensDoDisjuntor = 30;
    private TimeSpan _janelaDoDisjuntor = TimeSpan.FromMinutes(60);
    private TimeSpan _idadeMaximaDoComando = TimeSpan.FromSeconds(120);
    private TimeSpan _idadeMaximaDaLista = TimeSpan.FromHours(72);

    public static OpcoesDoMembroDaPresidencia Membro(params string[] ids) => new() { Nome = "Presidente", Ids = ids };

    /// <summary>Um tema da legenda com o cabeçalho e a linha do horário escolhidos, e um apelido em cada cor.</summary>
    public static OpcoesDoModeloDaLegenda ModeloDaLegenda(string cabecalho, string linhaDoHorario) => new()
    {
        Cabecalho = cabecalho,
        LinhaDoHorario = linhaDoHorario,
        ApelidoLaranja = "OS DA LARANJA",
        ApelidoVerde = "OS DA VERDE",
        ApelidoCinza = "OS DA CINZA",
        ApelidoAzul = "OS DA AZUL",
        ApelidoSemColete = "OS SEM COLETE",
    };

    public ConstrutorDeOpcoesDoBot ComQuantidadeDeGrupos(int quantidadeDeGrupos)
    {
        _quantidadeDeGrupos = quantidadeDeGrupos;
        return this;
    }

    public ConstrutorDeOpcoesDoBot ComIdDoGrupo(string idDoGrupo)
    {
        _idDoGrupo = idDoGrupo;
        return this;
    }

    public ConstrutorDeOpcoesDoBot ComPresidencia(params OpcoesDoMembroDaPresidencia[] membros)
    {
        _presidencia = membros;
        return this;
    }

    public ConstrutorDeOpcoesDoBot ComFrasesDoRodape(params string[] frases)
    {
        _frasesDoRodape = frases;
        return this;
    }

    public ConstrutorDeOpcoesDoBot ComApelidosAvulsos(OpcoesDosApelidosAvulsos apelidos)
    {
        _apelidosAvulsos = apelidos;
        return this;
    }

    public ConstrutorDeOpcoesDoBot ComModelosDaLegenda(params OpcoesDoModeloDaLegenda[] modelos)
    {
        _modelosDaLegenda = modelos;
        return this;
    }

    public ConstrutorDeOpcoesDoBot ComMensagensDeCobranca(params string[] mensagens)
    {
        _mensagensDeCobranca = mensagens;
        return this;
    }

    public ConstrutorDeOpcoesDoBot ComMaximoDeMensagensDoDisjuntor(int maximoDeMensagens)
    {
        _maximoDeMensagensDoDisjuntor = maximoDeMensagens;
        return this;
    }

    /// <summary>Define um dos limites de duração, nomeado pela sua propriedade de <see cref="OpcoesDeLimites"/>.</summary>
    public ConstrutorDeOpcoesDoBot ComLimiteDeDuracao(string limite, TimeSpan valor)
    {
        switch (limite)
        {
            case nameof(OpcoesDeLimites.JanelaDeRespostaNaoPresidente):
                _janelaDeRespostaNaoPresidente = valor;
                break;
            case nameof(OpcoesDeLimites.JanelaDeCobranca):
                _janelaDeCobranca = valor;
                break;
            case nameof(OpcoesDeLimites.JanelaDoDisjuntor):
                _janelaDoDisjuntor = valor;
                break;
            case nameof(OpcoesDeLimites.IdadeMaximaDoComando):
                _idadeMaximaDoComando = valor;
                break;
            case nameof(OpcoesDeLimites.IdadeMaximaDaLista):
                _idadeMaximaDaLista = valor;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(limite), limite, "Limite de duração desconhecido.");
        }

        return this;
    }

    /// <summary>O nome e o caminho do escudo no topo do card.</summary>
    public ConstrutorDeOpcoesDoBot ComIdentidadeDoCard(string titulo, string caminhoDoEscudo)
    {
        _tituloDoCard = titulo;
        _caminhoDoEscudo = caminhoDoEscudo;

        return this;
    }

    public OpcoesDoBot Build()
    {
        OpcoesDoGrupo grupo = new()
        {
            Id = _idDoGrupo,
            Presidencia = _presidencia,
            FrasesDoRodape = _frasesDoRodape,
            MensagensDeCobranca = _mensagensDeCobranca,
            ModelosDaLegenda = _modelosDaLegenda,
            ApelidosAvulsos = _apelidosAvulsos,
            TituloDoCard = _tituloDoCard,
            CaminhoDoEscudo = _caminhoDoEscudo,
        };

        return new OpcoesDoBot
        {
            // A suíte gravou o relógio com as regras de horário de verão da UE. O exemplo de produção é America/Sao_Paulo.
            Fuso = "Europe/Rome",
            Grupos = [.. Enumerable.Repeat(grupo, _quantidadeDeGrupos)],
            Limites = new OpcoesDeLimites
            {
                JanelaDeRespostaNaoPresidente = _janelaDeRespostaNaoPresidente,
                JanelaDeCobranca = _janelaDeCobranca,
                MaximoDeMensagensDoDisjuntor = _maximoDeMensagensDoDisjuntor,
                JanelaDoDisjuntor = _janelaDoDisjuntor,
                IdadeMaximaDoComando = _idadeMaximaDoComando,
                IdadeMaximaDaLista = _idadeMaximaDaLista,
            },
        };
    }
}
