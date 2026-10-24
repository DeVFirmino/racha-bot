using System.Globalization;

namespace PeladaBot.Respostas;

/// <summary>
/// A hora que uma resposta mostra, no fuso configurado em <c>Bot:Fuso</c> (IANA, horário de verão incluído), seja
/// qual for o relógio do servidor. <see cref="FusoDaSuite"/> é o fuso dos testes que já gravaram o relógio: a
/// inicialização do bot usa o da configuração.
/// </summary>
internal static class HorarioDoGrupo
{
    public const string IdDoFusoDaSuite = "Europe/Rome";

    public static TimeZoneInfo FusoDaSuite { get; } = TimeZoneInfo.FindSystemTimeZoneById(IdDoFusoDaSuite);

    public static TimeZoneInfo Carregar(string id) => TimeZoneInfo.FindSystemTimeZoneById(id);

    /// <summary>"21:04".</summary>
    public static string HoraEMinuto(DateTimeOffset instante, TimeZoneInfo fuso) =>
        NoFuso(instante, fuso).ToString("HH':'mm", CultureInfo.InvariantCulture);

    /// <summary>A hora no fuso, de 0 a 23.</summary>
    public static int Hora(DateTimeOffset instante, TimeZoneInfo fuso) => NoFuso(instante, fuso).Hour;

    /// <summary>"12/10".</summary>
    public static string DiaEMes(DateTimeOffset instante, TimeZoneInfo fuso) =>
        NoFuso(instante, fuso).ToString("dd'/'MM", CultureInfo.InvariantCulture);

    private static DateTimeOffset NoFuso(DateTimeOffset instante, TimeZoneInfo fuso) =>
        TimeZoneInfo.ConvertTime(instante, fuso);
}
