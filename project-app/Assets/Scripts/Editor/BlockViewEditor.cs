/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 15/06/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class VRLearnPrefabBuilder
{
    // Define la ruta en el menú de Unity. Puedes poner el nombre que quieras.
    [MenuItem("VRLearnTOBOT/Construir Prefabs de Bloques desde XML")]
    public static void BuildBlockPrefabsFromXML()
    {
        Logger.Log("<color=orange>>>> INICIANDO CONSTRUCCIÓN DE PREFABS DE VRLEARNTOBOT <<<<</color>");

        //  1. Cargar todas las definiciones desde  ficheros XML 
        BlockDataLoader.LoadAllDefinitions();

        //  2. Obtener todas las definiciones cargadas 
        var allDefinitions = BlockFactory.Instance.GetAllBlockDefinitions();

        if (allDefinitions.Count == 0)
        {
            Debug.LogError("No se encontraron definiciones de bloques en BlockFactory. ¿Están los XML en 'Resources/XML/Blocks'?");
            return;
        }

        //  3. Preparar la carpeta de salida 
        string outputPath = "Assets/Resources/VRLearn_BlockPrefabs"; 
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
            Debug.Log($"Carpeta de salida creada en: {outputPath}");
        }

        // workspace falso, en memoria, solo para poder crear los modelos de bloque.
        WorkSpaceModel temporaryWorkspace = new WorkSpaceModel();

        // Barra de progreso para saber qué está pasando
        EditorUtility.DisplayProgressBar("Construyendo Prefabs", "Iniciando...", 0.0f);

        try
        {
            int index = 0;
            foreach (var definitionEntry in allDefinitions)
            {
                string blockType = definitionEntry.Key;
                BlockDefinition definition = definitionEntry.Value;

                // Actualiza la barra de progreso
                EditorUtility.DisplayProgressBar(
                    "Construyendo Prefabs",
                    $"Procesando bloque: {blockType}",
                    (float)index / allDefinitions.Count
                );

                Debug.Log($"--- Construyendo prefab para: {blockType} ---");

                //  4. Crear el MODELO de datos del bloque 
                BlockModel blockModel = BlockFactory.Instance.CreateBlock(temporaryWorkspace, blockType);
                if (blockModel == null)
                {
                    Debug.LogWarning($"No se pudo crear el modelo para el bloque '{blockType}'. Saltando...");
                    continue;
                }

                //  5. Construir la VISTA del GO a partir del modelo 
                
                GameObject blockViewObject = VRLearnBlockViewBuilder.BuildBlockView(blockModel, null);

                //  6. Guardar el GameObject como un Prefab 
                string prefabPath = Path.Combine(outputPath, "Block_" + blockType + ".prefab");

                // Elimina el prefab antiguo si existe para evitar conflictos.
                if (File.Exists(prefabPath))
                    AssetDatabase.DeleteAsset(prefabPath);

                // Guarda el nuevo prefab.
                PrefabUtility.SaveAsPrefabAsset(blockViewObject, prefabPath);

                //  7. Limpieza 
                // Destruye el GO temporal que se creó
                GameObject.DestroyImmediate(blockViewObject);

                index++;
            }
        }
        finally
        {
            // Limpia y guarda todos los cambios en los assets.
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>>>> CONSTRUCCIÓN DE {allDefinitions.Count} PREFABS COMPLETADA <<<<</color>");
        }
    }
}
#endif