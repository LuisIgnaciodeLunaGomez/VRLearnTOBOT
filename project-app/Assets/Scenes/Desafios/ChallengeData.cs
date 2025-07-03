/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 19/06/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Carga la información de un desafío específico
 */

using UnityEngine;

[CreateAssetMenu(fileName = "Nuevo Desafio", menuName = "RoboCode/Desafio")]
public class ChallengeData : ScriptableObject
{
    [Header("Información del Desafío")]
    public string challengeTitle;

    [Tooltip("Un resumen corto para mostrar en la tarjeta del menú de selección.")]
    [TextArea(2, 4)]
    public string shortDescription;


    [Tooltip("Instrucciones detalladas y la tarea a realizar en la escena del desafío.")]
    [TextArea(4, 10)]
    public string detailedInstructions;

}

