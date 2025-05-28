/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 28/05/2025
 * 
 * Versión: 1.0.
 * 
 * Descripción: 
 */

using UnityEngine;
using System.Collections; // Necesario para corrutinas

public class RobotBehaviour : MonoBehaviour
{
    public float moveSpeed = 5f; // Velocidad del robot

    private Coroutine m_currentMovementCoroutine;

    /// <summary>
    /// Mueve el robot hacia adelante una distancia especificada.
    /// </summary>
    /// <param name="distance">Distancia a moverse.</param>
    public void MoveForward(float distance)
    {
        // Detener cualquier movimiento anterior para evitar conflictos
        StopAllActions();

        m_currentMovementCoroutine = StartCoroutine(MoveOverTime(distance));
        Debug.Log($"RobotBehaviour: Iniciando movimiento de {distance} unidades hacia adelante.");
    }

    // Corrutina para suavizar el movimiento a lo largo del tiempo
    private IEnumerator MoveOverTime(float distance)
    {
        Vector3 startPosition = transform.position;
        // transform.forward apunta en la dirección Z del robot
        Vector3 targetPosition = transform.position + transform.forward * distance;

        float journeyLength = Vector3.Distance(startPosition, targetPosition);
        if (journeyLength < 0.001f) // Evita divisiones por cero o movimientos insignificantes
        {
            Debug.LogWarning("RobotBehaviour: Distancia de movimiento insignificante, abortando.");
            yield break;
        }

        float startTime = Time.time;
        // Calcula la duración basada en la velocidad (Tiempo = Distancia / Velocidad)
        float duration = journeyLength / moveSpeed;

        while (Vector3.Distance(transform.position, targetPosition) > 0.01f) //  pequeño umbral para la finalización
        {
            float fracJourney = (Time.time - startTime) / duration;
            // Interpolar linealmente la posición del robot
            transform.position = Vector3.Lerp(startPosition, targetPosition, fracJourney);
            yield return null; // Espera al siguiente frame
        }
        // Asegurarse de que el robot llega exactamente al destino
        transform.position = targetPosition;
        m_currentMovementCoroutine = null;
        Debug.Log("RobotBehaviour: Movimiento completado.");
    }

    /// <summary>
    /// Detiene cualquier acción en curso del robot.
    /// </summary>
    public void StopAllActions()
    {
        if (m_currentMovementCoroutine != null)
        {
            StopCoroutine(m_currentMovementCoroutine);
            m_currentMovementCoroutine = null;
            Debug.Log("RobotBehaviour: Acciones detenidas.");
        }
    }
}