/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 22/06/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 *//// <summary>
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