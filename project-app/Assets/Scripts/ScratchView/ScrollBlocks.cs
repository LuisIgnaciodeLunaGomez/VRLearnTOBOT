/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 26/01/2025
 * 
 * Versión: 1.0.0
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ScrollBlocks : MonoBehaviour
{
    private VisualElement scrollBlocks; // Zona donde se mostrarán los bloques

    // Se definen  colores por categoría
    Dictionary<string, Color> categoryColors = new Dictionary<string, Color>
{
    { "Movimiento", new Color(0.2f, 0.4f, 1f) },    // Azul
    { "Apariencia", new Color(0.6f, 0.4f, 1f) },   // Morado
    { "Sonido", new Color(1f, 0.4f, 0.6f) },       // Rosa
    { "Eventos", new Color(1f, 0.8f, 0f) },        // Amarillo
    { "Control", new Color(1f, 0.6f, 0f) },        // Naranja
    { "Sensores", new Color(0.4f, 0.8f, 1f) },     // Celeste
    { "Operadores", new Color(0.4f, 0.8f, 0.4f) }, // Verde
    { "Variables", new Color(1f, 0.6f, 0.2f) }     // Naranja oscuro
};


    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // Referencia a la zona de bloques
        scrollBlocks = root.Q<VisualElement>("ScrollBlocks");
        if (scrollBlocks == null)
        {
            Debug.LogError("No se encontró la zona de bloques (ScrollBlocks).");
            return;
        }
    }


    // Método para mostrar bloques de una categoría específica
    public void ShowBlocksByCategory(string categoryName)
    {
        Debug.Log($"Cargando bloques para la categoría: {categoryName}");

        // Limpia los bloques existentes
        scrollBlocks.Clear();

        // Ruta del archivo JSON
       // string jsonFilePath = $"{Application.dataPath}/Scripts/Blocks/JSONFiles/{categoryName.ToLower()}Blocks.json";
        string jsonFilePath = $"JSONFiles/{categoryName.ToLower()}Blocks"; // Sin la extensión ".json"
        Debug.Log($"Intentando cargar JSON desde: {jsonFilePath}");

        var categoryData = BlockDataLoader.LoadCategoryData(jsonFilePath);
        // Depuración: verificar si se cargó correctamente el JSON
        if (categoryData == null || categoryData.blocks ==null)
        {
            Debug.LogError($"No se pudo cargar el archivo JSON: {jsonFilePath}");
            return;
        }

        //Depuración: verificar estructura correcta del JSON

        if (categoryData.blocks == null || categoryData.blocks.Count == 0)
        {
            Debug.LogError($"El archivo JSON está vacío o mal estructurado: {jsonFilePath}");
            return;
        }

        Debug.Log($"Bloques encontrados: {categoryData.blocks.Count}");

        if (categoryData == null || categoryData.blocks == null)
        {
            Debug.LogError($"No se pudieron cargar los bloques para la categoría: {categoryName}");
            return;
        }

        Color categoryColor = GetCategoryColor(categoryName);


        // Crear y agregar cada bloque al área de trabajo
        foreach (var blockData in categoryData.blocks)
        {
            // Depuración: Asegurar que el JSON contiene los datos necesarios
            if (string.IsNullOrEmpty(blockData.spriteName) || string.IsNullOrEmpty(blockData.type))
            {
                Debug.LogError($"Faltan datos en blockData: {blockData}");
                continue;
            }

            // Ajustar la ruta correcta de carga de sprites
            string spritePath = $"Icons/Textures/{blockData.spriteName}";

            //Depuración

            Debug.Log($"Ruta de carga {spritePath} ");

            Texture2D spriteTexture = Resources.Load<Texture2D>(spritePath);

            if (spriteTexture == null)
            {
                Debug.LogError($"No se encontró el sprite en {spritePath}");
                continue;
            }

            // Obtener el color de la categoría
            Color blockColor = categoryColors.ContainsKey(categoryName) ? categoryColors[categoryName] : Color.gray;

            // Crear bloque con color de fondo dinámico

            var block = BlockUIFactory.CreateBlockElement(blockData.spriteName, categoryColor);
           // block.style.backgroundColor = blockColor;  // Aplicar color al fondo

            scrollBlocks.Add(block);
        }
    }


    // Método para obtener el color de la categoría
    private Color GetCategoryColor(string categoryName)
    {
        switch (categoryName.ToLower())
        {
            case "eventos": return new Color(1f, 0.8f, 0f); // Amarillo
            case "movimiento": return new Color(0.2f, 0.4f, 1f); // Azul
            case "apariencia": return new Color(0.6f, 0.4f, 1f); // Morado
            case "sonido": return new Color(1f, 0.4f, 0.6f); // Rosa
            case "control": return new Color(1f, 0.6f, 0f); // Naranja
            default: return Color.gray; // Color por defecto
        }
    }
}
