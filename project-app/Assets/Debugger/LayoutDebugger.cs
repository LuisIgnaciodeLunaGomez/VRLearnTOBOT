/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 14/06/2025
 * 
 * Versión:1.0.0
 * 
 * Descripción: 
 * 
 */


using UnityEngine;

public class LayoutDebugger : MonoBehaviour
{
    void OnDrawGizmos()
    {
        if (BaseView.PrefabPositions == null) return;

        foreach (var entry in BaseView.PrefabPositions)
        {
            if (entry.Key == null) continue;

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.magenta;

            string label = $"{entry.Key.name}\nPrefabPos: {entry.Value.ToString("F2")}\nActualPos: {(entry.Key.transform as RectTransform).anchoredPosition.ToString("F2")}";

            UnityEditor.Handles.Label(entry.Key.transform.position, label, style);
        }
    }
}