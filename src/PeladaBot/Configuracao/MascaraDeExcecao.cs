using System.Text;

namespace PeladaBot.Configuracao;

/// <summary>Descreve uma exceção para um log sem sua mensagem, que pode carregar dados pessoais.</summary>
public static class MascaraDeExcecao
{
    public static string Aplicar(Exception excecao)
    {
        ArgumentNullException.ThrowIfNull(excecao);

        StringBuilder descricao = new();

        for (Exception? atual = excecao; atual is not null; atual = atual.InnerException)
        {
            if (atual != excecao)
            {
                descricao.Append(" ---> ");
            }

            descricao.Append(atual.GetType().FullName);

            if (atual.StackTrace is not null)
            {
                descricao.Append('\n').Append(atual.StackTrace);
            }
        }

        return descricao.ToString();
    }
}
