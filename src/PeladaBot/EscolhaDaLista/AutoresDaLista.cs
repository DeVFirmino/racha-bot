namespace PeladaBot.EscolhaDaLista;

/// <summary>De quem são as listas que um comando usa quando não cita nenhuma.</summary>
public enum AutoresDaLista
{
    /// <summary>!sortear: só as listas do presidente e dos admins, já que o ✅ que conta é o deles.</summary>
    Presidencia,

    /// <summary>!pendentes: a lista de qualquer um, pois é ali que aparece quem chegou depois e ainda não pagou.</summary>
    QualquerUm,
}
