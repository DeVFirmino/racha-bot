namespace PeladaBot.Comandos;

/// <summary>O que uma mensagem pede ao bot para fazer, mapeado a partir da primeira palavra da mensagem.</summary>
public enum TipoDeComando
{
    /// <summary>!sortear ou !times: sorteia os times. Só o presidente e os admins.</summary>
    Sortear,

    /// <summary>!ajuda: o guia rápido, para qualquer integrante.</summary>
    Ajuda,

    /// <summary>
    /// !pendentes ou !cobrar: uma mensagem de cobrança da diretoria com quem está sem ✅. Presidente e admins a qualquer
    /// hora; os outros, uma cobrança por janela no grupo.
    /// </summary>
    Cobrar,
}
