/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 12/06/2025
 * 
 * Versión: 2.0.0 (ampliando detalles)
 * 
 * Descripción: Analizador de prefabs para depuración en Unity. Extrae y muestra
 * las propiedades clave de los componentes de UI y Layout.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR // Asegura que este script solo se compile en el editor de Unity
using UnityEditor;
/// <summary>
/// Una herramienta de depuración para analizar la estructura y componentes de los Prefabs en tiempo de edición.
/// </summary>
public class PrefabDebugger : MonoBehaviour
{
    [Header("1. Configurar Prefabs")]
    [Tooltip("Añade los prefabs directamente desde tu proyecto o busca sus rutas en una carpeta 'Resources'.")]
    public List<GameObject> prefabsToAnalyze = new List<GameObject>();

    [Header("2. Configurar Salida")]
    [Tooltip("Nombre de la carpeta en 'Assets' para guardar los informes.")]
    public string outputFolderName = "PrefabAnalysisLogs";

    [Header("3. Ejecutar Análisis")]
    [Tooltip("Haz clic derecho en este componente en el Inspector y elige 'Analizar Prefabs...'.")]
    public bool _runFromContextMenu = true;
    /// <summary>
    /// Menú contextual para iniciar el análisis.
    /// </summary>
    [ContextMenu("Analizar Prefabs (Reporte Completo) y Guardar")]
    public void AnalyzePrefabsAndSaveToFile()
    {
        Debug.Log("==================================================\n<color=yellow><b>INICIANDO ANÁLISIS COMPLETO DE PREFABS</b></color>\n==================================================");

        StringBuilder fileReportBuilder = new StringBuilder();

        fileReportBuilder.AppendLine("=======================================");
        fileReportBuilder.AppendLine("    INFORME DE ANÁLISIS DE PREFABS (V2 - CON DETALLES)");
        fileReportBuilder.AppendLine($"    Generado el: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
        fileReportBuilder.AppendLine("=======================================\n");

        if (prefabsToAnalyze == null || prefabsToAnalyze.Count == 0)
        {
            string warningMsg = "La lista 'prefabsToAnalyze' está vacía. Arrastra los prefabs que quieres analizar al Inspector.";
            Debug.LogWarning(warningMsg);
            fileReportBuilder.AppendLine(warningMsg);
        }
        else
        {
            foreach (var prefab in prefabsToAnalyze)
            {
                if (prefab == null) continue;
                ProcessPrefab(prefab, fileReportBuilder);
            }
        }

        fileReportBuilder.AppendLine("\n=======================================");
        fileReportBuilder.AppendLine("        FIN DEL INFORME");
        fileReportBuilder.AppendLine("=======================================");

        SaveReportToFile(fileReportBuilder.ToString());

        Debug.Log("==================================================\n<color=lime><b>ANÁLISIS DE PREFABS COMPLETADO</b></color>\n==================================================");
    }

    private void ProcessPrefab(GameObject prefab, StringBuilder fileReportBuilder)
    {
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        string headerFile = $"--- Analizando Prefab: '{prefab.name}' (Ruta: {assetPath}) ---";

        fileReportBuilder.AppendLine(headerFile);
        Debug.Log($"<color=cyan><b>{headerFile}</b></color>", prefab);

        LogAndBuildHierarchyRecursive(prefab.transform, 0, fileReportBuilder);

        fileReportBuilder.AppendLine("-----------------------------------------------------\n");
    }
    
    private void LogAndBuildHierarchyRecursive(Transform currentTransform, int indentationLevel, StringBuilder fileReportBuilder)
    {
        string indent = new string(' ', indentationLevel * 4);
        string fileHeader = $"{indent}{currentTransform.gameObject.name} {(currentTransform.gameObject.activeSelf ? "" : "(Inactivo)")}";
        fileReportBuilder.AppendLine(fileHeader);
        
        // --- Versión de Consola ---
        string consoleHeader = $"{indent}<b>{currentTransform.gameObject.name}</b> {(currentTransform.gameObject.activeSelf ? "" : "<color=grey>(Inactivo)</color>")}";
        StringBuilder consoleMessageBuilder = new StringBuilder();
        consoleMessageBuilder.AppendLine(consoleHeader);
        
        Component[] components = currentTransform.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component is Transform) continue;

            string componentName = component.GetType().Name;
            string componentDetails = GetComponentDetails(component, indent + "    ");

            // --- Fichero ---
            fileReportBuilder.AppendLine($"{indent}    - {componentName}");
            if (!string.IsNullOrEmpty(componentDetails))
            {
                fileReportBuilder.Append(componentDetails);
            }

            // --- Consola ---
            consoleMessageBuilder.Append(GetStyledConsoleLine(component, componentName, indent + "    "));
            if (!string.IsNullOrEmpty(componentDetails))
            {
                consoleMessageBuilder.Append($"<color=#90A4AE>{componentDetails}</color>"); // Gris azulado para detalles
            }
            consoleMessageBuilder.AppendLine();

        }

        Debug.Log(consoleMessageBuilder.ToString(), currentTransform.gameObject);

