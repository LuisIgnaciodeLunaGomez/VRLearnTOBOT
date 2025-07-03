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
 * Descripción: Script que controla el comportamiento del robot en la escena.
 */

using UnityEngine;
using System.Collections; // Necesario para corrutinas

public class RobotBehaviour : MonoBehaviour
{
    public float moveSpeed = 0.5f; // Velocidad del robot
    [Tooltip("Define cuánto se mueve el robot por cada 'paso'")]
    public float stepSize = 0.1f;

    public float turnSpeed = 90f; // Grados por segundo

    private Coroutine m_currentMovementCoroutine;

    /// <summary>
    /// Método público principal para iniciar el movimiento.
    /// Este método DETIENE cualquier movimiento anterior e INICIA uno nuevo.
    /// </summary>
    public void StartMove(float steps)
    {
        float distance = steps * stepSize;

        // Si ya hay un movimiento en curso, lo detenemos.
        if (m_currentMovementCoroutine != null)
        {
            StopCoroutine(m_currentMovementCoroutine);
        }

        // Iniciamos la nueva corrutina de movimiento.
        m_currentMovementCoroutine = StartCoroutine(DoMovement(distance));
    }

    /// <summary>
    /// Corrutina pública que UBlockly puede esperar.
    /// Inicia el movimiento y espera a que termine.
    /// </summary>
    public IEnumerator MoveCoroutine(float steps)
    {
        Debug.Log($"<color=lime><b>RobotBehaviour.MoveCoroutine:</b></color> Recibidos {steps} pasos. moveSpeed={moveSpeed}, stepSize={stepSize}");
        float distance = steps * stepSize;
        Debug.Log($"<color=lime><b>RobotBehaviour.MoveCoroutine:</b></color> Distancia a mover calculada: {distance}");

        // Si hay otro movimiento, detenlo
        if (m_currentMovementCoroutine != null)
        {
            StopCoroutine(m_currentMovementCoroutine);
        }

        // Ejecuta el movimiento y ESPERA a que termine.
        m_currentMovementCoroutine = StartCoroutine(DoMovement(distance));
        yield return m_currentMovementCoroutine;
    }

    private IEnumerator DoMovement(float distance)
    {
        Vector3 startPosition = transform.position;
       // Vector3 targetPosition = transform.position + transform.forward * distance;
        Vector3 targetPosition = transform.position + /*Vector3.right*/ transform.forward * distance;

        Debug.Log($"<color=red><b>PRUEBA DE MOVIMIENTO:</b></color> Forzando movimiento en Vector3.right. Target: {targetPosition}");

        float journeyLength = distance; // Ya hemos calculado la distancia

        if (journeyLength < 0.001f) yield break;

        float duration = journeyLength / moveSpeed;
        if (duration < 0.001f)
        {
            transform.position = targetPosition;
            yield break;
        }

        float timeElapsed = 0;
        while (timeElapsed < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null; // Espera al siguiente frame.
        }

        transform.position = targetPosition; // Asegura la posición final
        m_currentMovementCoroutine = null;   // Limpia la referencia a la corrutina.
        Debug.Log("RobotBehaviour: Movimiento completado.");
    }



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
            yield return null; 
        }
        if (this != null) transform.position = endPos;
        m_currentMovementCoroutine = null;
        Debug.Log("RobotBehaviour: Movimiento completado.");
    }


    public IEnumerator TurnCoroutine(float degrees)
    {
        
        if (m_currentMovementCoroutine != null)
        {
            StopCoroutine(m_currentMovementCoroutine);
        }

        m_currentMovementCoroutine = StartCoroutine(DoTurn(degrees));
        yield return m_currentMovementCoroutine;
    }

    private IEnumerator DoTurn(float degrees)
    {
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = transform.rotation * Quaternion.Euler(0, degrees, 0);

        float duration = Mathf.Abs(degrees) / turnSpeed;
        if (duration < 0.001f)
        {
            transform.rotation = targetRotation;
            yield break;
        }

        float timeElapsed = 0;
        while (timeElapsed < duration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = targetRotation; 
        m_currentMovementCoroutine = null;
        Debug.Log("RobotBehaviour: Giro completado.");
    }

    /// <summary>
    /// Este método es llamado automáticamente por el motor de física de Unity
    /// en el momento en que nuestro robot (que tiene un Rigidbody) toca otro Collider.
    /// </summary>
    /// <param name="collision">Información sobre la colisión, como el objeto con el que chocó.</param>
    private void OnCollisionEnter(Collision collision)
    {
        // Opcional: podemos comprobar contra qué hemos chocado si quisiéramos
        // hacer cosas diferentes. Por ejemplo, si el borde tiene un "tag" específico.
        // if (collision.gameObject.CompareTag("Wall")) { ... }

        Debug.Log($"<color=red><b>COLISIÓN DETECTADA!</b></color> Chocando con: {collision.gameObject.name}. Deteniendo movimiento actual.");

        // Llamamos a nuestro método para detener cualquier corrutina de movimiento o giro.
        StopAllActions();
    }
}