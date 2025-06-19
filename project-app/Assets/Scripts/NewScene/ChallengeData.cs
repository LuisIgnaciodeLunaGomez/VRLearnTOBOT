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
 * Descripción: 
 */

using UnityEngine;


[CreateAssetMenu(fileName = "Nuevo Desafio", menuName = "RoboCode/Desafio")]
public class ChallengeData : ScriptableObject
{
    [Header("Información del Desafío")]
    public string challengeTitle;

    [TextArea(4, 8)] // Esto hace que el campo de texto sea más grande en el Inspector
    public string challengeDescription;

    // TODO  se podrían  añadir más cosas como condiciones de victoria, tiempo límite, etc.
}

