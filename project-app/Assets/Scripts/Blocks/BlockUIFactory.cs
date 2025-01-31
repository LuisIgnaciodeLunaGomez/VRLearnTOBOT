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
 */



using UnityEngine;
using UnityEngine.UIElements;

public class BlockUIFactory
{
    public static VisualElement CreateBlockElement(string blockType, Color categoryColor)
    {
        // Crear el contenedor principal del bloque
        var blockElement = new VisualElement();
        blockElement.AddToClassList("block");
        blockElement.style.backgroundColor = categoryColor; // 🔹 Aplicamos el color de la categoría

        // Obtener los datos del bloque (texto, icono, sprite)
        BlockShapeLoader.BlockShapeData blockData = BlockShapeLoader.GetBlockData(blockType);

        if (blockData == null)
        {
            Debug.LogError($"No se encontraron datos para el bloque: {blockType}");
            return blockElement;
        }

        Debug.Log($"Información cargada desde el JSON para {blockType}: {blockData}");

        // Obtener la forma del bloque
        BlockShapeLoader.BlockShapeData shapeData = BlockShapeLoader.GetBlockShape(blockData.spriteName);
        if (shapeData == null) return blockElement;

        // Cargar el sprite correspondiente
        string spritePath = $"Icons/{blockData.spriteName}";

        Texture2D sprite = Resources.Load<Texture2D>(spritePath);
        if (sprite == null)
        {
            Debug.LogError($"BlockUIFactory: No se pudo cargar el sprite en {spritePath}");
            return blockElement; // Retorna un bloque vacío si no encuentra el sprite
        }

        Debug.Log($"Sprite cargado: {spritePath}");


        var backgroundImage = new VisualElement();
        backgroundImage.AddToClassList("block-icon");
        backgroundImage.style.backgroundImage = new StyleBackground(sprite);
        backgroundImage.style.width = shapeData.width;
        backgroundImage.style.height = shapeData.height;

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
                backgroundImage.Add(icon);
            }
        }

        // Texto del bloque
        var textLabel = new Label(blockData.text);
        textLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        textLabel.style.color = Color.black;
        textLabel.style.marginLeft = 10;

        // Ajustar posiciones
        blockElement.style.flexDirection = FlexDirection.Row;
        blockElement.style.alignItems = Align.Center;
        blockElement.Add(backgroundImage);
        blockElement.Add(textLabel);

        return blockElement;
    }
}
