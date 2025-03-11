
/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 08/03/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Esta clase valida que el sprite generado sea correcto y que los bloques tengan  su componente blockView.
 */

using UnityEditor;
using UnityEngine;

public class PrefabValidator
{
    [MenuItem("Tools/Validate Block Prefabs")]
    static void ValidatePrefabs()
    {
        string[] prefabPaths = System.IO.Directory.GetFiles("Assets/Resources/Prefabs/BlocksPrefab/", "*.prefab", System.IO.SearchOption.AllDirectories);
        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab.GetComponent<BlockView>() == null)
            {
                Debug.LogError($"Prefab en '{path}' falta el componente BlockView.");
            }
        }
        Debug.Log("Validación completada.");
    }
}