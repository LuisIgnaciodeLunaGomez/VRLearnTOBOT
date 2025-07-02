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
 */

using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void CargarEscena(string nombreDeLaEscena)
    {
       
        //Debug.Log("Cargando escena: " + nombreDeLaEscena);

        // Carga la escena que corresponde al nombre proporcionado.
        SceneManager.LoadScene(nombreDeLaEscena);
    }
}
