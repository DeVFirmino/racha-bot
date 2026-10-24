namespace PeladaBot.Configuracao;

/// <summary>Mascara um id do WhatsApp antes de um log: mantém só os últimos quatro caracteres e o sufixo "@".</summary>
public static class MascaraDeId
{
    private const string Oculto = "***";
    private const int CaracteresVisiveis = 4;
    private const char SeparadorDoIdDaMensagem = '_';

    public static string Aplicar(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        int inicioDoSufixo = id.IndexOf('@', StringComparison.Ordinal);
        string usuario = inicioDoSufixo < 0 ? id : id[..inicioDoSufixo];
        string sufixo = inicioDoSufixo < 0 ? string.Empty : id[inicioDoSufixo..];

        return UltimosCaracteres(usuario) + sufixo;
    }

    /// <summary>Mascara o id completo de uma mensagem do WAHA para casar com sua forma curta de citação, ligando os logs.</summary>
    public static string AplicarNaMensagem(string idDaMensagem)
    {
        ArgumentNullException.ThrowIfNull(idDaMensagem);

        string[] partes = idDaMensagem.Split(SeparadorDoIdDaMensagem);
        string chave = partes.Length is 3 or 4 ? partes[2] : idDaMensagem;

        return chave.Contains('@', StringComparison.Ordinal) ? Oculto : UltimosCaracteres(chave);
    }

    private static string UltimosCaracteres(string valor) =>
        valor.Length <= CaracteresVisiveis ? Oculto : Oculto + valor[^CaracteresVisiveis..];
}
