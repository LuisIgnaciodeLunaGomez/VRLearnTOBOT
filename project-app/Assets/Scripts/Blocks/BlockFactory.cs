/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 21/02/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Clase que se encarga de la creación de los bloques para cada categoría
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlockFactory
{
    public static void CreateBlock(Transform parent, string type, string labelStart, string labelEnd, Sprite blockSprite, Color categoryColor)
    {
        GameObject newBlock = new GameObject(type, typeof(RectTransform), typeof(Image));
        newBlock.transform.SetParent(parent, false);
        newBlock.name = type;

        RectTransform rect = newBlock.GetComponent<RectTransform>();

        if (rect == null)
        {
            rect = newBlock.AddComponent<RectTransform>();
        }

        rect.anchorMin = new Vector2(0, 1);  // Arriba-izquierda
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(10, -40); // * parent.childCount); 
        rect.sizeDelta = new Vector2(100, 60);
        rect.localScale = Vector3.one;

        Image blockImage = newBlock.GetComponent<Image>();
        blockImage.sprite = blockSprite;
        blockImage.type = Image.Type.Sliced;
        blockImage.color = categoryColor;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(newBlock.transform, false);

        Vector2 pos = new Vector2(10, 0);
        float blockWidth = 20;  // Ancho inicial del bloque

        // Reemplazo "%1" con un InputField
        string[] parts = labelStart.Split(new string[] { "%1" }, System.StringSplitOptions.None);

        // Primera parte del texto (antes de %1)
        if (parts.Length > 0)
        {
            // CreateTextElement(parts[0], content.transform, pos);
            // pos.x += 60;
            GameObject textStart = CreateTextElement(parts[0], content.transform, pos);
            pos.x += textStart.GetComponent<RectTransform>().sizeDelta.x + 10;
            blockWidth += textStart.GetComponent<RectTransform>().sizeDelta.x + 10;
        }

        // Campo de entrada (en la posición de %1)
        // GameObject inputField = CreateInputField(content.transform, pos);
        //pos.x += 60;  // Mover la posición para el siguiente texto

        // Segunda parte del texto (después de %1)
        if (parts.Length > 1)
        {
            //CreateTextElement(parts[1], content.transform, pos);
            GameObject textEnd = CreateTextElement(parts[1], content.transform, pos);
            pos.x += textEnd.GetComponent<RectTransform>().sizeDelta.x + 10;
            blockWidth += textEnd.GetComponent<RectTransform>().sizeDelta.x + 10;
        }

        GameObject textStartGO = CreateTextElement(labelStart, content.transform, new Vector2(10, 0));
        GameObject numberGO = CreateInputField(content.transform, new Vector2(90, 0));
        GameObject textEndGO = CreateTextElement(labelEnd, content.transform, new Vector2(150, 0));

        // Ajustar el tamaño del bloque según su contenido
        rect.sizeDelta = new Vector2(blockWidth + 20, 60);
    }

    public static GameObject CreateTextElement(string text, Transform parent, Vector2 position)
    {
        // Crear el objeto de texto
        GameObject textGO = new GameObject("TextElement", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(parent, false);

        // Configurar el componente TextMeshProUGUI
        TextMeshProUGUI textComponent = textGO.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;  // Texto que se mostrará
        textComponent.fontSize = 24;
        textComponent.color = Color.black;
        textComponent.alignment = TextAlignmentOptions.Center;

        // Configurar la posición del texto dentro del bloque
        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(80, 40);
        rect.anchoredPosition = position;

        return textGO;
    }

    private static GameObject CreateInputField(Transform parent, Vector2 position)
    {
        // Crear el objeto InputField
        GameObject inputGO = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputGO.transform.SetParent(parent, false);

        // Configurar el RectTransform
        RectTransform rect = inputGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(60, 40);
        rect.anchoredPosition = position;

        // Configurar el fondo del InputField
        Image bgImage = inputGO.GetComponent<Image>();
        bgImage.color = Color.white;

        // Crear el objeto de texto dentro del InputField
        GameObject textGO = CreateTextElement("10", inputGO.transform, Vector2.zero);
        TextMeshProUGUI textComponent = textGO.GetComponent<TextMeshProUGUI>();

        // Configurar TMP_InputField
        TMP_InputField inputField = inputGO.GetComponent<TMP_InputField>();
        inputField.text = "10";  // Valor por defecto
        inputField.textComponent = textComponent;

        return inputGO;
    }

}

