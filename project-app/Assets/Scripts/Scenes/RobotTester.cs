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
 * Descripción: 
 */using UnityEngine;

public class RobotTester : MonoBehaviour
{
    // Referencia a nuestro robot
    public RobotBehaviour robotToTest;

    void Update()
    {
        // Al pulsar la tecla "M"
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (robotToTest != null)
            {
                Debug.Log("<color=red><b>TESTER:</b></color> Pulsada la tecla M. Ordenando al robot moverse 10 pasos.");
               
                StartCoroutine(robotToTest.MoveCoroutine(100));
            }
            else
            {
                Debug.LogError("<color=red><b>TESTER:</b></color> No se ha asignado un RobotBehaviour al script de prueba.");
            }
        }

        // Al pulsar la tecla "S"
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (robotToTest != null)
            {
                Debug.Log("<color=red><b>TESTER:</b></color> Pulsada la tecla S. Deteniendo al robot.");
                robotToTest.StopAllActions();
            }
        }
    }
}
