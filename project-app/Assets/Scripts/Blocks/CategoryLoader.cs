/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 18/02/2025
 * 
 * Versión: 1.0.2
 * 
 * Descripción: : Carga la definición de las categorías desde un XML y crea los elementos UI correspondientes en el panel izquierdo.
 */

using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public static class CategoryLoader
{

    private static Dictionary<string, Color> s_categoryInfoCache = null;
    

    /// Carga la información Nombre->Color de las categorías desde el XML especificado.
    /// Método rstático para ser llamado durante la inicialización.
    /// </summary>
    /// <param name="resourcePath">Ruta del archivo XML dentro de Resources (sin extensión).</param>
    /// <returns>Un diccionario mapeando nombre de categoría a su color.</returns>
    public static Dictionary<string, Color> LoadCategoryInfo(string resourcePath = "XML/Categories")
    {
        var categoryColorMap = new Dictionary<string, Color>(System.StringComparer.OrdinalIgnoreCase); 
        TextAsset xmlData = Resources.Load<TextAsset>(resourcePath);

        if (xmlData == null)
        {
            Debug.LogError($"CategoryLoader.LoadCategoryInfo: No se pudo cargar el archivo XML: {resourcePath}");
            return categoryColorMap; // Devolver diccionario vacío
        }

        try
        {
            XDocument xmlDoc = XDocument.Parse(xmlData.text);
            XElement rootElement = xmlDoc.Root;
            if (rootElement == null || rootElement.Name.LocalName != "Categories")
            {
                Debug.LogError($"CategoryLoader.LoadCategoryInfo: Archivo XML '{resourcePath}' no contiene la etiqueta raíz <Categories>.");
                return categoryColorMap;
            }

            IEnumerable<XElement> categories = rootElement.Elements("Category");

            foreach (XElement categoryElement in categories)
            {
                string name = categoryElement.Element("Name")?.Value; // Usamos ? para evitar null ref
                string colorHex = categoryElement.Element("Color")?.Value;

                if (string.IsNullOrEmpty(name))
                {
                    Debug.LogWarning("CategoryLoader.LoadCategoryInfo: Se encontró una categoría sin <Name>. Saltando.");
                    continue;
                }
                if (string.IsNullOrEmpty(colorHex))
                {
                    Debug.LogWarning($"CategoryLoader.LoadCategoryInfo: Categoría '{name}' no tiene <Color>. Usando gris por defecto.");
                    colorHex = "#808080"; // Gris por defecto
                }


                Color categoryColor;
                if (UnityEngine.ColorUtility.TryParseHtmlString(colorHex, out categoryColor))
                {
                    if (!categoryColorMap.ContainsKey(name))
                    {
                        categoryColorMap.Add(name, categoryColor);
                    }
                    else
                    {
                        Debug.LogWarning($"CategoryLoader.LoadCategoryInfo: Nombre de categoría duplicado '{name}' encontrado en {resourcePath}. Se usará el primer color encontrado.");
                    }
                }
                else
                {
                    Debug.LogError($"CategoryLoader.LoadCategoryInfo: No se pudo parsear el color '{colorHex}' para la categoría '{name}'. Usando negro por defecto.");
                    if (!categoryColorMap.ContainsKey(name))
                        categoryColorMap.Add(name, Color.black);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CategoryLoader.LoadCategoryInfo: Error parseando XML '{resourcePath}': {e.Message}\n{e.StackTrace}");
        }

        return categoryColorMap;
    }    
    
   // Método para limpiar el caché si necesitas recargar la configuración
    public static void ClearCache()
    {
        s_categoryInfoCache = null;
        Debug.Log("CategoryLoader: Cache cleared.");
    }
}
