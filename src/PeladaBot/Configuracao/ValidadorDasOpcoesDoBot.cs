using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PeladaBot.Respostas;

namespace PeladaBot.Configuracao;

/// <summary>Rejeita uma configuração inválida; ValidateOnStart então recusa iniciar e loga o motivo.</summary>
public sealed class ValidadorDasOpcoesDoBot : IValidateOptions<OpcoesDoBot>
{
    public const string FusoEmBranco = "Bot:Fuso não pode ser vazio.";
    public const string FusoDesconhecido = "Bot:Fuso não é um fuso da tzdata; encontrado {0}.";
    public const string QuantidadeDeGruposErrada = "Bot:Grupos deve ter exatamente um grupo; a v1 suporta só um; encontrado {0}.";
    public const string IdDoGrupoEmBranco = "Bot:Grupos:{0}:Id não pode ser vazio.";
    public const string IdDoGrupoComEspacos = "Bot:Grupos:{0}:Id não pode começar ou terminar com espaço.";
    public const string TamanhoDaPresidenciaErrado = "Bot:Grupos:{0}:Presidencia deve ter 1 ou 2 membros; encontrado {1}.";
    public const string MembroSemIds = "Bot:Grupos:{0}:Presidencia:{1}:Ids deve ter ao menos um id do WhatsApp.";
    public const string IdDoMembroEmBranco = "Bot:Grupos:{0}:Presidencia:{1}:Ids:{2} não pode ser vazio.";
    public const string IdDoMembroComEspacos =
        "Bot:Grupos:{0}:Presidencia:{1}:Ids:{2} não pode começar ou terminar com espaço.";
    public const string SemFrasesDeRodape = "Bot:Grupos:{0}:FrasesDoRodape deve ter ao menos uma frase.";
    public const string FraseDeRodapeEmBranco = "Bot:Grupos:{0}:FrasesDoRodape:{1} não pode ser vazia.";
    public const string SemModelosDaLegenda = "Bot:Grupos:{0}:ModelosDaLegenda deve ter ao menos um modelo.";
    public const string CampoDoModeloEmBranco = "Bot:Grupos:{0}:ModelosDaLegenda:{1}:{2} não pode ser vazio.";
    public const string ApelidoAvulsoEmBranco = "Bot:Grupos:{0}:ApelidosAvulsos:{1}:{2} não pode ser vazio.";
    public const string LinhaDoHorarioSemHora = "Bot:Grupos:{0}:ModelosDaLegenda:{1}:LinhaDoHorario deve ter {{hora}}.";
    public const string SemMensagensDeCobranca = "Bot:Grupos:{0}:MensagensDeCobranca deve ter ao menos uma mensagem.";
    public const string MensagemDeCobrancaEmBranco = "Bot:Grupos:{0}:MensagensDeCobranca:{1} não pode ser vazia.";
    public const string MarcaDesconhecidaNaCobranca =
        "Bot:Grupos:{0}:MensagensDeCobranca:{1} tem a marca {2}; só valem {{nome}}, {{quantidade}}, {{s}}, {{valor}} e {{link}}.";
    public const string EscudoInexistente = "Bot:Grupos:{0}:CaminhoDoEscudo aponta para um arquivo que não existe.";
    public const string LimiteNaoPositivo = "Bot:Limites:{0} deve ser maior que zero; encontrado {1}.";
    public const string DuracaoMuitoLonga = "Bot:Limites:{0} deve ser no máximo {1} (d.hh:mm:ss); encontrado {2}.";
    public const string QuantidadeMuitoGrande = "Bot:Limites:{0} deve ser no máximo {1}; encontrado {2}.";

    private const int QuantidadeDeGruposSuportada = 1;
    private const int MinimoDeMembrosDaPresidencia = 1;
    private const int MaximoDeMembrosDaPresidencia = 2;
    private const int MaximoDeMensagensDoDisjuntor = 1000;

