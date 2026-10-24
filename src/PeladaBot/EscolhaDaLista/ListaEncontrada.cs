using PeladaBot.Listas;
using PeladaBot.Respostas;

namespace PeladaBot.EscolhaDaLista;

/// <summary>A lista que o comando usa, lida uma vez: o card e a legenda vêm dessa mesma versão.</summary>
public sealed record ListaEncontrada : ListaEscolhida
{
    public required ListaDoGrupo Lista { get; init; }

    /// <summary>Quem enviou a lista e quando, para a linha "📋"; null quando citada.</summary>
    public required OrigemDaLista? Origem { get; init; }
}
