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
using System.Linq;

[System.Serializable]
public class BlockShapeData
{
    public string spriteName;
    public float width;
    public float height;
    public float rect_x;
    public float rect_y;
    public float rect_width;
    public float rect_height;

    public static explicit operator BlockShapeData(BlockShapeLoader.BlockShapeCollection.BlockShapeData v)
    {
        throw new NotImplementedException();
    }
    //public bool hasHat;
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


    private static Dictionary<string, BlockShapeData> blockShapes = new Dictionary<string, BlockShapeData>();


    static BlockShapeLoader()
    {
        Debug.Log("Iniciando carga de bloques...");
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

        Debug.Log($"Cargando JSON de formas desde: {jsonFilePath}");

        try
        {
            // Deserializar correctamente el JSON asegurando la estructura
            BlockShapeCollection shapeCollection = JsonConvert.DeserializeObject<BlockShapeCollection>(shapeJson);

            if (shapeCollection != null && shapeCollection.blocks != null)
            {
                blockShapes.Clear();
                blockShapes = shapeCollection.blocks.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new BlockShapeData
                    {
                        spriteName = kvp.Value.spriteName,
                        width = kvp.Value.width,
                        height = kvp.Value.height,
                        rect_x = kvp.Value.rect_x,
                        rect_y = kvp.Value.rect_y,
                        rect_width = kvp.Value.rect_width,
                        rect_height = kvp.Value.rect_height
                    });
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
      
    }

    /* Método para obtener la forma de un bloque específico */
    public static BlockShapeData GetBlockShape(string spriteName)
    {
        spriteName = spriteName.Trim();
        Debug.Log($"Buscando forma del bloque para: {spriteName}");

        // Verificar si el spriteName contiene un "_", lo cual indica que es una clave única
        if (spriteName.Contains("_"))
        {
            spriteName = spriteName.Split('_')[0];  // Recuperar solo el spriteName original
        }
        if (blockShapes.ContainsKey(spriteName))
        {
            Debug.Log($"Forma obtenida para {spriteName}: {blockShapes[spriteName].spriteName}");
            return blockShapes[spriteName];
        }

        // Intentar encontrar una clave similar si no existe exactamente
        string foundKey = blockShapes.Keys.FirstOrDefault(k => k.Contains(spriteName) || spriteName.Contains(k));

        /* if (blockShapes == null || !blockShapes.ContainsKey(spriteName))
         {
             Debug.LogError($" No se encontró la forma del bloque para: {spriteName}");
             foreach (var key in blockShapes.Keys)
             {
                 Debug.Log($" - {key}");
             }
             return null;
         }*/

        if (foundKey != null)
        {
            Debug.Log($"Usando clave alternativa encontrada: {foundKey}");
            return blockShapes[foundKey];
        }

        //Debug.Log($"Forma obtenida para {spriteName}: {blockShapes[spriteName].spriteName}");

        //return blockShapes[spriteName];

        Debug.LogError($"No se encontró la forma del bloque para: {spriteName}");
        return null;
    }

    [Serializable]
    public class BlockShapeCollection
    {
        public Dictionary<string, BlockShapeData> blocks { get; set; }

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
}