        // --- Recursión ---
        foreach (Transform child in currentTransform)
        {
            LogAndBuildHierarchyRecursive(child, indentationLevel + 1, fileReportBuilder);
        }
    }

    /// <summary>
    /// Devuelve las propiedades clave de los componentes relevantes en formato string.
    /// </summary>
    private string GetComponentDetails(Component c, string indent)
    {
        StringBuilder details = new StringBuilder();

        switch (c)
        {
            case HorizontalLayoutGroup hlg:
                details.AppendLine($"{indent}  - Padding: (L:{hlg.padding.left}, R:{hlg.padding.right}, T:{hlg.padding.top}, B:{hlg.padding.bottom})");
                details.AppendLine($"{indent}  - Spacing: {hlg.spacing}");
                details.AppendLine($"{indent}  - Alignment: {hlg.childAlignment}");
                details.AppendLine($"{indent}  - Control Size: (W:{(hlg.childControlWidth ? "✓" : "x")}, H:{(hlg.childControlHeight ? "✓" : "x")})");
                details.Append($"{indent}  - Force Expand: (W:{(hlg.childForceExpandWidth ? "✓" : "x")}, H:{(hlg.childForceExpandHeight ? "✓" : "x")})");
                break;

            case VerticalLayoutGroup vlg:
                details.AppendLine($"{indent}  - Padding: (L:{vlg.padding.left}, R:{vlg.padding.right}, T:{vlg.padding.top}, B:{vlg.padding.bottom})");
                details.AppendLine($"{indent}  - Spacing: {vlg.spacing}");
                details.AppendLine($"{indent}  - Alignment: {vlg.childAlignment}");
                details.AppendLine($"{indent}  - Control Size: (W:{(vlg.childControlWidth ? "✓" : "x")}, H:{(vlg.childControlHeight ? "✓" : "x")})");
                details.Append($"{indent}  - Force Expand: (W:{(vlg.childForceExpandWidth ? "✓" : "x")}, H:{(vlg.childForceExpandHeight ? "✓" : "x")})");
                break;
                
            case ContentSizeFitter csf:
                details.Append($"{indent}  - H-Fit: <b>{csf.horizontalFit}</b>, V-Fit: <b>{csf.verticalFit}</b>");
                break;

            case LayoutElement le:
                if (le.ignoreLayout) details.Append($"{indent}  - <color=red><b>Ignore Layout: ✓</b></color>\n");
                if (le.minWidth >= 0) details.Append($"{indent}  - Min Width: {le.minWidth}\n");
                if (le.minHeight >= 0) details.Append($"{indent}  - Min Height: {le.minHeight}\n");
                if (le.preferredWidth >= 0) details.Append($"{indent}  - Preferred Width: {le.preferredWidth}\n");
                if (le.preferredHeight >= 0) details.Append($"{indent}  - Preferred Height: {le.preferredHeight}");
                // Elimina el último salto de línea si existe
                return details.ToString().TrimEnd('\n', '\r');

            case Image img:
                // Propiedad clave para fondos expandibles
                details.Append($"{indent}  - Image Type: {(img.type == Image.Type.Sliced ? $"<b><color=#4CAF50>{img.type}</color></b>" : $"<color=orange>{img.type}</color>")} | Raycast: {(img.raycastTarget ? "✓" : "x")}");
                break;
                
            case TextMeshProUGUI tmp:
                details.Append($"{indent}  - Text: \"<i>{tmp.text.Replace("\n", " ")}</i>\" | Font Size: {tmp.fontSize} | Raycast: {(tmp.raycastTarget ? "✓" : "x")}");
                break;
        }

        return details.ToString();
    }
    
    /// <summary>
    /// Genera la línea de consola con el formato de color apropiado para el componente.
    /// </summary>
    private string GetStyledConsoleLine(Component c, string componentName, string indent)
    {
        string color = "#BDBDBD"; // Gris por defecto
        bool isBold = false;
        
        switch (c)
        {
            case LayoutGroup _: color = "#4CAF50"; isBold = true; break; // Verde
            case LayoutElement _: color = "#FFC107"; isBold = true; break; // Ámbar
            case ContentSizeFitter _: color = "#2196F3"; isBold = true; break; // Azul
            case Image _: case RawImage _: case TextMeshProUGUI _: color = "#9C27B0"; break; // Púrpura
            case ConnectionView _: case BlockView _: case InputView _: case LineGroupView _: color = "#F44336"; isBold = true; break; // Rojo (Scripts propios)
        }
        
        string line = $"{indent}- <color={color}>{(isBold ? $"<b>{componentName}</b>" : componentName)}</color>";
        return line;
    }

    private void SaveReportToFile(string reportContent)
    {
        try
        {
            string logDirectory = Path.Combine(Application.dataPath, outputFolderName);
            Directory.CreateDirectory(logDirectory);
            string fileName = $"Prefab_Analysis_V2_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
            string filePath = Path.Combine(logDirectory, fileName);
            File.WriteAllText(filePath, reportContent, Encoding.UTF8);
            Debug.Log($"<color=lime><b>✔ ANÁLISIS GUARDADO CORRECTAMENTE EN:</b></color>\nAssets/{outputFolderName}/{fileName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red><b>Error al guardar el fichero de análisis:</b></color> {e.Message}");
        }
    }
}

#endif
