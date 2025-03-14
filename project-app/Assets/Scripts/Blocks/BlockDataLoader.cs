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


using System.Collections.Generic;
using System.Xml;
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
        public string label;      // Etiqueta del bloque
        public List<BlockArg> args = new List<BlockArg>(); // Lista de argumentos
    }
    [System.Serializable]
    public class BlockArg
    {
        public string type;      // Tipo de argumento (label, input, etc.)
        public string value;     // Texto del label (si aplica)
        public string name;      // Nombre del input (si aplica)
        public string inputType; // Tipo del input (number, text, etc.)
        public string defaultValue; // Valor por defecto (si aplica)
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
        //Debug.Log($"Intentando cargar XML: {categoryName}");

        // var jsonText = Resources.Load<TextAsset>(jsonFilePath); // Cargo desde la carpeta Resources
        string xmlFilePath = $"{categoryName}";

        TextAsset xmlFile = Resources.Load<TextAsset>(xmlFilePath);
        if (xmlFile == null)
        {
            Debug.LogError($"No se pudo cargar el archivo JSON: {xmlFilePath}");
            return null;
        }

        // Valido para depurar el xml
        //Debug.Log($"XML encontrado: {xmlFilePath}, contenido: {xmlFile.text}"); 

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
                Debug.Log($"Tipo: {block.type}, Label: {block.label}, SpriteName: {block.spriteName}");
            }

            XmlNodeList blockNodes = xmlDoc.SelectNodes("/Blocks/Block");


            if(blockNodes == null || blockNodes.Count == 0)
            {
                Debug.LogWarning($"No se encontraron bloques en el archivo XML: {xmlFilePath}");
                return null;
            }

            //Debug.Log($" Bloques encontrados en {xmlFilePath}: {blockNodes.Count}");

            /*foreach (XmlNode blockNode in blockNodes)
            {
                Debug.Log($" Bloque encontrado: {blockNode.InnerXml}");
            }*/

            foreach (XmlNode blockNode in blockNodes)
            {
                XmlNode typeNode = blockNode.SelectSingleNode("Type");
                XmlNode labelNode = blockNode.SelectSingleNode("Label");
                XmlNode spriteNode = blockNode.SelectSingleNode("SpriteName");

                BlockData blockData = new BlockData
                {
                    /* type = blockNode.SelectSingleNode("Type").Value,
                     label = blockNode.SelectSingleNode("Label").InnerText.Trim(),
                     spriteName = blockNode.SelectSingleNode("SpriteName")?.InnerText.Trim()*/

                    type = typeNode?.InnerText?.Trim() ?? "Unknown",
                    label = labelNode?.InnerText?.Trim() ?? "Unnamed",
                    spriteName = spriteNode?.InnerText?.Trim() ?? "NoSprite",
                    args = new List<BlockArg>() // Inicializa la lista de argumentos
                };

                // Leer los <args>
                /* XmlNodeList argsNodes = blockNode.SelectNodes("args/arg");

                 if (argsNodes == null)
                 {
                     Debug.LogError($"No se encontró el nodo <args> en el bloque {blockData.type}");
                 }
                 else
                 {

                     Debug.Log($" Bloque {blockData.type} tiene {argsNodes.Count} argumentos");

                 }*/

                // Comprobamos si existe el nodo <args>
                XmlNode argsNode = blockNode.SelectSingleNode("args");
                if (argsNode == null)
                {
                   // Debug.LogWarning($"No se encontró el nodo <args> en el bloque `{blockData.type}`. XML del bloque: {blockNode.OuterXml}");
                }
                else
                {
                   // Debug.Log($"Nodo <args> encontrado en `{blockData.type}`: {argsNode.OuterXml}");
                }

                XmlNodeList argsNodes = blockNode.SelectNodes("args/arg");

                if (argsNodes != null && argsNodes.Count > 0)
                {

                    foreach (XmlNode argNode in argsNodes)
                    {
                        BlockArg arg = new BlockArg
                        {
                            type = argNode.Attributes["type"]?.Value,
                            value = argNode.Attributes["value"]?.Value,
                            name = argNode.Attributes["name"]?.Value,
                            inputType = argNode.Attributes["inputType"]?.Value,
                            defaultValue = argNode.Attributes["default"]?.Value
                        };

                        blockData.args.Add(arg);
                        //Debug.Log($"Arg: type={arg.type}, value={arg.value}, name={arg.name}, inputType={arg.inputType}, default={arg.defaultValue}");
                    }

                }
                else
                {
                    Debug.LogWarning($"El bloque `{blockData.type}` no tiene argumentos definidos.");
                }            
                
                


                // Almacena el bloque en m_blockData usando el type como clave
                if (!string.IsNullOrEmpty(blockData.type))
                {
                    m_blockData[blockData.type] = blockData; //Se guarda la información del bloque en el diccionario
                }
                else
                {
                    Debug.LogError($"El bloque {blockData.type} tiene un `type` nulo o vacío. Verifica el XML.");
                }

                if (string.IsNullOrEmpty(blockData.spriteName))
                {
                    Debug.LogWarning($"El bloque {blockData.type} no tiene un spriteName definido en el XML");
                }

               

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

    public static Vector2 GetBlockSize(string type)
    {
        return m_blockSizes.ContainsKey(type) ? m_blockSizes[type] : new Vector2(316, 175); // Tamaño por defecto
       /* return m_blockSizes.ContainsKey(type) ?
        new Vector2(m_blockSizes[type].x * 0.16f, m_blockSizes[type].y * 0.16f) :
        new Vector2(316 * 0.16f, 175 * 0.16f);*/
    }
}