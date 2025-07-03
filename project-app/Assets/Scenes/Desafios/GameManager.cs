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
 * Descripción: Gestor del juego que maneja la lógica de ejecución del desafío.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuración del Desafío")] 
    public ChallengeData currentChallenge;

    [Header("Referencias de la Escena")]
    public RobotController robot;
    public IDE_UIManager uiManager;

    private Coroutine executionTimerCoroutine;
    void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
    }

    void Start() 
    {
        if (ChallengeLoader.Instance != null && ChallengeLoader.Instance.SelectedChallenge != null)
        {
            // Usamos el desafío que se seleccionó en el menú.
            currentChallenge = ChallengeLoader.Instance.SelectedChallenge;

         
            uiManager.DisplayChallenge(currentChallenge);
        }
        else
        {
            Debug.LogError("No se pudo cargar el desafío. ChallengeLoader no encontrado o no hay desafío seleccionado.");
            uiManager.SetOutputText("ERROR: No se pudo cargar el desafío.", Color.red);
        }
    }
    /*
    public void ProcessAndRunCode(string sourceCode)
    {
        uiManager.SetButtonsInteractable(false); // Desactivar botones mientras se ejecuta
        robot.ResetToInitialState(); // Reiniciar el robot antes de cada ejecución esto depende de si queremos seguir o no el estado del robot entre ejecuciones

        if (CommandParser.Parse(sourceCode, out var program, out string errorMessage))
        {
            // Si el código es válido
            uiManager.SetOutputText("Código Válido. Ejecutando...", Color.cyan);
           // robot.ExecuteProgram(program);
            StartCoroutine(RunProgramAndFinish(program));

        }
        else
        {
            // Si el Parser encontró un error
            uiManager.SetOutputText(errorMessage, Color.red);
            uiManager.SetButtonsInteractable(true); // Reactivar botones
        }
    }*/

    private IEnumerator RunProgramAndFinish(List<Instruction> program)
    {
        var status = new RobotController.ExecutionStatus();
        // El robot ejecuta el programa
        //robot.ExecuteProgram(program, status);

       
        yield return robot.ExecuteProgram(program, status);

        OnExecutionFinished(status.collided);

        if (status.collided)
        {
            Debug.LogWarning("El robot ha colisionado durante la ejecución del programa.");
        }
        //  OnExecutionFinished(program);
    }
    public void OnExecutionFinished(bool causedByCollision)
    {
        // El robot  avisa que ha terminado
        // Comprobamos el resultado
        if (causedByCollision)
        {
            // Si hubo colisión, le decimos al UIManager que muestre este mensaje
            uiManager.SetOutputText("¡El robot ha colisionado!", new Color(1, 0.8f, 0)); // Un color naranja/ámbar
        }
        else
        {
            // Si no hubo colisión, le decimos que muestre el mensaje de éxito
            uiManager.SetOutputText("¡Ejecución Completada!", Color.green);
        }
        uiManager.SetButtonsInteractable(true); // Reactivar botones
        uiManager.UpdateChronometer(0);
       // uiManager.SetOutputText("Programa borrado. Listo.", Color.white);
    }

    public void ProcessAndRunProgram(List<Instruction> program)
    {
        if (executionTimerCoroutine != null) StopCoroutine(executionTimerCoroutine);
        executionTimerCoroutine = StartCoroutine(ExecutionTimer());
        uiManager.SetButtonsInteractable(false);
        robot.ResetToInitialState();

        uiManager.SetOutputText("Ejecutando programa...", Color.cyan);
        StartCoroutine(RunProgramAndFinish(program));
    }

    private IEnumerator ExecutionTimer()
    {
        float elapsedTime = 0f;
        while (true)
        {
            // Actualizamos la UI en cada frame
            uiManager.UpdateChronometer(elapsedTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}