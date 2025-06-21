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


using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Tooltip("La lista de todas las cámaras de simulación disponibles.")]
    public List<Camera> simulationCameras;

    private int activeCameraIndex = 0;

    void Start()
    {
        // Al empezar, nos aseguramos de que solo la primera cámara está activa.
        if (simulationCameras != null && simulationCameras.Count > 0)
        {
            SwitchToCameraByIndex(0);
        }
    }

    // Este método público lo llamarán los botones de la UI
    public void SwitchToNextCamera()
    {
        if (simulationCameras == null || simulationCameras.Count == 0) return;

        activeCameraIndex = (activeCameraIndex + 1) % simulationCameras.Count;
        SwitchToCameraByIndex(activeCameraIndex);
    }

    // Este método público permite cambiar a una cámara específica por su índice
    public void SwitchToCameraByIndex(int index)
    {
        if (simulationCameras == null || index < 0 || index >= simulationCameras.Count)
        {
            Debug.LogWarning("Índice de cámara no válido o la lista de cámaras está vacía.");
            return;
        }

        // Desactivamos el GameObject de TODAS las cámaras en la lista.
        for (int i = 0; i < simulationCameras.Count; i++)
        {
            if (simulationCameras[i] != null)
            {
                simulationCameras[i].gameObject.SetActive(false);
            }
        }

        // Activamos SOLO el GameObject de la cámara seleccionada.
        activeCameraIndex = index;
        if (simulationCameras[activeCameraIndex] != null)
        {
            simulationCameras[activeCameraIndex].gameObject.SetActive(true);
            Debug.Log($"Cámara activa: {simulationCameras[activeCameraIndex].name}");
        }
    }

    /*
    private void SwitchToCamera(int index)
    {
        // Desactivamos todas las cámaras de la lista
        for (int i = 0; i < simulationCameras.Count; i++)
        {
            if (simulationCameras[i] != null)
            {
                simulationCameras[i].gameObject.SetActive(false);
                simulationCameras[i].targetTexture = null; // Desasignar la textura
            }
        }

        // Activamos la cámara seleccionada
        if (simulationCameras[index] != null)
        {
            activeCameraIndex = index;
            Camera activeCamera = simulationCameras[index];

            // Le asignamos la Render Texture
           // activeCamera.targetTexture = targetRenderTexture;

            // Y la activamos
            activeCamera.gameObject.SetActive(true);

            Debug.Log($"Cambiando a cámara: {activeCamera.name}");
        }
    }*/
}
