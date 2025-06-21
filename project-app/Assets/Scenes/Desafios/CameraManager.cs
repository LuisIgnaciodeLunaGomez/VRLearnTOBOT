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

    [Tooltip("La Render Texture a la que dibujará la cámara activa.")]
    public RenderTexture targetRenderTexture;

    private int activeCameraIndex = 0;

    void Start()
    {
        // Al empezar, activamos solo la primera cámara de la lista.
        if (simulationCameras.Count > 0)
        {
            SwitchToCamera(0);
        }
    }

    // Este método público lo llamarán los botones de la UI
    public void SwitchToNextCamera()
    {
        // Incrementamos el índice y usamos el operador módulo para volver al principio si llegamos al final
        activeCameraIndex = (activeCameraIndex + 1) % simulationCameras.Count;
        SwitchToCamera(activeCameraIndex);
    }

    // Este método público permite cambiar a una cámara específica por su índice
    public void SwitchToCameraByIndex(int index)
    {
        if (index >= 0 && index < simulationCameras.Count)
        {
            SwitchToCamera(index);
        }
    }

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
            activeCamera.targetTexture = targetRenderTexture;

            // Y la activamos
            activeCamera.gameObject.SetActive(true);

            Debug.Log($"Cambiando a cámara: {activeCamera.name}");
        }
    }
}
