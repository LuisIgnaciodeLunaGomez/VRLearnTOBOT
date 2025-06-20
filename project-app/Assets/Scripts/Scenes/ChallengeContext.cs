/// <summary>
/// Clase estática y global para pasar información simple entre escenas,
/// como el ID del desafío seleccionado.
/// </summary>
public static class ChallengeContext
{
    /// <summary>
    /// El ID del desafío que el usuario ha seleccionado en el menú.
    /// Será null si se elige "Programación Libre".
    /// </summary>
    public static string SelectedChallengeId { get; set; } = null;
}