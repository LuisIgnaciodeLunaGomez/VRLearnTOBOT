
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

    [Tooltip("La distancia en unidades de Unity que representa un 'paso' en el lenguaje de comandos.")]
    public float distanciaPorPaso = 0.4f;

    private bool isExecuting = false;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Rigidbody rb;
    private bool collisionDetected = false;
    private bool forceStop = false;
    public IDE_UIManager uiManager;
    public class ExecutionStatus { public bool collided = false; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Debug.Log($"Rigidbody encontrado. Is Kinematic: {rb.isKinematic}");

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

    public IEnumerator ExecuteProgram(List<Instruction> program, ExecutionStatus status)
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

    private IEnumerator ProcessInstructionList(List<Instruction> program/*, ExecutionStatus status*/)
    {
        isExecuting = true;

        var status = new ExecutionStatus(); //Objeto de estado de ejecución
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
                    yield return StartCoroutine(MoveForwardRoutine(instruction.Value, status));
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
                case CommandType.MoveForDuration:
                    yield return StartCoroutine(MoveForDurationRoutine(instruction.Value));
                    break;
            }
        }
        isExecuting = false;
        // Notificar al GameManager que hemos terminado
        GameManager.Instance.OnExecutionFinished(false); //false significa sin colisión
    }

    private IEnumerator _MoveForwardRoutine(float steps)
    {
        // 1. Calcular las posiciones de inicio y fin ANTES de movernos.
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + transform.forward * steps / 10;

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
            Vector3 newPosition =/*transform.position*/  Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            rb.MovePosition(newPosition);
            // Avanzamos el cronómetro.
            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        // 4. Asegurarse de que el robot termina EXACTAMENTE en la posición final.
        /* if (!forceStop)
         {
             transform.position = targetPosition;
         }*/

        rb.MovePosition(targetPosition);
    }

    private IEnumerator MoveForwardRoutine(float steps, ExecutionStatus status)
    {

        //Cáculo la distancia que representa un "paso" en unidades de Unity.
        float distanciaRealEnUnidades = steps * distanciaPorPaso;


        // --- CONSULTA PROACTIVA DE COLISIÓN ---
        RaycastHit hitInfo;
        // Lanzamos un "molde" con la forma de nuestro collider hacia adelante.
        // Comprobamos hasta una distancia de 'steps'.
        bool willCollide = rb.SweepTest(transform.forward, out hitInfo, distanciaRealEnUnidades, QueryTriggerInteraction.Ignore);

        float distanceToMove = distanciaRealEnUnidades;

        // Si se predice una colisión...
        if (willCollide)
        {
            Debug.Log($"¡COLISIÓN INMINENTE con {hitInfo.collider.name} a {hitInfo.distance} unidades!");
           
            distanceToMove = hitInfo.distance + 0.01f;

            // Si la distancia es negativa (empezamos ya tocando), no nos movemos.
            if (distanceToMove < 0) distanceToMove = 0;

            collisionDetected = true;


        }

        if (distanceToMove <= 0.001f)
        {
            yield break;
        }
        //  MOVIMIENTO PRECISO 
        // Ahora ejecutamos el movimiento, pero con la distancia segura que hemos calculado.
        // Si no hubo colisión, distanceToMove sigue siendo igual a 'steps'.

        Vector3 startPosition = transform.position;
        // Usamos transform.forward para la dirección, que es el eje Z azul local.
        Vector3 targetPosition = startPosition + transform.forward * distanceToMove;
        float duration = distanceToMove / moveSpeed;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            
            rb.MovePosition(Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration));
            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Aseguramos la posición final precisa.
        rb.MovePosition(targetPosition);

        // Si hemos chocado
        if (collisionDetected)
        {
            Debug.Log("Movimiento interrumpido por colisión. El resto del programa se detendrá.");
            status.collided = true;
            yield break;
        }
    }

    private IEnumerator TurnRoutine(float angle)
    {
        Quaternion fromRotation = rb.rotation;
        Quaternion toRotation = fromRotation * Quaternion.Euler(0, angle, 0);
        float time = 0;
        float duration = Mathf.Abs(angle) / turnSpeed;

        while (time < duration)
        {
            Quaternion newRotation /*transform.rotation*/ = Quaternion.Slerp(fromRotation, toRotation, time / duration);

            rb.MoveRotation(newRotation);
            time += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }
        rb.MoveRotation(toRotation);// transform.rotation = toRotation; // Asegurar rotación exacta
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


    // Rutina para moverse durante un tiempo determinado
    private IEnumerator MoveForDurationRoutine(float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            // Nos movemos hacia adelante a velocidad constante.
            
            rb.MovePosition(transform.position + transform.forward * moveSpeed * Time.fixedDeltaTime);

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

}