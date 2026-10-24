namespace PeladaBot.Configuracao;

public sealed class OpcoesDoGrupo
{
    /// <summary>O id do grupo no WhatsApp, algo como "120363...@g.us". O bot age somente nesse grupo.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>O presidente e, opcionalmente, um admin: 1 ou 2 pessoas, reconhecidas só pelo id do WhatsApp.</summary>
    public IReadOnlyList<OpcoesDoMembroDaPresidencia> Presidencia { get; init; } = [];

    /// <summary>
    /// As frases do fim da legenda, como a resenha e o lembrete do horário, escritas com a marcação do WhatsApp
    /// (negrito com *) e, se preciso, em mais de uma linha. Todo sorteio termina com uma delas, escolhida ao acaso.
    /// </summary>
    public IReadOnlyList<string> FrasesDoRodape { get; init; } = [];

    /// <summary>Os temas da legenda do sorteio. Cada sorteio usa um deles, escolhido ao acaso.</summary>
    public IReadOnlyList<OpcoesDoModeloDaLegenda> ModelosDaLegenda { get; init; } = [];

    /// <summary>Apelidos de time fora dos temas, por cor. Cada time sorteia o seu entre estes e os dos temas.</summary>
    public OpcoesDosApelidosAvulsos ApelidosAvulsos { get; init; } = new();

    /// <summary>O valor da pelada no !ajuda, como o grupo escreve. Vazio, o !ajuda fala do pagamento sem valor.</summary>
    public string ValorDaPelada { get; init; } = string.Empty;

    /// <summary>
    /// O link de pagamento no !ajuda, como "pagamento.exemplo/grupo". É de uma pessoa: fica fora do Git, numa variável de ambiente.
    /// Vazio, o !ajuda não mostra link.
    /// </summary>
    public string LinkDePagamento { get; init; } = string.Empty;

    /// <summary>
    /// As mensagens do !cobrar, escritas com a marcação do WhatsApp e em quantas linhas precisarem. Cada cobrança
    /// usa uma delas, escolhida ao acaso.
    /// </summary>
    public IReadOnlyList<string> MensagensDeCobranca { get; init; } = [];

    /// <summary>O nome no topo do card, como "PELADA". Vazio, fica "⚽ PELADA ⚽".</summary>
    public string TituloDoCard { get; init; } = string.Empty;

    /// <summary>
    /// Caminho de uma imagem PNG ou JPEG para o escudo ao lado do título, recortada num círculo. Vazio, sem escudo.
    /// A imagem fica fora do Git: o caminho aponta para um arquivo local ou um volume.
    /// </summary>
    public string CaminhoDoEscudo { get; init; } = string.Empty;
}
