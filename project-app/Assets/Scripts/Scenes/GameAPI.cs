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
 * Descripción: script que contiene la API del juego.
 */
using UnityEngine;
using System.Collections; // Necesario para IEnumerator

public static class GameAPI
{

    private static RobotBehaviour FindRobot()
    {
        RobotBehaviour robot = GameObject.FindFirstObjectByType<RobotBehaviour>();
        if (robot == null)
        {
            // Usa LogError para que sea rojo y muy visible en la consola.
            Debug.LogError("--- ¡CRÍTICO! --- GameAPI.FindRobot() NO ENCONTRÓ NINGÚN GameObject activo con el script RobotBehaviour.");
        }
        else
        {
            Debug.Log("<color=lime><b>GameAPI.FindRobot()</b></color> - Robot encontrado con éxito: " + robot.name, robot.gameObject);
        }
        return robot;
    }


    /// <summary>
    /// Inicia el movimiento del robot y devuelve la corrutina para poder esperarla.
    /// Este método ahora será llamado por el bloque "mover".
    /// </summary>
    /// <param name="steps">El número de pasos a moverse.</param>
    /// <returns>Una corrutina (IEnumerator) que representa la operación de movimiento.</returns>
    public static IEnumerator MoveRobotSteps(float steps)
    {
        RobotBehaviour robot = FindRobot();
        if (robot != null)
        {
            // Ahora llamamos al método corrutina público de nuestro robot.
            yield return robot.MoveCoroutine(steps);
        }
        else
        {
            yield return null;
        }
    }

    /// <summary>
    /// Método para detener todas las acciones del robot de inmediato.
    /// Puede ser llamado por un bloque de "parar" o por el botón de stop de la UI.
    /// </summary>
    public static void StopRobot()
    {
        RobotBehaviour robot = FindRobot();
        if (robot != null)
        {
            robot.StopAllActions();
        }
    }

    public static IEnumerator TurnRobot(float degrees)
    {
        RobotBehaviour robot = FindRobot();
        if (robot != null)
        {
            yield return robot.TurnCoroutine(degrees);
        }
        else
        {
            yield return null;
        }
    }

}


