/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 27/01/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Clase que se encarga de cargar los datos de los bloques desde un archivo JSON
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
public class BlockDataLoader
{
    [System.Serializable]
    public class BlockData
    {
        public string type;     // Tipo de bloque
        public string text;      // Texto del bloque
        public string iconPath;   // Ruta al icono del bloque
        public string spriteName; // Nombre del sprite
    }

    [System.Serializable]
    public class BlockCategoryData
    {
        public string category;      // Nombre de la categoría
        public List<BlockData> blocks; // Lista de bloques
    }

    // Diccionario para almacenar los datos de los bloques
    private static Dictionary<string, BlockData> blockData = new Dictionary<string, BlockData>();

    // Método para cargar los datos de bloques desde un archivo JSON
    public static BlockCategoryData LoadCategoryData(string jsonFilePath)
    {
        var jsonText = Resources.Load<TextAsset>(jsonFilePath); // Cargo desde la carpeta Resources

        if (jsonText == null)
        {
            Debug.LogError($"No se pudo cargar el archivo JSON: {jsonFilePath}");
            return null;
        }

        Debug.Log($"JSON encontrado: {jsonFilePath}, contenido: {jsonText.text}");

        try
        {
            BlockCategoryData categoryData = JsonUtility.FromJson<BlockCategoryData>(jsonText.text);
            if (categoryData != null && categoryData.blocks != null)
            {
                blockData.Clear();
                foreach (var block in categoryData.blocks)
                {
                    string uniqueKey = block.spriteName + "_" + block.type; // Clave única para evitar sobrescritura
                    blockData[uniqueKey] = block;
                    Debug.Log($"Bloque cargado: {block.type} - {block.text} con clave {uniqueKey}");

                }
            }
            return categoryData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al parsear JSON {jsonFilePath}: {e.Message}");
            return null;
        }
    }

    // Método para obtener los datos de un bloque específico
    public static BlockData GetBlockData(string blockName)
    {
        Debug.Log($"Buscando datos del bloque: {blockName}");

        // Verificar qué claves están en el diccionario
        Debug.Log("Claves disponibles en blockData:");
        foreach (var key in blockData.Keys)
        {
            Debug.Log($" - {key}");
        }

        string foundKey = blockData.Keys.FirstOrDefault(k => k.Contains(blockName));

        Debug.Log($"Clave encontrada: {foundKey}");

        if (blockData.Count == 0)
        {
            Debug.LogError("Error: blockData sigue vacío.");
            return null;
        }

        /*if (!blockData.ContainsKey(blockName))
        {
            Debug.LogError($"No se encontraron datos para el bloque: {blockName}");
            return null;
        }*/

        if (foundKey == null)
        {
            Debug.LogError($"No se encontraron datos para el bloque: {blockName}");
            return null;
        }

        Debug.Log($"Datos obtenidos para {foundKey}: {blockData[foundKey].text}");
        return blockData[foundKey];
    }
}