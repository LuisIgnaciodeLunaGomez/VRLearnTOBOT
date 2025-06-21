
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
 * Descripción: Controlador del robot que ejecuta instrucciones de movimiento y rotación.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RobotController : MonoBehaviour
{
    public float moveSpeed = 1.0f; // Unidades de Unity por segundo
    public float turnSpeed = 90.0f; // Grados por segundo

    private bool isExecuting = false;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private bool collisionDetected = false;
    private bool forceStop = false;
    void Awake()
    {
        // Guardar el estado inicial para poder resetear
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public void ResetToInitialState()
    {
        StopAllCoroutines(); // Detener cualquier movimiento en curso
        isExecuting = false;
        transform.position = startPosition;
        transform.rotation = startRotation;
        collisionDetected = false;
        forceStop = false;
    }

    public IEnumerator ExecuteProgram(List<Instruction> program)
    {
        // if (isExecuting) return;
        // StartCoroutine(ExecutionRoutine(program));
        if (isExecuting)
        {
            // Si ya está ejecutando, salimos de la corrutina inmediatamente
            yield break;
        }
        // En lugar de iniciarla aquí, le pasamos la responsabilidad al que llama
        yield return StartCoroutine(ProcessInstructionList(program));
    }

    private IEnumerator ProcessInstructionList(List<Instruction> program)
    {
        isExecuting = true;
        foreach (var instruction in program)
        {
            if (forceStop)
            {
                Debug.Log("Programa interrumpido por 'forceStop'.");
                break; // Sale del bucle foreach
            }


            switch (instruction.Type)
            {
                case CommandType.MoveForward:
                    yield return StartCoroutine(MoveForwardRoutine(instruction.Value));
                    break;
                case CommandType.TurnLeft:
                    yield return StartCoroutine(TurnRoutine(-90));
                    break;
                case CommandType.TurnRight:
                    yield return StartCoroutine(TurnRoutine(90));
                    break;
                case CommandType.Repeat:
                    int iterations = (int)instruction.Value;
                    if (iterations == -1) // Bucle "por_siempre"
                    {
                        while (true)
                        {
                            // Llamada recursiva: procesa las instrucciones DENTRO del bucle
                            yield return StartCoroutine(ProcessInstructionList(instruction.NestedInstructions));
                        }
                    }
                    else // Bucle finito
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            // Llamada recursiva
                            yield return StartCoroutine(ProcessInstructionList(instruction.NestedInstructions));
                        }
                    }
                    break;
            }
        }
        isExecuting = false;
        // Notificar al GameManager que hemos terminado
        GameManager.Instance.OnExecutionFinished();
    }

    private IEnumerator MoveForwardRoutine(float steps)
    {
        // 1. Calcular las posiciones de inicio y fin ANTES de movernos.
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + transform.forward * steps;

        // 2. Calcular cuánto tiempo debería durar el movimiento basado en la velocidad.
        float duration = steps / moveSpeed;
        float elapsedTime = 0f;

        // 3. Moverse durante el tiempo calculado hasta llegar al objetivo.
        while (elapsedTime < duration)
        {
            // ANTES de movernos, comprobamos si hemos chocado
            if (forceStop)
            {
                Debug.Log("Movimiento detenido a mitad de camino por 'forceStop'.");
                yield break; // ¡SALE DE LA CORRUTINA INMEDIATAMENTE!
            }

            // S usa Lerp para movernos suavemente desde el inicio hasta el fin.
            // elapsedTime / duration nos da un valor que va de 0 a 1.
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);

            // Avanzamos el cronómetro.
            elapsedTime += Time.deltaTime;
            yield return null; // Espera al siguiente frame.
        }

        // 4. Asegurarse de que el robot termina EXACTAMENTE en la posición final.
        if (!forceStop)
        {
            transform.position = targetPosition;
        }
    }

    private IEnumerator TurnRoutine(float angle)
    {
        Quaternion fromRotation = transform.rotation;
        Quaternion toRotation = transform.rotation * Quaternion.Euler(0, angle, 0);
        float time = 0;
        float duration = Mathf.Abs(angle) / turnSpeed;

        while (time < duration)
        {
            transform.rotation = Quaternion.Slerp(fromRotation, toRotation, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        transform.rotation = toRotation; // Asegurar rotación exacta
    }

    void OnDisable()
    {
        // Detener todo si el robot se desactiva, para evitar bucles infinitos fantasma.
        StopAllCoroutines();
    }

   /* void OnCollisionEnter(Collision collision)
    {
        Debug.Log("OnCollisionEnter se ha disparado con " + collision.gameObject.name);
        // Este evento se llama cuando nuestro collider (robot) toca otro (muro)
        if (collision.gameObject.CompareTag("Muro"))
        {
            Debug.Log("¡Colisión con un muro detectada!");
            collisionDetected = true;
        }
    }*/

    public void StopDueToCollision()
    {
        Debug.Log("ROBOT: He recibido la orden de parar por colisión.");

        if (!forceStop) // Solo imprimir y actuar la primera vez
        {
            Debug.Log("ROBOT: ¡Interruptor de emergencia activado por colisión!");
            forceStop = true;
        }
    }
}
