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
 * Descripción: Sistema para detectar colisiones entre el robot y los muros.
 */


using UnityEngine;

public class MuroController : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        // Comprobamos si lo que nos ha chocado es el robot. Robot etiqueta para identificar al robot.
        if (collision.gameObject.CompareTag("Robot"))
        {
            Debug.Log($"Muro '{this.name}' ha sido golpeado por el Robot.");

            // Obtiene el script RobotController del objeto que ha chocado.
            RobotController robotController = collision.gameObject.GetComponent<RobotController>();

            // Si lo encontramos, le enviamos el mensaje para que se detenga el robot.
            if (robotController != null)
            {
                robotController.StopDueToCollision();
            }
        }
    }
}