    private static readonly Regex _marca = new(@"\{[^{}\n]*\}", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly TimeSpan _janelaDeRespostaMaxima = TimeSpan.FromDays(1);
    private static readonly TimeSpan _janelaDoDisjuntorMaxima = TimeSpan.FromDays(1);
    private static readonly TimeSpan _idadeMaximaDoComando = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan _idadeMaximaDaLista = TimeSpan.FromDays(7);

    public ValidateOptionsResult Validate(string? name, OpcoesDoBot options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> falhas = [];

        ValidarFuso(options.Fuso, falhas);
        ValidarGrupos(options.Grupos, falhas);
        ValidarLimites(options.Limites, falhas);

        return falhas.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(falhas);
    }

    private static void ValidarFuso(string fuso, List<string> falhas)
    {
        if (string.IsNullOrWhiteSpace(fuso))
        {
            falhas.Add(FusoEmBranco);
            return;
        }

        try
        {
            _ = HorarioDoGrupo.Carregar(fuso);
        }
        catch (TimeZoneNotFoundException)
        {
            falhas.Add(Formatar(FusoDesconhecido, fuso));
        }
        catch (InvalidTimeZoneException)
        {
            falhas.Add(Formatar(FusoDesconhecido, fuso));
        }
    }

    private static void ValidarGrupos(IReadOnlyList<OpcoesDoGrupo> grupos, List<string> falhas)
    {
        if (grupos.Count != QuantidadeDeGruposSuportada)
        {
            falhas.Add(Formatar(QuantidadeDeGruposErrada, grupos.Count));
        }

        for (int indiceDoGrupo = 0; indiceDoGrupo < grupos.Count; indiceDoGrupo++)
        {
            ValidarGrupo(indiceDoGrupo, grupos[indiceDoGrupo], falhas);
        }
    }

    private static void ValidarGrupo(int indiceDoGrupo, OpcoesDoGrupo grupo, List<string> falhas)
    {
        if (string.IsNullOrWhiteSpace(grupo.Id))
        {
            falhas.Add(Formatar(IdDoGrupoEmBranco, indiceDoGrupo));
        }
        else if (TemEspacosNasBordas(grupo.Id))
        {
            falhas.Add(Formatar(IdDoGrupoComEspacos, indiceDoGrupo));
        }

        ValidarPresidencia(indiceDoGrupo, grupo.Presidencia, falhas);
        ValidarFrasesDoRodape(indiceDoGrupo, grupo.FrasesDoRodape, falhas);
        ValidarMensagensDeCobranca(indiceDoGrupo, grupo.MensagensDeCobranca, falhas);
        ValidarModelosDaLegenda(indiceDoGrupo, grupo.ModelosDaLegenda, falhas);
        ValidarApelidosAvulsos(indiceDoGrupo, grupo.ApelidosAvulsos, falhas);

        if (string.IsNullOrWhiteSpace(grupo.CaminhoDoEscudo) is false && File.Exists(grupo.CaminhoDoEscudo) is false)
        {
            falhas.Add(Formatar(EscudoInexistente, indiceDoGrupo));
        }
    }

    private static void ValidarPresidencia(
        int indiceDoGrupo, IReadOnlyList<OpcoesDoMembroDaPresidencia> presidencia, List<string> falhas)
    {
        if (presidencia.Count is < MinimoDeMembrosDaPresidencia or > MaximoDeMembrosDaPresidencia)
        {
            falhas.Add(Formatar(TamanhoDaPresidenciaErrado, indiceDoGrupo, presidencia.Count));
        }

        for (int indiceDoMembro = 0; indiceDoMembro < presidencia.Count; indiceDoMembro++)
        {
            ValidarIdsDoMembro(indiceDoGrupo, indiceDoMembro, presidencia[indiceDoMembro].Ids, falhas);
        }
    }

    private static void ValidarIdsDoMembro(
        int indiceDoGrupo, int indiceDoMembro, IReadOnlyList<string> ids, List<string> falhas)
    {
        if (ids.Count == 0)
        {
            falhas.Add(Formatar(MembroSemIds, indiceDoGrupo, indiceDoMembro));
        }

        for (int indiceDoId = 0; indiceDoId < ids.Count; indiceDoId++)
        {
            if (string.IsNullOrWhiteSpace(ids[indiceDoId]))
            {
                falhas.Add(Formatar(IdDoMembroEmBranco, indiceDoGrupo, indiceDoMembro, indiceDoId));
            }
            else if (TemEspacosNasBordas(ids[indiceDoId]))
            {
                falhas.Add(Formatar(IdDoMembroComEspacos, indiceDoGrupo, indiceDoMembro, indiceDoId));
            }
        }
    }

    private static void ValidarFrasesDoRodape(int indiceDoGrupo, IReadOnlyList<string> frases, List<string> falhas)
    {
        if (frases.Count == 0)
        {
            falhas.Add(Formatar(SemFrasesDeRodape, indiceDoGrupo));
        }

        for (int indiceDaFrase = 0; indiceDaFrase < frases.Count; indiceDaFrase++)
        {
            if (string.IsNullOrWhiteSpace(frases[indiceDaFrase]))
            {
                falhas.Add(Formatar(FraseDeRodapeEmBranco, indiceDoGrupo, indiceDaFrase));
            }
        }
    }

    private static void ValidarApelidosAvulsos(int indiceDoGrupo, OpcoesDosApelidosAvulsos apelidos, List<string> falhas)
    {
        (string Cor, IReadOnlyList<string> Apelidos)[] cores =
        [
            (nameof(apelidos.Laranja), apelidos.Laranja),
            (nameof(apelidos.Verde), apelidos.Verde),
            (nameof(apelidos.Cinza), apelidos.Cinza),
            (nameof(apelidos.Azul), apelidos.Azul),
            (nameof(apelidos.SemColete), apelidos.SemColete),
        ];

        foreach ((string cor, IReadOnlyList<string> daCor) in cores)
        {
            for (int indice = 0; indice < daCor.Count; indice++)
            {
                if (string.IsNullOrWhiteSpace(daCor[indice]))
                {
                    falhas.Add(Formatar(ApelidoAvulsoEmBranco, indiceDoGrupo, cor, indice));
                }
            }
        }
    }

    private static void ValidarModelosDaLegenda(int indiceDoGrupo, IReadOnlyList<OpcoesDoModeloDaLegenda> modelos, List<string> falhas)
    {
        if (modelos.Count == 0)
        {
            falhas.Add(Formatar(SemModelosDaLegenda, indiceDoGrupo));
        }

        for (int indiceDoModelo = 0; indiceDoModelo < modelos.Count; indiceDoModelo++)
        {
            OpcoesDoModeloDaLegenda modelo = modelos[indiceDoModelo];
            (string Nome, string Valor)[] campos =
            [
                (nameof(modelo.Cabecalho), modelo.Cabecalho),
                (nameof(modelo.LinhaDoHorario), modelo.LinhaDoHorario),
                (nameof(modelo.ApelidoLaranja), modelo.ApelidoLaranja),
                (nameof(modelo.ApelidoVerde), modelo.ApelidoVerde),
                (nameof(modelo.ApelidoCinza), modelo.ApelidoCinza),
                (nameof(modelo.ApelidoAzul), modelo.ApelidoAzul),
                (nameof(modelo.ApelidoSemColete), modelo.ApelidoSemColete),
            ];

            foreach ((string nome, string valor) in campos.Where(campo => string.IsNullOrWhiteSpace(campo.Valor)))
            {
                falhas.Add(Formatar(CampoDoModeloEmBranco, indiceDoGrupo, indiceDoModelo, nome));
            }

            if (string.IsNullOrWhiteSpace(modelo.LinhaDoHorario) is false
                && modelo.LinhaDoHorario.Contains("{hora}", StringComparison.Ordinal) is false)
            {
                falhas.Add(Formatar(LinhaDoHorarioSemHora, indiceDoGrupo, indiceDoModelo));
            }
        }
    }

    private static void ValidarMensagensDeCobranca(int indiceDoGrupo, IReadOnlyList<string> mensagens, List<string> falhas)
    {
        if (mensagens.Count == 0)
        {
            falhas.Add(Formatar(SemMensagensDeCobranca, indiceDoGrupo));
        }

        for (int indiceDaMensagem = 0; indiceDaMensagem < mensagens.Count; indiceDaMensagem++)
        {
            if (string.IsNullOrWhiteSpace(mensagens[indiceDaMensagem]))
            {
                falhas.Add(Formatar(MensagemDeCobrancaEmBranco, indiceDoGrupo, indiceDaMensagem));
                continue;
            }

            foreach (string marca in MarcasDaMensagem(mensagens[indiceDaMensagem]).Except(ModeloDeCobranca.Marcas))
            {
                falhas.Add(Formatar(MarcaDesconhecidaNaCobranca, indiceDoGrupo, indiceDaMensagem, marca));
            }
        }
    }

    /// <summary>Tudo entre chaves na mensagem, como "{nome}", na ordem e sem repetir.</summary>
    private static IEnumerable<string> MarcasDaMensagem(string mensagem) =>
        _marca.Matches(mensagem).Select(marca => marca.Value).Distinct(StringComparer.Ordinal);

    private static void ValidarLimites(OpcoesDeLimites limites, List<string> falhas)
    {
        ValidarDuracao(
            nameof(OpcoesDeLimites.JanelaDeRespostaNaoPresidente),
            limites.JanelaDeRespostaNaoPresidente,
            _janelaDeRespostaMaxima,
            falhas);
        ValidarDuracao(
            nameof(OpcoesDeLimites.JanelaDeCobranca),
            limites.JanelaDeCobranca,
            _janelaDeRespostaMaxima,
            falhas);
        ValidarQuantidade(
            nameof(OpcoesDeLimites.MaximoDeMensagensDoDisjuntor),
            limites.MaximoDeMensagensDoDisjuntor,
            MaximoDeMensagensDoDisjuntor,
            falhas);
        ValidarDuracao(nameof(OpcoesDeLimites.JanelaDoDisjuntor), limites.JanelaDoDisjuntor, _janelaDoDisjuntorMaxima, falhas);
        ValidarDuracao(nameof(OpcoesDeLimites.IdadeMaximaDoComando), limites.IdadeMaximaDoComando, _idadeMaximaDoComando, falhas);
        ValidarDuracao(nameof(OpcoesDeLimites.IdadeMaximaDaLista), limites.IdadeMaximaDaLista, _idadeMaximaDaLista, falhas);
    }

    private static void ValidarDuracao(string limite, TimeSpan valor, TimeSpan maximo, List<string> falhas)
    {
        if (valor <= TimeSpan.Zero)
        {
            falhas.Add(Formatar(LimiteNaoPositivo, limite, valor));
        }
        else if (valor > maximo)
        {
            falhas.Add(Formatar(DuracaoMuitoLonga, limite, maximo, valor));
        }
    }

    private static void ValidarQuantidade(string limite, int valor, int maximo, List<string> falhas)
    {
        if (valor <= 0)
        {
            falhas.Add(Formatar(LimiteNaoPositivo, limite, valor));
        }
        else if (valor > maximo)
        {
            falhas.Add(Formatar(QuantidadeMuitoGrande, limite, maximo, valor));
        }
    }

    /// <summary>Um id do WhatsApp nunca tem espaços, e um com espaços nunca combinaria com o remetente de uma mensagem.</summary>
    private static bool TemEspacosNasBordas(string valor) =>
        char.IsWhiteSpace(valor[0]) || char.IsWhiteSpace(valor[^1]);

    private static string Formatar(string mensagem, params object[] argumentos) =>
        string.Format(CultureInfo.InvariantCulture, mensagem, argumentos);
}
