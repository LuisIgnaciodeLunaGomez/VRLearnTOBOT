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

using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Xml;
using System.Dynamic;
using System;
using System.Web;
using System.Security.Cryptography;


public class BlockManager : MonoBehaviour
{
   // public GameObject blockPrefab;
    public Transform blockContainer;

    /**
     * LoadBlocks
     * 
     * Load blocks from a category
     * 
     * @param string categoryName
     * @param Color categoryColor
     * @return void
     */
    public void LoadBlocks(string categoryName, Color categoryColor)
    {

        string xmlPath = "XML/Blocks/" + categoryName;
        TextAsset xmlData = Resources.Load<TextAsset>(xmlPath);

        if (xmlData == null)
        {
            Debug.LogError("No se pudo cargar el archivo XML" + xmlPath);
            return;
        }

        //Elimino los bloques anteriores antes de cargar nuevos

        foreach (Transform child in blockContainer)
        {
            Destroy(child.gameObject);
        }

        XDocument xmlDoc = XDocument.Parse(xmlData.text);
        IEnumerable<XElement> blocks = xmlDoc.Element("Blocks").Elements("Block");

        foreach (XElement block in blocks)
        {
            string type = block.Element("Type").Value;
            string label = block.Element("Label").Value;
            string spriteName = block.Element("Sprite").Value;

            // Compruebo si LabelStart y LabelEnd existen en el XML para generar los bloques provisionales
            string labelStart = block.Element("LabelStart") != null ? block.Element("LabelStart").Value : label;
            string labelEnd = block.Element("LabelEnd") != null ? block.Element("LabelEnd").Value : "";


            bool hasTopConnection = block.Element("connections")?.Element("top")?.Value == "true";
            bool hasBottomConeection = block.Element("connections")?.Element("bottom")?.Value == "true";

            // Cargar la textura del bloque
            Sprite blockSprite = Resources.Load<Sprite>("Textures/" + spriteName);

            if (blockSprite == null)
            {
                Debug.LogError("No se pudo cargar la textura en bloques " + spriteName);
                return;
            }

           // CreateBlock(type, label, categoryColor, blockSprite, hasTopConnection, hasBottomConeection);

            // Crear el bloque
            BlockFactory.CreateBlock(blockContainer, type, labelStart, labelEnd, blockSprite, categoryColor);

        }
    }
    /**
     * CreateBlock
     * 
     * Create a block
     * 
     * @param string type
     * @param string label
     * @param Color categoryColor
     * @return void
     */
    private void CreateBlock(string type, string label, Color categoryColor, Sprite blockSprite, bool hasTopConnection, bool hasBottomConnection)
    {
        // Crear el objeto del bloque
        GameObject newBlock = new GameObject(type, typeof(RectTransform), typeof(Image));
        newBlock.transform.SetParent(blockContainer, false);
        newBlock.name = type;

        RectTransform rect = newBlock.GetComponent<RectTransform>();

        if (rect == null)
        {
            rect = newBlock.AddComponent<RectTransform>();
        }

        // Configuro anclajes y posición para que los bloques aparezcan en la zona correcta
        rect.anchorMin = new Vector2(0, 1);  // Arriba-izquierda
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(10, -40); // * blockContainer.childCount); 
        rect.sizeDelta = new Vector2(100, 60);
        rect.localScale = Vector3.one;

        //Configuro el tamaño y el color del bloque
        Image blockImage = newBlock.GetComponent<Image>();
        blockImage.sprite = blockSprite;
        blockImage.type = Image.Type.Sliced; // 9-Slice para que pueda expandirse
        // Configuro el tamaño de la imagen
        blockImage.rectTransform.localScale = new Vector3(0.2f, 0.2f, 1f);
        blockImage.color = categoryColor;

        // Crear un contenedor para el contenido del bloque
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(newBlock.transform, false);

        // Texto inicial ("mover")
        GameObject textStartGO = CreateTextElement(label, content.transform, new Vector2(10, 0));

        // Campo numérico
        GameObject numberGO = CreateInputField(content.transform, new Vector2(90, 0));

        // Texto final ("pasos")
        GameObject textEndGO = CreateTextElement("pasos", content.transform, new Vector2(150, 0));

        //Configuración de conexiones visuales
        Transform topConnection = newBlock.transform.Find("TopConnection");
        Transform bottomConnection = newBlock.transform.Find("BottomConnection");

        if (topConnection != null)
            topConnection.gameObject.SetActive(hasTopConnection);

        if (bottomConnection != null)
            bottomConnection.gameObject.SetActive(hasBottomConnection);
    }

    private GameObject CreateTextElement(string text, Transform parent, Vector2 position)
    {
        GameObject textGO = new GameObject("Text", typeof(TextMeshProUGUI));
        textGO.transform.SetParent(parent, false);

        TextMeshProUGUI textComponent = textGO.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 24;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(60, 40);
        rect.anchoredPosition = position;

        return textGO;
    }

    private GameObject CreateInputField(Transform parent, Vector2 position)
    {
        GameObject inputGO = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputGO.transform.SetParent(parent, false);

        RectTransform rect = inputGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(40, 40);
        rect.anchoredPosition = position;

        Image bgImage = inputGO.GetComponent<Image>();
        bgImage.color = Color.white;

        TMP_InputField inputField = inputGO.GetComponent<TMP_InputField>();
        inputField.text = "10";
        inputField.textComponent = CreateTextElement("10", inputGO.transform, Vector2.zero).GetComponent<TextMeshProUGUI>();
        inputField.textComponent.fontSize = 24; // Mover la configuración del tamaño de fuente aquí

        return inputGO;
    }

    private void CreateConnection(GameObject block, string connectionName, Vector2 position)
    {
        GameObject connection = new GameObject(connectionName, typeof(RectTransform), typeof(Image));
        connection.transform.SetParent(block.transform, false);

        RectTransform rect = connection.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(20, 10);
        rect.anchoredPosition = position;

        Image image = connection.GetComponent<Image>();
        image.color = Color.gray;
    }
}

