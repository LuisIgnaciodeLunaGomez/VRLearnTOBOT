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
 * Descripción: Clase que se encarga de crear los elementos visuales de los bloques
 */


using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static BlockDataLoader;
using static UnityEngine.EventSystems.EventTrigger;

public class BlockUIFactory
{
    public static VisualElement CreateBlockElement(string blockType, Color categoryColor)
    {
        // Crear el contenedor principal del bloque
        var blockElement = new VisualElement();
        blockElement.AddToClassList("block");
        //blockElement.style.backgroundColor = categoryColor; // Aplicamos el color de la categoría

        // Obtener los datos del bloque (texto, icono, sprite)
        BlockData blockData = BlockDataLoader.GetBlockData(blockType);

        Debug.Log($"Cargando datos para el bloque: {blockType}");

        if (blockData == null)
        {
            Debug.LogError($"No se encontraron datos para el bloque: {blockType}");
            return blockElement;
        }

        Debug.Log($"Información cargada desde el JSON para {blockType}: {blockData}");

        // Obtener la forma del bloque
        BlockShapeData shapeData = BlockShapeLoader.GetBlockShape(blockData.spriteName);

        if (shapeData == null) return blockElement;

        // Cargar el sprite correspondiente
        string spritePath = $"Icons/Textures/{blockData.spriteName}";

        Texture2D originalSprite = Resources.Load<Texture2D>(spritePath);
        if (originalSprite == null)
        {
            Debug.LogError($"BlockUIFactory: No se pudo cargar el sprite en {spritePath}");
            return blockElement; // Retorna un bloque vacío si no encuentra el sprite
        }

        Debug.Log($"Sprite cargado: {spritePath}");

        Texture2D sprite = ApplyColorToTexture(originalSprite, categoryColor);
        // Ajustar la escala para mantener una relación con los bloques originales de Scratch
        float targetHeight = 80f;  // Altura base del bloque
        float scaleFactor = targetHeight / shapeData.height;  // Escalado proporcional

        Debug.Log($"Escala: {scaleFactor}");

        var backgroundImage = new VisualElement();
       // backgroundImage.AddToClassList("block-icon");
        backgroundImage.style.backgroundImage = new StyleBackground(sprite);
        backgroundImage.style.width = shapeData.width*scaleFactor;
        backgroundImage.style.height = targetHeight;// shapeData.height*scaleFactor;
        backgroundImage.style.position = Position.Relative;

        //Debug.Log($"Background color {blockElement.style.backgroundColor}");

        // Calcular tamaño del rectángulo interno
        float minWidth = shapeData.rect_width; // Ancho mínimo basado en la textura
        float contentWidth = Mathf.Max(minWidth, shapeData.rect_width + 20); // Crecimiento dinámico
        float offsetX = shapeData.rect_x - (contentWidth - minWidth); // Ajuste a la izquierda


        //Rectángulo dinámico dentro del bloque(contenedor del texto e íconos)
        var blockContainer = new VisualElement();
        //blockContainer.style.backgroundColor = categoryColor;
        //blockContainer.style.position = Position.Relative;
       // blockContainer.style.left = blockData.rect_x; // Se inicia en la posición original
       // blockContainer.style.top = blockData.rect_y;
        blockContainer.style.width = StyleKeyword.Auto;// blockData.rect_width; // Ancho inicial
        blockContainer.style.height = StyleKeyword.Auto;// blockData.rect_height;
        blockContainer.style.flexDirection = FlexDirection.Row;
         blockContainer.style.alignItems = Align.Center;
      //  blockContainer.style.paddingLeft = 10;

        //Ajustar crecimiento dinámico del rectángulo según contenido
        //float contentWidth = 0;

        //Icono del bloque (si existe)
        if (!string.IsNullOrEmpty(blockData.iconPath))
        {
            string iconPath = blockData.iconPath;
            Texture2D iconTexture = Resources.Load<Texture2D>(iconPath);

            if (iconTexture != null)
            {
                var icon = new VisualElement();
                icon.style.backgroundImage = new StyleBackground(iconTexture);
                icon.style.width = 24;
                icon.style.height = 24;
                blockContainer.Add(icon);
            }
           
            Debug.Log("Icono cargado!!!");
        }

        // Texto del bloque
        var textLabel = new Label(blockData.text);
       
        textLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        textLabel.style.color = Color.black;
        textLabel.style.marginLeft = 10;

        Debug.Log($"Texto: {blockData.text}");
        // Ajustar posiciones
        //blockElement.style.flexDirection = FlexDirection.Row;
        // blockElement.style.alignItems = Align.Center;
        ////backgroundImage.Add(dynamicRectangle);
        //lockElement.Add(backgroundImage);
        // blockElement.Add(dynamicRectangle);
        blockContainer.Add(backgroundImage);
        blockContainer.Add(textLabel); // Se alinea dinámicamente
        //blockContainer.Add(icon); // Se ajusta dentro del layout
        blockElement.Add(blockContainer); // El fondo ajusta su tamaño automáticamente

        //        blockElement.Add(textLabel);

        return blockElement;
    }

    //Método para cambiar el color de la imagen del bloque
    private static Texture2D ApplyColorToTexture(Texture2D original, Color color)
    {
        if (original == null)
        {
            Debug.LogError("ApplyColorToTexture: La textura original es NULL.");
            return null;
        }

        Debug.Log($"Aplicando color {color} a la textura {original.name}");

        //Creación de una copia de la tetura original con su formato adecuado
        Texture2D newTexture = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
        for (int x = 0; x < original.width; x++)
        {
            for (int y = 0; y < original.height; y++)
            {
               Color originalColor = original.GetPixel(x, y);
               Color newColor = new Color(color.r * originalColor.r, color.g * originalColor.g, color.b * originalColor.b, originalColor.a);
                newTexture.SetPixel(x, y, newColor);
            }
        }
        newTexture.Apply();
        return newTexture;
    }
}
