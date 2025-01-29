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



using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ScrollBlocks : MonoBehaviour
{
    private VisualElement scrollBlocks; // Zona donde se mostrarán los bloques

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

    public void ShowEventBlocks()

    {
        Debug.Log("Cargando bloques de eventos...");

        // Limpia los bloques existentes
        scrollBlocks.Clear();
        var eventBlock = gameObject.AddComponent<EventBlockMutator>();

        // Ejemplo de bloque "al hacer clic en"
        var block1 = eventBlock.CreateEventBlock("al hacer clic en", " ");
        
        //Ojo los bloques se crean en evetBlocks.css o en json
        
        block1.AddToClassList("hat-block");

        scrollBlocks.Add(block1);

        // Agregar un menú desplegable al bloque
       // eventBlock.AddDropdown("Tecla:");

        // Ejemplo de bloque con número
       // var block2 = eventBlock.CreateEventBlock("cuando volumen del sonido
        // Añade los bloques de eventos
        foreach (var blockData in EventBlocks.Blocks)
        {
            Debug.Log($"Creando bloque: {blockData.text}");
            var block = CreateBlock(blockData.text, blockData.iconPath);
            scrollBlocks.Add(block);
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
        if (categoryData == null)
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
            string spritePath = $"Icons/{blockData.spriteName}";

            //Depuración

            Debug.Log($"Ruta de carga {spritePath} ");

            Texture2D spriteTexture = Resources.Load<Texture2D>(spritePath);

            if (spriteTexture == null)
            {
                Debug.LogError($"No se encontró el sprite en {spritePath}");
                continue;
            }
            
            var block = BlockUIFactory.CreateBlockElement(blockData.type,blockData.text, spritePath);
            scrollBlocks.Add(block);
        }
    }


    // Método para crear un bloque genérico
    private VisualElement CreateBlock(string text, string iconPath)
    {
        var block = new VisualElement();
        block.AddToClassList("block");

        // Contenido del bloque
        var content = new VisualElement();
        content.AddToClassList("block-content");

        // Texto
        var label = new Label(text);
        label.AddToClassList("block-label");

        // Icono (si corresponde)
        if (!string.IsNullOrEmpty(iconPath))
        {
            var icon = new VisualElement();
            icon.AddToClassList("block-icon");
            var iconTexture = Resources.Load<Texture2D>(iconPath);
            if (iconTexture != null)
            {
                icon.style.backgroundImage = new StyleBackground(iconTexture);
            }
            content.Add(icon);
        }

        content.Add(label);
        block.Add(content);

        return block;
    }


}
