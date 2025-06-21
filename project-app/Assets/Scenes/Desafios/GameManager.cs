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

    void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
    }

    void Start() 
    {
        // Cargar y mostrar el desafío actual
        if (currentChallenge != null)
        {
            uiManager.DisplayChallenge(currentChallenge);
        }
        else
        {
            // Mensaje por si se olvida asignar un desafío
            uiManager.SetOutputText("No hay ningún desafío cargado.", Color.yellow);
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
        // El robot ejecuta el programa
        robot.ExecuteProgram(program);

       
        yield return StartCoroutine(robot.ExecuteProgram(program));

        OnExecutionFinished();
    }
    public void OnExecutionFinished()
    {
        // El robot  avisa que ha terminado
        uiManager.SetOutputText("¡Ejecución Completada!", Color.green);
        uiManager.SetButtonsInteractable(true); // Reactivar botones
    }

    public void ProcessAndRunProgram(List<Instruction> program)
    {
        uiManager.SetButtonsInteractable(false);
        robot.ResetToInitialState();

        uiManager.SetOutputText("Ejecutando programa...", Color.cyan);
        StartCoroutine(RunProgramAndFinish(program));
    }
}