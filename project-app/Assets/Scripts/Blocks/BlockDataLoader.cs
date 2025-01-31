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

    // Método para cargar los datos de bloques desde un archivo JSON
    public static BlockCategoryData LoadCategoryData(string jsonFilePath)
    {
        var jsonText = Resources.Load<TextAsset>(jsonFilePath); // Cargar desde la carpeta Resources
        if (jsonText == null)
        {
            Debug.LogError($"No se pudo cargar el archivo JSON: {jsonFilePath}");
            return null;
        }

        Debug.Log($"JSON encontrado: {jsonFilePath}, contenido: {jsonText.text}");

        try
        {
            return JsonUtility.FromJson<BlockCategoryData>(jsonText.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al parsear JSON {jsonFilePath}: {e.Message}");
            return null;
        }
    }
}