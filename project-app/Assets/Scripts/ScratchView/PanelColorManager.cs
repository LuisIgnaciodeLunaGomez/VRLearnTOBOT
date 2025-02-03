/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 03/02/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Clase que permite pintar los paneles si al final se van a usar
 */

using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UI;

public class PanelColorManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Image blockZone;
    public Image workSpace;

    void Start()
    {
        if (blockZone != null)
            blockZone.color = new Color(0.2f, 0.4f, 1f, 1f); // Azul para zona de bloques

        if (workSpace != null)
            workSpace.color = new Color(1f, 0.8f, 0f, 1f); // Amarillo para espacio de trabajo
    }
}
