/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 31/01/2025
 * 
 * Versión: 1.0.0
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using System;

[System.Serializable]
public class BlockShapeData
{
    public int width;
    public int height;
    public int rect_x;
    public int rect_y;
    public int rect_width;
    public int rect_height;
    public string spriteName;
    public string iconPath;
    public string text;
}

[System.Serializable]
public class BlockData
{
    public string type;
    public string text;
    public string iconPath;
    public string spriteName;
 
}

[System.Serializable]
public class BlockCategory
{
    public string category;
    public List<BlockData> blocks;
}

[System.Serializable]
public class BlockShapeCollection
{
    public Dictionary<string, BlockShapeData> blocks;
}

[InitializeOnLoad]
public class BlockShapeLoader : EditorWindow
{
    private static string jsonFilePath = Path.Combine(Application.dataPath, "Resources/block_shapes.json"); //Archvo con las formas de los bloques para calcular donde introducir las partes dinámicas.
    private static string categoryJsonPath = Path.Combine(Application.dataPath, "Resources/JSONFiles/EventosBlocks.json");  // Archivo con categorías


    private static Dictionary<string, BlockShapeData> blockShapes;

    static BlockShapeLoader()
    {
        LoadBlockShapes();  // Se ejecutará automáticamente al iniciar Unity
    }

    [MenuItem("Blocks/Cargar forma de los bloques")] // Agrega una opción en el menú de Unity
    public static void LoadBlockShapes()
    {
        if (!File.Exists(jsonFilePath))
        {
            Debug.LogError($" No se encontró el archivo JSON en: {jsonFilePath}");
            return;
        }

        // Cargar los datos del JSON generado por Python
        string shapeJson = File.ReadAllText(jsonFilePath);


        try
        {
            // Deserializar correctamente el JSON asegurando la estructura
            BlockShapeCollection shapeCollection = JsonConvert.DeserializeObject<BlockShapeCollection>(shapeJson);

            if (shapeCollection != null && shapeCollection.blocks != null)
            {
                blockShapes = shapeCollection.blocks;
                Debug.Log($"Bloques cargados correctamente: {blockShapes.Count}");

                // Imprimir nombres de los bloques cargados
                foreach (var key in blockShapes.Keys)
                {
                    Debug.Log($" - Bloque cargado: {key}");
                }
            }
            else
            {
                Debug.LogError("La estructura del JSON no es válida o está vacía.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al leer el JSON de formas: {e.Message}");
        }

        // Cargar los datos de la estructura de bloques (eventos, etc.)
        string categoryJson = File.ReadAllText(categoryJsonPath);

        try
        {
            BlockCategory categoryData = JsonConvert.DeserializeObject<BlockCategory>(categoryJson);

            if (categoryData != null && categoryData.blocks != null)
            {
                Debug.Log($"Categoría cargada correctamente: {categoryData.category}");
            }
            else
            {
                Debug.LogError("La estructura del JSON no es válida o está vacía.");
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"Error al leer el JSON de blqoues: {e.Message}");

        }
    }
	
	 //**Método para obtener datos de un bloque específico**
    public static BlockShapeData GetBlockData(string blockName)
    {
        if (blockShapes == null )
        {
            Debug.LogError("BlockShapeLoader: blockShapes no ha sido inicializado.");
            // return null;
            LoadBlockShapes();  // Forzar la carga de bloques

        }

        if (blockShapes == null || blockShapes.Count == 0)
        {
            Debug.LogError("Error: blockShapes sigue vacío después de LoadBlockShapes.");
            return null;
        }

        if (!blockShapes.ContainsKey(blockName))
        {
            Debug.LogError($"No se encontraron datos para el bloque: {blockName}");

            // Imprimir todas las claves disponibles para depuración
            Debug.Log("Bloques disponibles en blockShapes:");
            foreach (var key in blockShapes.Keys)
            {
                Debug.Log($" - {key}");
            }

            return null;
        }


        return blockShapes[blockName];
    }
    /* Método para obtener la forma de un bloque específico */
    public static BlockShapeData GetBlockShape(string spriteName)
    {
        if (blockShapes == null || !blockShapes.ContainsKey(spriteName))
        {
            Debug.LogError($"❌ No se encontró la forma del bloque para: {spriteName}");
            return null;
        }
        return blockShapes[spriteName];
    }

    [Serializable]
    public class BlockShapeCollection
    {
        public Dictionary<string, BlockShapeData> blocks { get; set; }  // Asegurar que la propiedad tenga "set;"
    }

    [Serializable]
    public class BlockShapeData
    {
        public int width { get; set; }
        public int height { get; set; }
        public int rect_x { get; set; }
        public int rect_y { get; set; }
        public int rect_width { get; set; }
        public int rect_height { get; set; }

        public string spriteName { get; set; }

        public string iconPath { get; set; }

        public string text { get; set; }
    }
}
