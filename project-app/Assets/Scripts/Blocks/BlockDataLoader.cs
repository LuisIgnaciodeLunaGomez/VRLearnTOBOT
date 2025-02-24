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
using System.Xml;
using Unity.VisualScripting;
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
    private static Dictionary<string, BlockData> m_blockData = new Dictionary<string, BlockData>();
    private static Dictionary<string, Vector2> m_blockSizes = new Dictionary<string, Vector2>();

    // Método para cargar los datos de bloques desde un archivo JSON
    public static BlockCategoryData LoadCategoryData(string categoryName)
    {
        Debug.Log($"Intentando cargar XML: {categoryName}");

        // var jsonText = Resources.Load<TextAsset>(jsonFilePath); // Cargo desde la carpeta Resources
        string xmlFilePath = $"{categoryName}";

        TextAsset xmlFile = Resources.Load<TextAsset>(xmlFilePath);
        if (xmlFile == null)
        {
            Debug.LogError($"No se pudo cargar el archivo JSON: {xmlFilePath}");
            return null;
        }

        Debug.Log($"XML encontrado: {xmlFilePath}, contenido: {xmlFile.text}");

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlFile.text); // Cargo el archivo XML

        if (xmlDoc.DocumentElement == null)
        {
            Debug.LogError(" Error: `DocumentElement` es NULL. El XML podría estar mal formateado o no se está leyendo correctamente.");
            return null;
        }

        try
        {
                     
            BlockCategoryData categoryData = new BlockCategoryData
            {
                category = categoryName,
                blocks = new List<BlockData>()
            };
            

            foreach (var block in categoryData.blocks)
            {
                Debug.Log($"Tipo: {block.type}, Label: {block.text}, SpriteName: {block.spriteName}");
            }

            XmlNodeList blockNodes = xmlDoc.SelectNodes("/Blocks/Block");

            if(blockNodes == null || blockNodes.Count == 0)
            {
                Debug.LogWarning($"No se encontraron bloques en el archivo XML: {xmlFilePath}");
                return null;
            }

            Debug.Log($" Bloques encontrados en {xmlFilePath}: {blockNodes.Count}");

            foreach (XmlNode blockNode in blockNodes)
            {
                Debug.Log($" Bloque encontrado: {blockNode.InnerXml}");
            }

            foreach (XmlNode blockNode in blockNodes)
            {
                BlockData blockData = new BlockData
                {
                    type = blockNode.SelectSingleNode("Type").Value,
                    text = blockNode.SelectSingleNode("Label").Value,
                    spriteName = blockNode.SelectSingleNode("SpriteName")?.InnerText.Trim()
                };

                // Almacena el bloque en m_blockData usando el type como clave
                if (!string.IsNullOrEmpty(blockData.type))
                {
                    m_blockData[blockData.type] = blockData;
                }

                if (string.IsNullOrEmpty(blockData.spriteName))
                {
                    Debug.LogWarning($"El bloque {blockData.type} no tiene un spriteName definido en el XML");
                }

                XmlNode spriteNode = blockNode.SelectSingleNode("SpriteName");

                if (spriteNode != null)
                {
                    blockData.spriteName = spriteNode.InnerText.Trim();
                    Debug.Log($" SpriteName encontrado para `{blockData.type}`: {blockData.spriteName}");
                }
                else
                {
                    Debug.LogWarning($" El bloque `{blockData.type}` no tiene un SpriteName definido en el XML.");
                }

                if (string.IsNullOrEmpty(blockData.spriteName))
                {
                    Debug.LogWarning($"El bloque {blockData.type} no tiene un spriteName definido en el XML");
                }


                categoryData.blocks.Add(blockData);
            }


            return categoryData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al parsear JSON {xmlFilePath}: {e.Message}");
            return null;
        }
    }

    // Método para obtener los datos de un bloque específico
    public static BlockData GetBlockData(string blockName)
    {
        Debug.Log($"Buscando datos del bloque: {blockName}");

        // Verificar qué claves están en el diccionario
        Debug.Log("Claves disponibles en blockData:");
        foreach (var key in m_blockData.Keys)
        {
            Debug.Log($" - {key}");
        }

        string foundKey = m_blockData.Keys.FirstOrDefault(k => k.Contains(blockName));

        Debug.Log($"Clave encontrada: {foundKey}");

        if (m_blockData.Count == 0)
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

        Debug.Log($"Datos obtenidos para {foundKey}: {m_blockData[foundKey].text}");
        return m_blockData[foundKey];
    }


    // Método para obtener el tamaño de un bloque específico
    public static void LoadBlockSizes()
    {
        string xmlFilePath = "XML/BlockSizes"; 
        TextAsset xmlFile = Resources.Load<TextAsset>(xmlFilePath);

        if (xmlFile == null)
        {
            Debug.LogError($"No se pudo cargar el archivo XML de tamaños de bloques: {xmlFilePath}");
            return;
        }

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlFile.text);

        XmlNodeList sizeNodes = xmlDoc.SelectNodes("/BlockSizes/BlockType");

        foreach (XmlNode sizeNode in sizeNodes)
        {
            string type = sizeNode.SelectSingleNode("Type")?.InnerText.Trim();
            float width = float.Parse(sizeNode.SelectSingleNode("Width")?.InnerText.Trim() ?? "316");
            float height = float.Parse(sizeNode.SelectSingleNode("Height")?.InnerText.Trim() ?? "175");

            if (!string.IsNullOrEmpty(type))
            {
                m_blockSizes[type] = new Vector2(width, height);
            }
        }

        Debug.Log("Tamaños de bloques cargados correctamente.");
    }

    public static Vector2 GetBlockSize(string type)
    {
        return m_blockSizes.ContainsKey(type) ? m_blockSizes[type] : new Vector2(316, 175); // Tamaño por defecto
       /* return m_blockSizes.ContainsKey(type) ?
        new Vector2(m_blockSizes[type].x * 0.16f, m_blockSizes[type].y * 0.16f) :
        new Vector2(316 * 0.16f, 175 * 0.16f);*/
    }
}