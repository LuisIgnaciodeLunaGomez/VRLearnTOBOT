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
    public static VisualElement CreateBlockElement(string blockType, string textContent, string spritePath)
    {
        // Crear el contenedor principal del bloque
        var blockElement = new VisualElement();
        blockElement.AddToClassList("block");

        // Cargar el sprite desde Resources
        Texture2D sprite = Resources.Load<Texture2D>(spritePath);
        if (sprite == null)
        {
            Debug.LogError($"BlockUIFactory: No se pudo cargar el sprite en {spritePath}");
            return blockElement; // Retorna un bloque vacío si no encuentra el sprite
        }

        Debug.Log($"Sprite cargado: {spritePath}");

        // Crear la imagen de fondo del bloque
        var backgroundImage = new VisualElement();
       
        backgroundImage.style.backgroundImage = new StyleBackground(sprite);
        backgroundImage.style.width = sprite.width;
        backgroundImage.style.height = sprite.height;
        backgroundImage.style.position = Position.Absolute;
        backgroundImage.AddToClassList("block-icon");

        // Agregar la imagen de fondo
        blockElement.Add(backgroundImage);

        // Crear la parte editable (Texto u otros elementos)
        var content = new VisualElement();
        content.AddToClassList("block-content");

        // Etiqueta con el texto del bloque
        var label = new Label(textContent);
        label.AddToClassList("block-label");
        content.Add(label);

        // Añadir el contenido por encima de la imagen de fondo
       // content.Add(icon);
        content.Add(label);
        content.Add(backgroundImage);
        blockElement.Add(content);

        return blockElement;
    }
}
