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
    public float moveSpeed = 0.5f; // Velocidad del robot

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
        Debug.Log($"RobotBehaviour: DEBUG - Inicia MoveRobotOverTime({distance})");
        StopAllActions(); // Asegura que se detiene cualquier movimiento anterior


        Vector3 startPosition = transform.position;
        // transform.forward apunta en la dirección Z del robot
        Vector3 targetPosition = transform.position + transform.forward * distance;

        float journeyLength = Vector3.Distance(startPosition, targetPosition);

        Debug.Log($"RobotBehaviour: DEBUG - startPosition={startPosition}, targetPosition={targetPosition}, transform.forward={transform.forward}, journeyLength={journeyLength}");
        
        if (journeyLength < 0.001f) // Evita divisiones por cero o movimientos insignificantes
        {
            Debug.LogWarning("RobotBehaviour: DEBUG - ** journeyLength INSIGNIFICANTE o CERO. Corrutina terminará YA. **");

            yield break;
        }

        float startTime = Time.time;
        // Calcula la duración basada en la velocidad (Tiempo = Distancia / Velocidad)
        float duration = journeyLength / moveSpeed;

        Debug.Log($"RobotBehaviour: DEBUG - moveSpeed={moveSpeed}, duration={duration}");

        if (duration <= 0.001f)
        {
            transform.position = targetPosition; // Si dura muy poco, se mueve al instante.
            Debug.LogWarning("RobotBehaviour: Duración del movimiento extremadamente corta, moviendo al instante.");
            yield break;
        }

        m_currentMovementCoroutine = StartCoroutine(DoMove(startPosition, targetPosition, duration));

        Debug.Log("RobotBehaviour: DEBUG - Llamando a DoMove y esperando.");

        yield return m_currentMovementCoroutine;

        Debug.Log("RobotBehaviour: DEBUG - DoMove ha finalizado.");
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
            Debug.Log("RobotBehaviour: Acciones detenidas (StopCoroutine llamado).");
        }
    }

    /// <summary>
    /// Mueve el robot hacia adelante una distancia especificada, devolviendo una corrutina para poder esperar a que finalice.
    /// </summary>
    /// <param name="distance">Distancia a moverse.</param>
    public IEnumerator MoveRobotOverTime(float distance)
    {
        StopAllActions(); // se detiene cualquier movimiento anterior

        Vector3 startPosition = transform.position;
        // transform.forward apunta en la dirección Z LOCAL del robot
        Vector3 targetPosition = transform.position + transform.forward * distance;

        float journeyLength = Vector3.Distance(startPosition, targetPosition);
        if (journeyLength < 0.001f) // Movimiento insignificante, salir
        {
            Debug.LogWarning("RobotBehaviour: Distancia de movimiento insignificante, abortando.");
            yield break;
        }

        float startTime = Time.time;
        float duration = journeyLength / moveSpeed;

        if (duration <= 0.001f)
        { // Prevenir división por cero si moveSpeed es muy alta o distance muy pequeña
            transform.position = targetPosition; // Mover al instante si es demasiado rápido/corto
            yield break;
        }

        m_currentMovementCoroutine = StartCoroutine(DoMove(startPosition, targetPosition, duration)); // La corrutina interna.
        yield return m_currentMovementCoroutine; // Esperar a que la corrutina interna termine

       
    }

    private IEnumerator DoMove(Vector3 startPos, Vector3 endPos, float duration)
    {
        Debug.Log($"RobotBehaviour: DEBUG - Inicia DoMove. De {startPos} a {endPos} en {duration}s.");

        float timeElapsed = 0;
        while (timeElapsed < duration)
        {
            if (this == null) // Evitar error si el GameObject es destruido mientras corre la corrutina.
            {
                Debug.LogWarning("RobotBehaviour: DEBUG - DoMove: GameObject destruido durante corrutina.");
                yield break;
            }

            transform.position = Vector3.Lerp(startPos, endPos, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null; // Espera al siguiente frame
        }
        // Asegurar  que el robot llega exactamente al destino
        if (this != null) transform.position = endPos;
        m_currentMovementCoroutine = null;
        Debug.Log("RobotBehaviour: Movimiento completado.");
    }

   

}