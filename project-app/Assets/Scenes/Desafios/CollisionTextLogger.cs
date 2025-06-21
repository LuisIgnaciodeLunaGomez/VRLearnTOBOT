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
 * Descripción: Sirve para depurar que detecta colisiones y mostrar un mensaje en la consola de Unity
 */
using UnityEngine;

public class CollisionTextLogger : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        
        // Imprime el nombre de este objeto y el nombre del objeto con el que chocó.
        Debug.Log($"¡COLISIÓN DETECTADA! '{this.gameObject.name}' chocó con '{collision.gameObject.name}'");
    }
}
